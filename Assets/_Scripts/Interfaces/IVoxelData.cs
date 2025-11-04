using UnityEngine;
public interface IVoxelData
{
    int Width { get; }
    int Height { get; }
    int Depth { get; }
    float VoxelSize { get; }
    float MaxStability { get; }
    float MaxDurability { get; }
    float this[int x, int y, int z] { get; set; }

    void SetDurability(int x, int y, int z, float durability);
    void SetStability(int x, int y, int z, float stability);
    //int[] ApplyDamage(Vector3 hitPosition, float radius, float stabilityDamage, float durabilityDamage);
}