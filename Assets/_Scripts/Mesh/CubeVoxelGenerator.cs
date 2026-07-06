public class CubeVoxelGenerator : IVoxelGenerator
{
    public void Fill(IVoxelData voxelData)
    {
        for (int x = 0; x < voxelData.Width; x++)
            for (int y = 0; y < voxelData.Height; y++)
                for (int z = 0; z < voxelData.Depth; z++)
                {
                    voxelData[x, y, z] = 1;
                    voxelData.SetDurability(x, y, z, voxelData.MaxDurability);
                    voxelData.SetStability(x, y, z, voxelData.MaxStability);
                }
    }
}
