public class VoxelChunkPadding
{
    public VoxelPaddingLayer FaceXPlus { get; private set; }
    public VoxelPaddingLayer FaceYPlus { get; private set; }
    public VoxelPaddingLayer FaceZPlus { get; private set; }

    public VoxelPaddingLayer FaceXMinus { get; private set; }
    public VoxelPaddingLayer FaceYMinus { get; private set; }
    public VoxelPaddingLayer FaceZMinus { get; private set; }

    public VoxelPaddingLayer EdgeXPlusYPlus { get; private set; }
    public VoxelPaddingLayer EdgeXPlusZPlus { get; private set; }
    public VoxelPaddingLayer EdgeYPlusZPlus { get; private set; }

    public VoxelPaddingLayer CornerXPlusYPlusZPlus { get; private set; }

    public VoxelChunkPadding(int width, int height, int depth)
    {
        FaceXPlus = new VoxelPaddingLayer(height, depth);
        FaceYPlus = new VoxelPaddingLayer(width, depth);
        FaceZPlus = new VoxelPaddingLayer(width, height);

        FaceXMinus = new VoxelPaddingLayer(height, depth);
        FaceYMinus = new VoxelPaddingLayer(width, depth);
        FaceZMinus = new VoxelPaddingLayer(width, height);

        EdgeXPlusYPlus = new VoxelPaddingLayer(depth, 1);
        EdgeXPlusZPlus = new VoxelPaddingLayer(height, 1);
        EdgeYPlusZPlus = new VoxelPaddingLayer(width, 1);

        CornerXPlusYPlusZPlus = new VoxelPaddingLayer(1, 1);
    }
    public void SwapAll()
    {
        FaceXPlus.Swap();
        FaceYPlus.Swap();
        FaceZPlus.Swap();

        FaceXMinus.Swap();
        FaceYMinus.Swap();
        FaceZMinus.Swap();

        EdgeXPlusYPlus.Swap();
        EdgeXPlusZPlus.Swap();
        EdgeYPlusZPlus.Swap();

        CornerXPlusYPlusZPlus.Swap();
    }

    public void DisposeAll()
    {
        FaceXPlus?.Dispose();
        FaceYPlus?.Dispose();
        FaceZPlus?.Dispose();

        FaceXMinus?.Dispose();
        FaceYMinus?.Dispose();
        FaceZMinus?.Dispose();

        EdgeXPlusYPlus?.Dispose();
        EdgeXPlusZPlus?.Dispose();
        EdgeYPlusZPlus?.Dispose();

        CornerXPlusYPlusZPlus?.Dispose();
    }
}
