using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoxelChunk : IVoxelData
{
    private static readonly Unity.Profiling.ProfilerMarker ApplyDamageMarker = new("Voxel.Chunk.ApplyDamage");
    private static readonly Unity.Profiling.ProfilerMarker ApplyCrackMarker = new("Voxel.Chunk.ApplyCrack");
    private static readonly Unity.Profiling.ProfilerMarker DestroyOreShellMarker = new("Voxel.Chunk.DestroyOreShellLayer");
    private static readonly Unity.Profiling.ProfilerMarker DestroyAllOreMarker = new("Voxel.Chunk.DestroyAllOreVoxels");
    public int VoxelsPerChunk { get; }
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

    // Per-voxel state. Density drives mesh, ore index redirects damage to OreMineable.
    private NativeArray<float> voxelArray;
    private NativeArray<float> durabilityArray;
    private NativeArray<float> stabilityArray;
    private NativeArray<int> oreIndexArray;
    private NativeArray<float> crackPercentArray;
    private NativeArray<Color32> crackColorArray;

    private readonly float _alpha;

    private readonly CubeVoxelGenerator _cubeVoxelGenerator = new();

    public VoxelChunk(int voxelsPerChunk, float voxelSize, float maxStability, float maxDurability, float alpha)
    {
        VoxelsPerChunk = voxelsPerChunk;
        VoxelSize = voxelSize;
        MaxStability = maxStability;
        MaxDurability = maxDurability;

        _alpha = alpha;

        Padding = new(VoxelsPerChunk);

        voxelArray = new NativeArray<float>(VoxelsPerChunk * VoxelsPerChunk * VoxelsPerChunk, Allocator.Persistent);
        durabilityArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        stabilityArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        oreIndexArray = new NativeArray<int>(voxelArray.Length, Allocator.Persistent);
        crackPercentArray = new NativeArray<float>(voxelArray.Length, Allocator.Persistent);
        crackColorArray = new NativeArray<Color32>(voxelArray.Length, Allocator.Persistent);

        _cubeVoxelGenerator.Fill(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFlatIndex(int x, int y, int z) => x + y * VoxelsPerChunk + z * VoxelsPerChunk * VoxelsPerChunk;

    public void SetDurability(int x, int y, int z, float durability)
    {
        durabilityArray[GetFlatIndex(x, y, z)] = durability;
        if (durability <= 0) voxelArray[GetFlatIndex(x, y, z)] = 0f;
    }

    public void SetStability(int x, int y, int z, float stability)
    {
        stabilityArray[GetFlatIndex(x, y, z)] = stability;
    }

    // Damages ground voxels in this chunk and reports ore ids touched by the hit.
    public bool ApplyDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage, HashSet<int> oreIndexes)
    {
        NativeQueue<int> oreQueue = new(Allocator.TempJob);
        NativeArray<int> hasDensityChanges = new(1, Allocator.TempJob);

        var job = new ApplyDamageJob
        {
            VoxelsPerChunk = this.VoxelsPerChunk,
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

            FoundOreIndexes = oreQueue.AsParallelWriter(),
            HasDensityChanges = hasDensityChanges
        };

        JobHandle handle = job.Schedule(voxelArray.Length, 64);
        handle.Complete();

        var tmp = oreQueue.ToArray(Allocator.Temp);
        for (int i = 0; i < tmp.Length; i++)
            oreIndexes.Add(tmp[i]);

        bool densityChanged = hasDensityChanges[0] != 0;
        tmp.Dispose();
        hasDensityChanges.Dispose();
        oreQueue.Dispose();
        return densityChanged;
    }

    [BurstCompile]
    public struct ApplyDamageJob : IJobParallelFor
    {
        [ReadOnly] public int VoxelsPerChunk;
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

        public NativeQueue<int>.ParallelWriter FoundOreIndexes;
        [NativeDisableParallelForRestriction] public NativeArray<int> HasDensityChanges;

        public void Execute(int index)
        {
            float val = Data[index];
            if (val < 0.5f) return;

            float dur = Durability[index];
            if (dur <= 0f) return;

            int z = index / (VoxelsPerChunk * VoxelsPerChunk);
            int rem = index - z * VoxelsPerChunk * VoxelsPerChunk;
            int y = rem / VoxelsPerChunk;
            int x = rem % VoxelsPerChunk;

            float3 voxelCenter = new float3(x + 0.5f, y + 0.5f, z + 0.5f);
            float3 delta = voxelCenter - LocalPosVox;
            float distanceSq = math.lengthsq(delta);

            if (distanceSq > SqrRadiusVox) return;

            // Ore voxels are handled by OreMineable, not by ground durability.
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
            HasDensityChanges[0] = 1;
        }
    }

    // Marks voxels covered by an ore instance so hits can route damage to that ore.
    public void OreGroundInitialize(Vector3 center, float radius, int oreTypeIndex)
    {
        var job = new OreGroundInitializeJob
        {
            VoxelsPerChunk = this.VoxelsPerChunk,
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
        [ReadOnly] public int VoxelsPerChunk;
        [ReadOnly] public int OreTypeIndex;

        [NativeDisableParallelForRestriction] public NativeArray<float> Data;
        [NativeDisableParallelForRestriction] public NativeArray<int> OreIndex;

        [ReadOnly] public float3 LocalPosVox;
        [ReadOnly] public float SqrRadiusVox;

        public void Execute(int index)
        {
            float val = Data[index];
            if (val < 0.5f) return;

            int z = index / (VoxelsPerChunk * VoxelsPerChunk);
            int rem = index - z * VoxelsPerChunk * VoxelsPerChunk;
            int y = rem / VoxelsPerChunk;
            int x = rem % VoxelsPerChunk;

            float3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
            if (math.lengthsq(voxelCenter - LocalPosVox) <= SqrRadiusVox)
            {
                OreIndex[index] = OreTypeIndex;
            }
        }
    }

    // Updates crack texture data only; geometry stays unchanged.
    public NativeArray<Color32> ApplyCrack(Vector3 hitPoint, Vector3 center, float stability, float maxStability, int oreTypeIndex)
    {
        var job = new ApplyCrackJob
        {
            VoxelsPerChunk = this.VoxelsPerChunk,

            Stability = stability,
            MaxStability = maxStability,

            OreTypeIndex = oreTypeIndex,
            Angle = Mathf.Cos(_alpha * Mathf.Deg2Rad),

            Data = voxelArray,
            CrackPercent = crackPercentArray,
            OreIndex = oreIndexArray,
            CrackColors = crackColorArray,

            PaddedDensity = Padding.PaddedDensity,
            PaddingSize = Padding.PaddingSize,
            PaddedSize = Padding.PaddedSize,

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
        [ReadOnly] public int VoxelsPerChunk;

        [ReadOnly] public int OreTypeIndex;
        [ReadOnly] public float Angle;

        [ReadOnly] public float Stability;
        [ReadOnly] public float MaxStability;

        [ReadOnly] public NativeArray<float> Data; // read/write
        [NativeDisableParallelForRestriction] public NativeArray<float> CrackPercent;
        [ReadOnly] public NativeArray<int> OreIndex;
        [NativeDisableParallelForRestriction] public NativeArray<Color32> CrackColors;

        [ReadOnly] public NativeArray<float> PaddedDensity;
        [ReadOnly] public int PaddingSize;
        [ReadOnly] public int PaddedSize;

        [ReadOnly] public float3 CenterVox;
        [ReadOnly] public float3 HitDirVox;

        public void Execute(int index)
        {
            if (Data[index] < 0.5f) return;
            if (OreIndex[index] != OreTypeIndex) return;

            int z = index / (VoxelsPerChunk * VoxelsPerChunk);
            int rem = index - z * VoxelsPerChunk * VoxelsPerChunk;
            int y = rem / VoxelsPerChunk;
            int x = rem % VoxelsPerChunk;

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
            int paddedX = x + PaddingSize;
            int paddedY = y + PaddingSize;
            int paddedZ = z + PaddingSize;

            if (paddedX < 0 || paddedX >= PaddedSize ||
                paddedY < 0 || paddedY >= PaddedSize ||
                paddedZ < 0 || paddedZ >= PaddedSize)
                return 0f;

            return PaddedDensity[paddedX + paddedY * PaddedSize + paddedZ * PaddedSize * PaddedSize];
        }
    }

    // Removes visible shell voxels on the hit-facing side of an ore.
    public void DestroyOreShellLayer(Vector3 hitPoint, Vector3 center, int oreTypeIndex)
    {
        var job = new DestroyOreShellLayerJob
        {
            VoxelsPerChunk = this.VoxelsPerChunk,

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
        [ReadOnly] public int VoxelsPerChunk;

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

            int z = index / (VoxelsPerChunk * VoxelsPerChunk);
            int rem = index - z * VoxelsPerChunk * VoxelsPerChunk;
            int y = rem / VoxelsPerChunk;
            int x = rem % VoxelsPerChunk;

            float3 voxelPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
            float3 toVoxel = math.normalize(voxelPos - CenterVox);
            if (math.dot(HitDirVox, toVoxel) >= Angle || CrackPercent[index] > 0)
            {
                Data[index] = 0f;
            }
        }
    }

    // Final ore destruction clears every voxel belonging to that ore id.
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
        if (left) for (int y = 0; y < VoxelsPerChunk; y++) for (int z = 0; z < VoxelsPerChunk; z++) this[0, y, z] = 0f;
        if (right) for (int y = 0; y < VoxelsPerChunk; y++) for (int z = 0; z < VoxelsPerChunk; z++) this[VoxelsPerChunk - 1, y, z] = 0f;
        if (bottom) for (int x = 0; x < VoxelsPerChunk; x++) for (int z = 0; z < VoxelsPerChunk; z++) this[x, 0, z] = 0f;
        if (top) for (int x = 0; x < VoxelsPerChunk; x++) for (int z = 0; z < VoxelsPerChunk; z++) this[x, VoxelsPerChunk - 1, z] = 0f;
        if (back) for (int x = 0; x < VoxelsPerChunk; x++) for (int y = 0; y < VoxelsPerChunk; y++) this[x, y, 0] = 0f;
        if (front) for (int x = 0; x < VoxelsPerChunk; x++) for (int y = 0; y < VoxelsPerChunk; y++) this[x, y, VoxelsPerChunk - 1] = 0f;
    }

    public float GetVoxelValue(int x, int y, int z)
    {
        return Padding.GetVoxelValue(x, y, z);
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

