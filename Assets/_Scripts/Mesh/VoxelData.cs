using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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
    }

    public void SetDurability(int x, int y, int z, float durability)
    {
        _durability[x, y, z] = durability;
    }

    public void SetStability(int x, int y, int z, float stability)
    {
        _stability[x, y, z] = stability;
    }

    public void ApplyDamage(Vector3 hitPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
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
                    Vector3 voxelCenter = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    float distance = (voxelCenter - localPos).magnitude;
                    float damageFactor = Mathf.Clamp01((radius - distance) / radius);

                    if ((voxelCenter - localPos).sqrMagnitude <= sqrRadius)
                    {
                        _stability[x, y, z] = Mathf.Clamp(_stability[x, y, z] - stabilityDamage * damageFactor, 0, MaxStability);
                        if (_stability[x, y, z] == 0)
                            _durability[x, y, z] = Mathf.Clamp(_durability[x, y, z] - durabilityDamage * damageFactor, 0, MaxDurability);
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
}
