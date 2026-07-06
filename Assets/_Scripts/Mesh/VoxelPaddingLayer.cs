using Unity.Collections;

public class VoxelPaddingLayer
{
    public NativeArray<float> Current => _current;
    private NativeArray<float> _current;
    private NativeArray<float> _next;

    private int _width;
    private int _height;

    public VoxelPaddingLayer(int width, int height)
    {
        _width = width;
        _height = height;

        _current = new NativeArray<float>(width * height, Allocator.Persistent);
        _next = new NativeArray<float>(width * height, Allocator.Persistent);
    }

    public void Swap()
    {
        (_next, _current) = (_current, _next);
    }
    public float Get(int x, int y) => _current[x + y * _width];
    public void Set(int x, int y, float value) => _next[x + y * _width] = value;

    public void Dispose()
    {
        if (_current.IsCreated) _current.Dispose();
        if (_next.IsCreated) _next.Dispose();
    }
}
