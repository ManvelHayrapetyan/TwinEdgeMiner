public class CubeVoxelGenerator : IVoxelGenerator
{
    // Starts a chunk as solid ground with full stability and durability.
    public void Fill(IVoxelData voxelData)
    {
        for (int x = 0; x < voxelData.VoxelsPerChunk; x++)
            for (int y = 0; y < voxelData.VoxelsPerChunk; y++)
                for (int z = 0; z < voxelData.VoxelsPerChunk; z++)
                {
                    voxelData[x, y, z] = 1;
                    voxelData.SetDurability(x, y, z, voxelData.MaxDurability);
                    voxelData.SetStability(x, y, z, voxelData.MaxStability);
                }
    }
}
