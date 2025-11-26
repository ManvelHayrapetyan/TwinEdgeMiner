public class VoxelPaddingLayer
{
    public float[,] Current { get; private set; }
    public float[,] Next { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public VoxelPaddingLayer(int width, int height)
    {
        Width = width;
        Height = height;
        Current = new float[width, height];
        Next = new float[width, height];
    }

    public void Swap()
    {
        (Next, Current) = (Current, Next);
    }
    public float Get(int x, int y) => Current[x, y];
    public void Set(int x, int y, float value) => Next[x, y] = value;
}
