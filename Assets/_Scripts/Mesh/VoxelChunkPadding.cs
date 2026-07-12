using Unity.Collections;

public class VoxelChunkPadding
{
    public int PaddingSize { get; }
    public int PaddedSize { get; }
    public NativeArray<float> PaddedDensity => _paddedDensityCurrent;

    private NativeArray<float> _paddedDensityCurrent;
    private NativeArray<float> _paddedDensityNext;

    public VoxelChunkPadding(int voxelsPerChunk, int paddingSize = 2)
    {
        PaddingSize = paddingSize;
        PaddedSize = voxelsPerChunk + paddingSize * 2;

        _paddedDensityCurrent = new NativeArray<float>(PaddedSize * PaddedSize * PaddedSize, Allocator.Persistent);
        _paddedDensityNext = new NativeArray<float>(_paddedDensityCurrent.Length, Allocator.Persistent);
    }

    public void ClearNext()
    {
        for (int i = 0; i < _paddedDensityNext.Length; i++)
            _paddedDensityNext[i] = 0f;
    }

    public void CopyDensityBlockFrom(
        NativeArray<float> sourceDensity,
        int sourceVoxelsPerChunk,
        int sourceStartX,
        int sourceStartY,
        int sourceStartZ,
        int destinationStartX,
        int destinationStartY,
        int destinationStartZ,
        int sizeX,
        int sizeY,
        int sizeZ)
    {
        for (int z = 0; z < sizeZ; z++)
            for (int y = 0; y < sizeY; y++)
            {
                int sourceIndex =
                    sourceStartX +
                    (sourceStartY + y) * sourceVoxelsPerChunk +
                    (sourceStartZ + z) * sourceVoxelsPerChunk * sourceVoxelsPerChunk;

                int destinationIndex =
                    destinationStartX +
                    (destinationStartY + y) * PaddedSize +
                    (destinationStartZ + z) * PaddedSize * PaddedSize;

                NativeArray<float>.Copy(sourceDensity, sourceIndex, _paddedDensityNext, destinationIndex, sizeX);
            }
    }


    public void SwapAll()
    {
        (_paddedDensityNext, _paddedDensityCurrent) = (_paddedDensityCurrent, _paddedDensityNext);
    }

    public float GetVoxelValue(int x, int y, int z)
    {
        int paddedX = x + PaddingSize;
        int paddedY = y + PaddingSize;
        int paddedZ = z + PaddingSize;

        if (paddedX < 0 || paddedX >= PaddedSize ||
            paddedY < 0 || paddedY >= PaddedSize ||
            paddedZ < 0 || paddedZ >= PaddedSize)
            return 0f;

        return PaddedDensity[paddedX + paddedY * PaddedSize + paddedZ * PaddedSize * PaddedSize];
    }

    public void DisposeAll()
    {
        if (_paddedDensityCurrent.IsCreated) _paddedDensityCurrent.Dispose();
        if (_paddedDensityNext.IsCreated) _paddedDensityNext.Dispose();
    }
}

