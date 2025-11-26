using System.Collections.Generic;
using UnityEngine;
public class VoxelData : IVoxelData
{
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public float VoxelSize { get; }
    public float MaxStability { get; }
    public float MaxDurability { get; }
    public float this[int x, int y, int z]
    {
        get => _data[x, y, z];
        set => _data[x, y, z] = value;
    }

    protected const float IsoLevel = 0.5f;

    protected readonly float[,,] _data;
    protected readonly float[,,] _stability;
    protected readonly float[,,] _durability;
    protected readonly bool[,,] _isAdjacentOre;
    protected readonly float[,,] _crackPercent;

    private static readonly Vector3Int[] NeighborDirs = {
        new(1,0,0), new(-1,0,0),
        new(0,1,0), new(0,-1,0),
        new(0,0,1), new(0,0,-1)};

    public VoxelData(int width, int height, int depth, float voxelSize, float maxStability, float maxDurability)
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
        _isAdjacentOre = new bool[width, height, depth];
        _crackPercent = new float[width, height, depth];
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

    public bool ApplyDamage(Vector3 hitPosition, float radius, float stabilityDamage, float durabilityDamage, out bool OreTouch)
    {
        OreTouch = false;
        bool chunkModified = false;

        Vector3 localPos = hitPosition / VoxelSize;
        int minX = Mathf.Max(0, Mathf.FloorToInt(localPos.x - radius));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(localPos.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPos.y - radius));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(localPos.y + radius));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(localPos.z - radius));
        int maxZ = Mathf.Min(Depth - 1, Mathf.CeilToInt(localPos.z + radius));

        float sqrRadius = radius * radius;

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;

                    Vector3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 delta = voxelCenter - localPos;
                    float distSq = delta.sqrMagnitude;

                    if (distSq > sqrRadius)
                        continue;

                    float damageFactor = Mathf.Clamp01(1f - Mathf.Sqrt(distSq) / radius);

                    if (_isAdjacentOre[x, y, z])
                    {
                        OreTouch = true;
                        continue;
                    }

                    _stability[x, y, z] = Mathf.Max(0, _stability[x, y, z] - stabilityDamage * damageFactor);

                    float durabilityReduction = (MaxStability == 0)
                        ? durabilityDamage * damageFactor
                        : durabilityDamage * damageFactor * (MaxStability - _stability[x, y, z]) / MaxStability;

                    _durability[x, y, z] = Mathf.Max(0, _durability[x, y, z] - durabilityReduction);

                    float normalizedDurability = _durability[x, y, z] / MaxDurability;
                    this[x, y, z] = normalizedDurability <= 0f ? 0f : 0.5f + 0.5f * normalizedDurability;

                    chunkModified = true;
                }
        return chunkModified;
    }


    public Vector3Int[] AdjacentOreIndexInitialize(Vector3 center, float radius)
    {
        List<Vector3Int> voxels = new();
        Vector3 localPos = center / VoxelSize;

        int minX = Mathf.Max(0, Mathf.FloorToInt(localPos.x - radius));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(localPos.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPos.y - radius));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(localPos.y + radius));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(localPos.z - radius));
        int maxZ = Mathf.Min(Depth - 1, Mathf.CeilToInt(localPos.z + radius));

        float sqrRadius = radius * radius;

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (this[x, y, z] < 0.5f || _durability[x, y, z] <= 0)
                        continue;
                    Vector3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    if ((voxelCenter - localPos).sqrMagnitude <= sqrRadius)
                    {
                        voxels.Add(new Vector3Int(x, y, z));
                        _isAdjacentOre[x, y, z] = true;
                    }
                }
        return voxels.ToArray();
    }

    public void ApplyCrackToVoxel(float crackPercent, Vector3Int voxel)
    {
        if (IsAdjacentToAir(voxel))
            _crackPercent[voxel.x, voxel.y, voxel.z] = crackPercent;
    }

    public bool IsAdjacentToAir(Vector3Int voxel)
    {
        foreach (var dir in NeighborDirs)
        {
            Vector3Int neighbor = voxel + dir;

            if (neighbor.x < 0 || neighbor.x >= Width ||
                neighbor.y < 0 || neighbor.y >= Height ||
                neighbor.z < 0 || neighbor.z >= Depth)
                continue;

            if (this[neighbor.x, neighbor.y, neighbor.z] < 0.5f)
                return true;
        }
        return false;
    }

    public void DestroyCracked(Vector3Int voxel)
    {
        if (_crackPercent[voxel.x, voxel.y, voxel.z] > 0)
            this[voxel.x, voxel.y, voxel.z] = 0f;
    }
}
