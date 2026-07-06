using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class VoxelChunk : IVoxelData
{
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public float VoxelSize { get; }
    public float MaxStability { get; }
    public float MaxDurability { get; }
    public VoxelChunkPadding Padding { get; }
    public NativeArray<float> Dencity { get => voxelArray; }
    public float this[int x, int y, int z]
    {
        get => voxelArray[GetFlatIndex(x, y, z)];
        set => voxelArray[GetFlatIndex(x, y, z)] = value;
    }

    private const float IsoLevel = 0.5f;

    private NativeArray<float> voxelArray;
    private NativeArray<float> durabilityArray;
    private NativeArray<float> stabilityArray;
    private NativeArray<int> oreIndexArray;
    private NativeArray<float> crackPercentArray;
    private NativeArray<Color32> crackColorArray;

    private readonly float _alpha;

    private readonly CubeVoxelGenerator _cubeVoxelGenerator = new();

    public VoxelChunk(int width, int height, int depth, float voxelSize, float maxStability, float maxDurability, float alpha)
    {
        Width = width;
        Height = height;
        Depth = depth;
        VoxelSize = voxelSize;
        MaxStability = maxStability;
        MaxDurability = maxDurability;

        _alpha = alpha;

        Padding = new(width, height, depth);

        voxelArray = new NativeArray<float>(Width * Height * Depth, Allocator.Persistent);
        durabilityArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        stabilityArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        oreIndexArray = new NativeArray<int>(voxelArray.Length, Allocator.Persistent);
        crackPercentArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        crackColorArray = new NativeArray<Color32>(voxelArray.Length, Allocator.Persistent);

        _cubeVoxelGenerator.Fill(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFlatIndex(int x, int y, int z) => x + y * Width + z * Width * Height;

    public void SetDurability(int x, int y, int z, float durability)
    {
        durabilityArray[GetFlatIndex(x, y, z)] = durability;
        if (durability <= 0) voxelArray[GetFlatIndex(x, y, z)] = 0f;
    }

    public void SetStability(int x, int y, int z, float stability)
    {
        stabilityArray[GetFlatIndex(x, y, z)] = stability;
    }
    public int[] ApplyDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        NativeQueue<int> oreQueue = new(Allocator.TempJob);

        var job = new ApplyDamageJob
        {
            Width = Width,
            Height = Height,
            Depth = Depth,
            MaxStability = MaxStability,
            MaxDurability = MaxDurability,

            Data = voxelArray,
            Durability = durabilityArray,
            Stability = stabilityArray,
            OreIndex = oreIndexArray,

            LocalPosVox = worldPosition / VoxelSize,
            RadiusVox = radius / VoxelSize,
            SqrRadiusVox = (radius / VoxelSize) * (radius / VoxelSize),
            StabilityDamage = stabilityDamage,
            DurabilityDamage = durabilityDamage,

            FoundOreIndexes = oreQueue.AsParallelWriter()
        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();

        var tmp = oreQueue.ToArray(Allocator.Temp);
        int[] returnArray = tmp.Distinct().ToArray();
        tmp.Dispose();
        oreQueue.Dispose();
        return returnArray;
    }

    [BurstCompile]
    public struct ApplyDamageJob : IJobParallelFor
    {
        [ReadOnly] public int Width;
        [ReadOnly] public int Height;
        [ReadOnly] public int Depth;
        [ReadOnly] public float MaxStability;
        [ReadOnly] public float MaxDurability;

        [NativeDisableParallelForRestriction] public NativeArray<float> Data; // read/write
        [NativeDisableParallelForRestriction] public NativeArray<float> Stability;
        [NativeDisableParallelForRestriction] public NativeArray<float> Durability;
        [NativeDisableParallelForRestriction] public NativeArray<int> OreIndex;

        [ReadOnly] public float3 LocalPosVox; // hitPosition in voxel units
        [ReadOnly] public float RadiusVox;
        [ReadOnly] public float SqrRadiusVox;
        [ReadOnly] public float StabilityDamage;
        [ReadOnly] public float DurabilityDamage;

        // Output: unique ore indexes found. We'll push occurrences; caller can dedupe.
        //public NativeHashSet<int> FoundOreIndexes; // must be created with Allocator.TempJob
        public NativeQueue<int>.ParallelWriter FoundOreIndexes;

        public void Execute(int index)
        {
            // read scalar (value)
            float val = Data[index];
            if (val < 0.5f) return;

            float dur = Durability[index];
            if (dur <= 0f) return;

            // index is 1D index of voxel (0..W*H*D-1)
            // compute x,y,z if necessary (we'll compute center)
            int z = index / (Width * Height);
            int rem = index - z * Width * Height;
            int y = rem / Width;
            int x = rem % Width;


            // compute voxel center in voxel cords (x+0.5 etc)
            float3 voxelCenter = new float3(x + 0.5f, y + 0.5f, z + 0.5f);
            float3 delta = voxelCenter - LocalPosVox;
            float distanceSq = math.lengthsq(delta);

            if (distanceSq > SqrRadiusVox) return;

            int ore = OreIndex[index];
            if (ore > 0)
            {
                FoundOreIndexes.Enqueue(ore);
                return;
            }

            float damageFactor = math.clamp(1f - math.sqrt(distanceSq) / RadiusVox, 0f, 1f);

            float st = Stability[index];
            st = math.max(0f, st - StabilityDamage * damageFactor);
            Stability[index] = st;

            float durabilityReduction = (MaxStability == 0f)
                ? DurabilityDamage * damageFactor
                : DurabilityDamage * damageFactor * (MaxStability - st) / MaxStability;

            dur = math.max(0f, dur - durabilityReduction);
            Durability[index] = dur;

            float normalizedDurability = dur / MaxDurability;
            Data[index] = normalizedDurability <= 0f ? 0f : 0.5f + 0.5f * normalizedDurability;
        }
    }

    public void OreGroundInitialize(Vector3 center, float radius, int oreTypeIndex)
    {
        var job = new OreGroundInitializeJob
        {
            Width = Width,
            Height = Height,
            Depth = Depth,
            OreTypeIndex = oreTypeIndex,

            Data = voxelArray,
            OreIndex = oreIndexArray,

            LocalPosVox = center / VoxelSize,
            SqrRadiusVox = (radius / VoxelSize) * (radius / VoxelSize),
        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();
    }

    [BurstCompile]
    public struct OreGroundInitializeJob : IJobParallelFor
    {
        [ReadOnly] public int Width;
        [ReadOnly] public int Height;
        [ReadOnly] public int Depth;
        [ReadOnly] public int OreTypeIndex;

        [NativeDisableParallelForRestriction] public NativeArray<float> Data;
        [NativeDisableParallelForRestriction] public NativeArray<int> OreIndex;

        [ReadOnly] public float3 LocalPosVox;
        [ReadOnly] public float SqrRadiusVox;

        public void Execute(int index)
        {
            float val = Data[index];
            if (val < 0.5f) return;

            int z = index / (Width * Height);
            int rem = index - z * Width * Height;
            int y = rem / Width;
            int x = rem % Width;

            float3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
            if (math.lengthsq(voxelCenter - LocalPosVox) <= SqrRadiusVox)
            {
                OreIndex[index] = OreTypeIndex;
            }
        }
    }

    public NativeArray<Color32> ApplyCrack(Vector3 hitPoint, Vector3 center, float stability, float maxStability, int oreTypeIndex)
    {
        var job = new ApplyCrackJob
        {
            Width = Width,
            Height = Height,
            Depth = Depth,

            Stability = stability,
            MaxStability = maxStability,

            OreTypeIndex = oreTypeIndex,
            Angle = Mathf.Cos(_alpha * Mathf.Deg2Rad),

            Data = voxelArray,
            CrackPercent = crackPercentArray,
            OreIndex = oreIndexArray,
            CrackColors = crackColorArray,

            FaceXPlus = Padding.FaceXPlus.Current,
            FaceYPlus = Padding.FaceYPlus.Current,
            FaceZPlus = Padding.FaceZPlus.Current,

            FaceXMinus = Padding.FaceXMinus.Current,
            FaceYMinus = Padding.FaceYMinus.Current,
            FaceZMinus = Padding.FaceZMinus.Current,

            CenterVox = center / VoxelSize,
            HitDirVox = math.normalize(hitPoint / VoxelSize - center / VoxelSize),

        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();

        return crackColorArray;
    }
    [BurstCompile]
    public struct ApplyCrackJob : IJobParallelFor
    {
        [ReadOnly] public int Width;
        [ReadOnly] public int Height;
        [ReadOnly] public int Depth;

        [ReadOnly] public int OreTypeIndex;
        [ReadOnly] public float Angle;

        [ReadOnly] public float Stability;
        [ReadOnly] public float MaxStability;

        [ReadOnly] public NativeArray<float> Data; // read/write
        [NativeDisableParallelForRestriction] public NativeArray<float> CrackPercent;
        [ReadOnly] public NativeArray<int> OreIndex;
        [NativeDisableParallelForRestriction] public NativeArray<Color32> CrackColors;

        [ReadOnly] public NativeArray<float> FaceXPlus;
        [ReadOnly] public NativeArray<float> FaceYPlus;
        [ReadOnly] public NativeArray<float> FaceZPlus;

        [ReadOnly] public NativeArray<float> FaceXMinus;
        [ReadOnly] public NativeArray<float> FaceYMinus;
        [ReadOnly] public NativeArray<float> FaceZMinus;

        [ReadOnly] public float3 CenterVox;
        [ReadOnly] public float3 HitDirVox;

        public void Execute(int index)
        {
            if (Data[index] < 0.5f) return;
            if (OreIndex[index] != OreTypeIndex) return;

            int z = index / (Width * Height);
            int rem = index - z * Width * Height;
            int y = rem / Width;
            int x = rem % Width;

            float3 voxelPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
            float3 toVoxel = math.normalize(voxelPos - CenterVox);
            if (math.dot(HitDirVox, toVoxel) >= Angle)
            {
                float crackPercent = MaxStability != 0 ? 1 - Stability / MaxStability : 0;

                if (IsAdjacentToAir(x, y, z))
                {
                    CrackPercent[index] = crackPercent;
                    byte v = (byte)(crackPercent * 255f);
                    CrackColors[index] = new Color32(v, 0, 0, 0);
                }
            }
        }

        private bool IsAdjacentToAir(int x, int y, int z)
        {
            if (GetVoxelValue(x + 1, y, z) < 0.5f)
                return true;
            if (GetVoxelValue(x, y + 1, z) < 0.5f)
                return true;
            if (GetVoxelValue(x, y, z + 1) < 0.5f)
                return true;
            if (GetVoxelValue(x - 1, y, z) < 0.5f)
                return true;
            if (GetVoxelValue(x, y - 1, z) < 0.5f)
                return true;
            if (GetVoxelValue(x, y, z - 1) < 0.5f)
                return true;

            return false;
        }

        public float GetVoxelValue(int x, int y, int z)
        {
            if (x >= 0 && x < Width &&
                y >= 0 && y < Height &&
                z >= 0 && z < Depth)
            {
                return Data[x + y * Width + z * Width * Height];
            }

            if (x == Width && y >= 0 && y < Height && z >= 0 && z < Depth)
                return FaceXPlus[y + z * Height];
            if (x == -1 && y >= 0 && y < Height && z >= 0 && z < Depth)
                return FaceXMinus[y + z * Height];

            if (y == Height && x >= 0 && x < Width && z >= 0 && z < Depth)
                return FaceYPlus[x + z * Width];
            if (y == -1 && x >= 0 && x < Width && z >= 0 && z < Depth)
                return FaceYMinus[x + z * Width];

            if (z == Depth && x >= 0 && x < Width && y >= 0 && y < Height)
                return FaceZPlus[x + y * Width];
            if (z == -1 && x >= 0 && x < Width && y >= 0 && y < Height)
                return FaceZMinus[x + y * Width];

            return 0f;
        }
    }

    public void DestroyOreShellLayer(Vector3 hitPoint, Vector3 center, int oreTypeIndex)
    {
        var job = new DestroyOreShellLayerJob
        {
            Width = Width,
            Height = Height,
            Depth = Depth,

            OreTypeIndex = oreTypeIndex,
            Angle = Mathf.Cos(_alpha * Mathf.Deg2Rad),

            Data = voxelArray,
            CrackPercent = crackPercentArray,
            OreIndex = oreIndexArray,

            CenterVox = center / VoxelSize,
            HitDirVox = math.normalize(hitPoint / VoxelSize - center / VoxelSize),

        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();
    }

    [BurstCompile]
    public struct DestroyOreShellLayerJob : IJobParallelFor
    {
        [ReadOnly] public int Width;
        [ReadOnly] public int Height;
        [ReadOnly] public int Depth;

        [ReadOnly] public int OreTypeIndex;
        [ReadOnly] public float Angle;

        [NativeDisableParallelForRestriction] public NativeArray<float> Data; // read/write
        [NativeDisableParallelForRestriction] public NativeArray<float> CrackPercent;
        [NativeDisableParallelForRestriction] public NativeArray<int> OreIndex;

        [ReadOnly] public float3 CenterVox;
        [ReadOnly] public float3 HitDirVox;

        public void Execute(int index)
        {
            if (Data[index] < 0.5f) return;
            if (OreIndex[index] != OreTypeIndex) return;

            int z = index / (Width * Height);
            int rem = index - z * Width * Height;
            int y = rem / Width;
            int x = rem % Width;

            float3 voxelPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
            float3 toVoxel = math.normalize(voxelPos - CenterVox);
            if (math.dot(HitDirVox, toVoxel) >= Angle || CrackPercent[index] > 0)
            {
                Data[index] = 0f;
            }
        }

    }

    public void DestroyAllOreVoxels(int oreTypeIndex)
    {
        var job = new DestroyAllOreVoxelsJob
        {
            OreTypeIndex = oreTypeIndex,

            Data = voxelArray,
            OreIndex = oreIndexArray
        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();
    }

    [BurstCompile]
    public struct DestroyAllOreVoxelsJob : IJobParallelFor
    {
        [ReadOnly] public int OreTypeIndex;

        [NativeDisableParallelForRestriction] public NativeArray<float> Data;
        [NativeDisableParallelForRestriction] public NativeArray<int> OreIndex;

        public void Execute(int index)
        {
            if (Data[index] < 0.5f) return;
            if (OreIndex[index] != OreTypeIndex) return;
            Data[index] = 0;
        }
    }

    public void ApplyWorldBoundaries(bool left, bool right, bool bottom, bool top, bool back, bool front)
    {
        if (left) for (int y = 0; y < Height; y++) for (int z = 0; z < Depth; z++) this[0, y, z] = 0f;
        if (right) for (int y = 0; y < Height; y++) for (int z = 0; z < Depth; z++) this[Width - 1, y, z] = 0f;
        if (bottom) for (int x = 0; x < Width; x++) for (int z = 0; z < Depth; z++) this[x, 0, z] = 0f;
        if (top) for (int x = 0; x < Width; x++) for (int z = 0; z < Depth; z++) this[x, Height - 1, z] = 0f;
        if (back) for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++) this[x, y, 0] = 0f;
        if (front) for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++) this[x, y, Depth - 1] = 0f;
    }

    public float GetVoxelValue(int x, int y, int z)
    {
        if (x >= 0 && x < Width &&
            y >= 0 && y < Height &&
            z >= 0 && z < Depth)
        {
            return this[x, y, z];
        }

        if (x == Width && y >= 0 && y < Height && z >= 0 && z < Depth)
            return Padding.FaceXPlus.Get(y, z);
        if (x == -1 && y >= 0 && y < Height && z >= 0 && z < Depth)
            return Padding.FaceXMinus.Get(y, z);

        if (y == Height && x >= 0 && x < Width && z >= 0 && z < Depth)
            return Padding.FaceYPlus.Get(x, z);
        if (y == -1 && x >= 0 && x < Width && z >= 0 && z < Depth)
            return Padding.FaceYMinus.Get(x, z);

        if (z == Depth && x >= 0 && x < Width && y >= 0 && y < Height)
            return Padding.FaceZPlus.Get(x, y);
        if (z == -1 && x >= 0 && x < Width && y >= 0 && y < Height)
            return Padding.FaceZMinus.Get(x, y);


        if (x == Width && y == Height && z >= 0 && z < Depth)
            return Padding.EdgeXPlusYPlus.Get(z, 0);
        if (x == Width && z == Depth && y >= 0 && y < Height)
            return Padding.EdgeXPlusZPlus.Get(y, 0);
        if (y == Height && z == Depth && x >= 0 && x < Width)
            return Padding.EdgeYPlusZPlus.Get(x, 0);

        if (x == Width && y == Height && z == Depth)
            return Padding.CornerXPlusYPlusZPlus.Get(0, 0);

        return 0f;
    }

    public void Dispose()
    {
        if (voxelArray.IsCreated) voxelArray.Dispose();
        if (durabilityArray.IsCreated) durabilityArray.Dispose();
        if (stabilityArray.IsCreated) stabilityArray.Dispose();
        if (oreIndexArray.IsCreated) oreIndexArray.Dispose();
        if (crackPercentArray.IsCreated) crackPercentArray.Dispose();
        if (crackColorArray.IsCreated) crackColorArray.Dispose();
        Padding.DisposeAll();
    }
}
