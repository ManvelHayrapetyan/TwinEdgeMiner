public interface IVoxelData
{
    int VoxelsPerChunk { get; }
    float VoxelSize { get; }
    float MaxStability { get; }
    float MaxDurability { get; }
    float this[int x, int y, int z] { get; set; }

    void SetDurability(int x, int y, int z, float durability);
    void SetStability(int x, int y, int z, float stability);
}

