using System;
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

    public void ApplyDamage(Vector3 hitPosition, float radius, float stabilityDamage, float durabilityDamage, out bool OreTouch)
    {
        OreTouch = false;
        Debug.Log($"{radius},{stabilityDamage},{durabilityDamage}");
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
                    float distance = (voxelCenter - localPos).magnitude;
                    float damageFactor = Mathf.Clamp01((radius - distance) / radius);

                    if ((voxelCenter - localPos).sqrMagnitude <= sqrRadius)
                    {
                        if (_isAdjacentOre[x, y, z])
                        {
                            OreTouch = true;
                            Debug.Log("asdasdasdasdsadsadsadsa");
                            continue;
                        }
                        _stability[x, y, z] = Mathf.Clamp(_stability[x, y, z] - stabilityDamage * damageFactor, 0, MaxStability);
                        if (MaxStability == 0)
                            _durability[x, y, z] = Mathf.Clamp(_durability[x, y, z] - durabilityDamage * damageFactor,
                               0, MaxDurability);
                        else
                            _durability[x, y, z] = Mathf.Clamp(_durability[x, y, z] - durabilityDamage * damageFactor *
                                    (MaxStability - _stability[x, y, z]) / MaxStability,
                                    0, MaxDurability);
                        if (_durability[x, y, z] < IsoLevel)
                            this[x, y, z] = 0f;
                        else
                            this[x, y, z] = 0.5f + 0.5f * (_durability[x, y, z] / MaxDurability);
                    }
                }
    }

    public Vector3Int[] AdjacentOreIndexInitialize(Vector3 center, float radius)
    {
        Debug.Log($"AdjacentOreIndexInitialize center {center} radius {radius}");
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
                    float distance = (voxelCenter - localPos).magnitude;
                    if ((voxelCenter - localPos).sqrMagnitude <= sqrRadius)
                    {
                        voxels.Add(new Vector3Int(x, y, z));
                        _isAdjacentOre[x,y,z] = true;
                    }
                }
        return voxels.ToArray();
    }

    public void ApplyCrackToVoxel(float crackPercent, Vector3Int voxel)
    {
        //if (IsAdjacentToAir(voxel))
            _crackPercent[voxel.x, voxel.y, voxel.z] = 1; /// dzi ara
    }

    public bool IsAdjacentToAir(Vector3Int voxel)
    {
        Vector3Int[] directions = {
        new(1,0,0), new(-1,0,0),
        new(0,1,0), new(0,-1,0),
        new(0,0,1), new(0,0,-1)};

        foreach (var dir in directions)
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

    public void Test()
    {
        bool HasNaN = false;
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    if (float.IsNaN(this[x, y, z]))
                    {
                        HasNaN = true;
                        Debug.Log($"NaN found at this ({x},{y},{z})");
                    }
                    if (float.IsNaN(_durability[x, y, z]))
                    {
                        HasNaN = true;
                        Debug.Log($"NaN found at durability ({x},{y},{z})");
                    }
                }

        Debug.Log("Contains NaN: " + HasNaN);
    }

    internal float GetCrackPercent(int x, int y, int z)
    {
        return _crackPercent[x, y, z];
    }
}
