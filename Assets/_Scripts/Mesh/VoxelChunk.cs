using System.Collections.Generic;
using System.Linq;
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
    public float this[int x, int y, int z]
    {
        get => _data[x, y, z];
        set => _data[x, y, z] = value;
    }


    private const float IsoLevel = 0.5f;

    private readonly float[,,] _data;
    private readonly float[,,] _stability;
    private readonly float[,,] _durability;
    private readonly int[,,] _oreIndex;
    private readonly float[,,] _crackPercent;


    private static readonly Vector3Int[] NeighborDirs = {
        new(1,0,0), new(-1,0,0),
        new(0,1,0), new(0,-1,0),
        new(0,0,1), new(0,0,-1)};

    private float _alpha;

    private CubeVoxelGenerator _cubeVoxelGenerator = new();

    private Texture3D _crackTex;
    private Color[] _crackColors;
    public VoxelChunk(int width, int height, int depth, float voxelSize, float maxStability, float maxDurability, float alpha)
    {
        Width = width;
        Height = height;
        Depth = depth;
        VoxelSize = voxelSize;
        MaxStability = maxStability;
        MaxDurability = maxDurability;

        _data = new float[width, height, depth];
        _stability = new float[width, height, depth];
        _durability = new float[width, height, depth];
        _oreIndex = new int[width, height, depth];
        _crackPercent = new float[width, height, depth];

        _alpha = alpha;

        _cubeVoxelGenerator.Fill(this);

        _crackTex = new Texture3D(Width, Height, Depth, TextureFormat.RGBA32, false);
        _crackColors = new Color[Width * Height * Depth];

        Padding = new(width, height, depth);
    }

    public void SetDurability(int x, int y, int z, float durability)
    {
        _durability[x, y, z] = durability;
        if (durability <= 0) this[x, y, z] = 0f;
    }

    public void SetStability(int x, int y, int z, float stability)
    {
        _stability[x, y, z] = stability;
    }
    public int[] ApplyDamage(Vector3 hitPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        HashSet<int> indexes = new();

        Vector3 localPos = hitPosition / VoxelSize;
        float radiusVox = radius / VoxelSize;
        float sqrRadius = radiusVox * radiusVox;
        int minX = Mathf.Max(0, Mathf.FloorToInt(localPos.x - radiusVox));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(localPos.x + radiusVox));
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPos.y - radiusVox));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(localPos.y + radiusVox));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(localPos.z - radiusVox));
        int maxZ = Mathf.Min(Depth - 1, Mathf.CeilToInt(localPos.z + radiusVox));

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;

                    Vector3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 delta = voxelCenter - localPos;
                    float distanceSq = delta.sqrMagnitude;

                    if (distanceSq > sqrRadius)
                        continue;
                    if (_oreIndex[x, y, z] > 0)
                    {
                        indexes.Add(_oreIndex[x, y, z]);
                        continue;
                    }

                    float damageFactor = Mathf.Clamp01(1f - Mathf.Sqrt(distanceSq) / radiusVox);
                    _stability[x, y, z] = Mathf.Max(0, _stability[x, y, z] - stabilityDamage * damageFactor);

                    float durabilityReduction = (MaxStability == 0)
                        ? durabilityDamage * damageFactor
                        : durabilityDamage * damageFactor * (MaxStability - _stability[x, y, z]) / MaxStability;

                    _durability[x, y, z] = Mathf.Max(0, _durability[x, y, z] - durabilityReduction);

                    float normalizedDurability = _durability[x, y, z] / MaxDurability;
                    this[x, y, z] = normalizedDurability <= 0f ? 0f : 0.5f + 0.5f * normalizedDurability;
                }
        return indexes.ToArray();
    }

    public void OreGroundInitialize(Vector3 center, float radius, int index)
    {
        int count = 0;
        List<int> indexes = new();
        Vector3 localPos = center / VoxelSize;
        float radiusVox = radius / VoxelSize;
        float sqrRadius = radiusVox * radiusVox;
        int minX = Mathf.Max(0, Mathf.FloorToInt(localPos.x - radiusVox));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(localPos.x + radiusVox));
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPos.y - radiusVox));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(localPos.y + radiusVox));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(localPos.z - radiusVox));
        int maxZ = Mathf.Min(Depth - 1, Mathf.CeilToInt(localPos.z + radiusVox));

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;
                    Vector3 voxelCenter = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    if ((voxelCenter - localPos).sqrMagnitude <= sqrRadius)
                    {
                        _oreIndex[x, y, z] = index;
                        count++;
                    }
                }
        Debug.Log($"idenx {index} - count - {count}");
    }

    public Color[] ApplyCrack(Vector3 hitPoint, Vector3 center, float stability, float maxStability, int oreIndex)
    {
        Vector3 hitPointVox = hitPoint / VoxelSize;
        Vector3 centerVox = center / VoxelSize;
        Vector3 hitDirection = (hitPointVox - centerVox).normalized;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    //_crackPercent[x, y, z] = 1;
                    //_crackColors[x + y * Width + z * Width * Height] = new Color(1, 0, 0, 0);

                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;
                    if (_oreIndex[x, y, z] != oreIndex)
                        continue;
                    Vector3 voxelPos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 toVoxel = (voxelPos - centerVox).normalized;
                    if (Vector3.Dot(hitDirection, toVoxel) >= angle)
                    {
                        float crackPercent = maxStability != 0 ? 1 - stability / maxStability : 0;

                        if (IsAdjacentToAir(new Vector3Int(x, y, z)))
                        {
                            _crackPercent[x, y, z] = crackPercent;
                            _crackColors[x + y * Width + z * Width * Height] = new Color(crackPercent, 0, 0, 0);
                        }
                    }
                }
        return _crackColors;
    }

    public void DestroyOreShellLayer(Vector3 hitPoint, Vector3 center, int oreIndex)
    {
        Vector3 hitPointVox = hitPoint / VoxelSize;
        Vector3 centerVox = center / VoxelSize;
        Vector3 hitDirection = (hitPointVox - centerVox).normalized;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    if (x < 5 && y < 5 && z < 5)
                        this[x, y, z] = 0;
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;
                    if (_oreIndex[x, y, z] != oreIndex)
                        continue;

                    Vector3 voxelPos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 toVoxel = (voxelPos - centerVox).normalized;

                    if (Vector3.Dot(hitDirection, toVoxel) >= angle || _crackPercent[x, y, z] > 0)
                    {
                        this[x, y, z] = 0f;
                    }
                }
    }

    public void DestroyAllOreVoxels(int oreIndex)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;
                    if (_oreIndex[x, y, z] != oreIndex)
                        continue;
                    this[x, y, z] = 0;
                }
    }

    private bool IsAdjacentToAir(Vector3Int voxel)
    {
        foreach (var dir in NeighborDirs)
        {
            Vector3Int neighbor = voxel + dir;

            if (GetVoxelValue(neighbor.x, neighbor.y, neighbor.z) < 0.5f)
                return true;
        }
        return false;
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
}
