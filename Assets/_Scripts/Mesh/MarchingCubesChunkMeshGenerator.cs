using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingCubesChunkMeshGenerator
{
    private static readonly Unity.Profiling.ProfilerMarker GenerateMeshMarker = new("Voxel.Mesh.GenerateMesh");
    private static readonly Unity.Profiling.ProfilerMarker ClearBuffersMarker = new("Voxel.Mesh.ClearBuffers");
    private static readonly Unity.Profiling.ProfilerMarker MarchCubeCompleteMarker = new("Voxel.Mesh.MarchCubeJobComplete");
    private static readonly Unity.Profiling.ProfilerMarker CompactMeshMarker = new("Voxel.Mesh.CompactMeshData");
    private static readonly Unity.Profiling.ProfilerMarker UploadMeshMarker = new("Voxel.Mesh.UploadMeshData");
    private const float IsoLevel = 0.5f;

    private readonly int _maxVertices;
    private readonly int _voxelCount;
    private readonly int _jobCount;
    private NativeArray<float3> vertices;
    private NativeArray<ushort> triangles;
    private NativeArray<float3> normals;

    private NativeArray<float> cubeValues;
    private NativeArray<float3> cubePositions;
    private NativeArray<float3> edgeVertices;

    private NativeArray<int> hasSurface;

    private Mesh mesh;

    public MarchingCubesChunkMeshGenerator(int voxelsPerChunk, int jobCount)
    {
        _maxVertices = 15 * voxelsPerChunk * voxelsPerChunk * voxelsPerChunk;
        _voxelCount = voxelsPerChunk * voxelsPerChunk * voxelsPerChunk;
        _jobCount = jobCount;
        vertices = new NativeArray<float3>(_maxVertices, Allocator.Persistent);
        triangles = new NativeArray<ushort>(_maxVertices, Allocator.Persistent);
        normals = new NativeArray<float3>(_maxVertices, Allocator.Persistent);

        cubeValues = new NativeArray<float>(8 * _voxelCount, Allocator.Persistent);
        cubePositions = new NativeArray<float3>(8 * _voxelCount, Allocator.Persistent);
        edgeVertices = new NativeArray<float3>(12 * _voxelCount, Allocator.Persistent);

        hasSurface = new NativeArray<int>(1, Allocator.Persistent);
    }

    public Mesh GenerateMesh(VoxelChunk chunk)
    {
        using Unity.Profiling.ProfilerMarker.AutoScope _ = GenerateMeshMarker.Auto();

        hasSurface[0] = 0;

        using (ClearBuffersMarker.Auto())
        {
            vertices.FillArray(float3.zero);
            normals.FillArray(float3.zero);
            triangles.FillArray(default);
        }

        var job = new MarchCubeJob
        {
            VoxelsPerChunk = chunk.VoxelsPerChunk,
            VoxelSize = chunk.VoxelSize,

            PaddedDensity = chunk.Padding.PaddedDensity,
            PaddingSize = chunk.Padding.PaddingSize,
            PaddedSize = chunk.Padding.PaddedSize,

            Vertices = vertices,
            Triangles = triangles,
            Normals = normals,

            CubeValues = cubeValues,
            CubePositions = cubePositions,
            EdgeVertices = edgeVertices,

            HasSurface = hasSurface,
        };

        JobHandle handle = job.Schedule(_voxelCount, _jobCount);
        using (MarchCubeCompleteMarker.Auto())
        {
            handle.Complete();
        }

        if (hasSurface[0] == 0)
            return null;

        int writeIndex = 0;

        using (CompactMeshMarker.Auto())
        {
            for (int i = 0; i < _maxVertices; i += 3)
            {
                float3 v0 = vertices[i];
                float3 v1 = vertices[i + 1];
                float3 v2 = vertices[i + 2];

                if (v0.Equals(float3.zero) &&
                    v1.Equals(float3.zero) &&
                    v2.Equals(float3.zero))
                    continue;

                vertices[writeIndex] = v0;
                vertices[writeIndex + 1] = v1;
                vertices[writeIndex + 2] = v2;

                triangles[writeIndex] = (ushort)writeIndex;
                triangles[writeIndex + 1] = (ushort)(writeIndex + 1);
                triangles[writeIndex + 2] = (ushort)(writeIndex + 2);

                normals[writeIndex] = normals[i];
                normals[writeIndex + 1] = normals[i + 1];
                normals[writeIndex + 2] = normals[i + 2];

                writeIndex += 3;
            }
        }

        using (UploadMeshMarker.Auto())
        {
            mesh = new Mesh();

            mesh.SetVertexBufferParams(
                writeIndex,
                new VertexAttributeDescriptor(
                    VertexAttribute.Position,
                    VertexAttributeFormat.Float32,
                    3
                )
            );

            mesh.SetIndexBufferParams(
                writeIndex,
                mesh.indexFormat
            );

            mesh.SetVertexBufferData(vertices, 0, 0, writeIndex, 0, MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndexBufferData(triangles, 0, 0, writeIndex, MeshUpdateFlags.DontValidateIndices);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, writeIndex, MeshTopology.Triangles), MeshUpdateFlags.DontValidateIndices);
            mesh.SetNormals(normals, 0, writeIndex);

            Vector3 center = Vector3.one * chunk.VoxelsPerChunk * chunk.VoxelSize * 0.5f;
            Vector3 boundsSize = Vector3.one * chunk.VoxelsPerChunk * chunk.VoxelSize;
            mesh.bounds = new Bounds(center, boundsSize);
        }

        return mesh;
    }
    [BurstCompile]
    public struct MarchCubeJob : IJobParallelFor
    {
        [ReadOnly] public int VoxelsPerChunk;
        [ReadOnly] public float VoxelSize;

        [ReadOnly] public NativeArray<float> PaddedDensity;
        [ReadOnly] public int PaddingSize;
        [ReadOnly] public int PaddedSize;

        [NativeDisableParallelForRestriction] public NativeArray<float3> Vertices;
        [NativeDisableParallelForRestriction] public NativeArray<ushort> Triangles;
        [NativeDisableParallelForRestriction] public NativeArray<float3> Normals;

        [NativeDisableParallelForRestriction] public NativeArray<float> CubeValues;
        [NativeDisableParallelForRestriction] public NativeArray<float3> CubePositions;
        [NativeDisableParallelForRestriction] public NativeArray<float3> EdgeVertices;

        [NativeDisableParallelForRestriction] public NativeArray<int> HasSurface;

        public void Execute(int index)
        {
            int offset8 = index * 8;
            int offset12 = index * 12;

            int z = index / (VoxelsPerChunk * VoxelsPerChunk);
            int rem = index - z * VoxelsPerChunk * VoxelsPerChunk;
            int y = rem / VoxelsPerChunk;
            int x = rem % VoxelsPerChunk;

            for (int i = 0; i < 8; i++)
            {
                int xi = x + MarchingTableBurst.VertexOffset[i * 3 + 0];
                int yi = y + MarchingTableBurst.VertexOffset[i * 3 + 1];
                int zi = z + MarchingTableBurst.VertexOffset[i * 3 + 2];

                CubeValues[offset8 + i] = GetVoxelValue(xi, yi, zi);
                CubePositions[offset8 + i] = new float3(xi, yi, zi);
            }

            int cubeIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (CubeValues[offset8 + i] < IsoLevel)
                    cubeIndex |= 1 << i;
            }

            int edges = MarchingTableBurst.CubeEdgeFlags[cubeIndex];

            if (edges == 0)
                return;

            HasSurface[0] = 1;

            for (int i = 0; i < 12; i++)
            {
                if ((edges & (1 << i)) != 0)
                {
                    int v1 = MarchingTableBurst.EdgeConnection[i * 2 + 0];
                    int v2 = MarchingTableBurst.EdgeConnection[i * 2 + 1];

                    EdgeVertices[offset12 + i] = InterpolateEdgeVertex(IsoLevel,
                        CubePositions[offset8 + v1], CubePositions[offset8 + v2],
                        CubeValues[offset8 + v1], CubeValues[offset8 + v2]);
                }
            }

            for (int i = 0; MarchingTableBurst.TriangleConnectionTable[cubeIndex * 16 + i] != -1; i += 3)
            {
                int index0 = MarchingTableBurst.TriangleConnectionTable[cubeIndex * 16 + i];
                int index1 = MarchingTableBurst.TriangleConnectionTable[cubeIndex * 16 + i + 1];
                int index2 = MarchingTableBurst.TriangleConnectionTable[cubeIndex * 16 + i + 2];

                Vertices[index * 15 + i] = EdgeVertices[offset12 + index0] * VoxelSize;
                Vertices[index * 15 + i + 1] = EdgeVertices[offset12 + index1] * VoxelSize;
                Vertices[index * 15 + i + 2] = EdgeVertices[offset12 + index2] * VoxelSize;

                Triangles[index * 15 + i] = (ushort)(index * 15 + i);
                Triangles[index * 15 + i + 1] = (ushort)(index * 15 + i + 1);
                Triangles[index * 15 + i + 2] = (ushort)(index * 15 + i + 2);

                Normals[index * 15 + i] = CalculateNormal(EdgeVertices[offset12 + index0]);
                Normals[index * 15 + i + 1] = CalculateNormal(EdgeVertices[offset12 + index1]);
                Normals[index * 15 + i + 2] = CalculateNormal(EdgeVertices[offset12 + index2]);
            }
        }

        private Vector3 InterpolateEdgeVertex(float isoLevel, Vector3 p1, Vector3 p2, float val1, float val2)
        {
            if (Mathf.Abs(isoLevel - val1) < 0.00001f)
                return p1;
            if (Mathf.Abs(isoLevel - val2) < 0.00001f)
                return p2;
            if (Mathf.Abs(val1 - val2) < 0.00001f)
                return p1;

            return p1 + (isoLevel - val1) * (p2 - p1) / (val2 - val1);
        }

        private Vector3 CalculateNormal(float3 pos)
        {
            float dx = SampleDensity(pos + new float3(1, 0, 0))
                     - SampleDensity(pos - new float3(1, 0, 0));

            float dy = SampleDensity(pos + new float3(0, 1, 0))
                     - SampleDensity(pos - new float3(0, 1, 0));

            float dz = SampleDensity(pos + new float3(0, 0, 1))
                     - SampleDensity(pos - new float3(0, 0, 1));

            float3 normal = new float3(dx, dy, dz);
            if (math.lengthsq(normal) < 0.000001f)
                return Vector3.up;

            return -math.normalize(normal);
        }

        private float SampleDensity(float3 pos)
        {
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);
            int z = Mathf.RoundToInt(pos.z);

            return GetVoxelValue(x, y, z);
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
    }

    public void Dispose()
    {
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();
        if (normals.IsCreated) normals.Dispose();

        if (cubeValues.IsCreated) cubeValues.Dispose();
        if (cubePositions.IsCreated) cubePositions.Dispose();
        if (edgeVertices.IsCreated) edgeVertices.Dispose();

        if (hasSurface.IsCreated) hasSurface.Dispose();
    }
}








