using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesChunkMeshGenerator
{
    private const float AirDensity = 0f;
    private const float IsoLevel = 0.5f;
    public Mesh GenerateMesh(VoxelChunk chunk)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();

        for (int x = 0; x < chunk.Width; x++)
            for (int y = 0; y < chunk.Height; y++)
                for (int z = 0; z < chunk.Depth; z++)
                {
                    Vector3 position = new Vector3(x, y, z) * chunk.VoxelSize;
                    MarchCube(position, chunk, x, y, z, vertices, triangles, normals);
                }
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] *= chunk.VoxelSize;

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray(),
        };
        return mesh;
    }
    private void MarchCube(Vector3 position, VoxelChunk chunk, int x, int y, int z,
        List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {

        float[] cubeValues = new float[8];
        Vector3[] cubePositions = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            int xi = x + MarchingTable.VertexOffset[i, 0];
            int yi = y + MarchingTable.VertexOffset[i, 1];
            int zi = z + MarchingTable.VertexOffset[i, 2];

            cubeValues[i] = chunk.GetVoxelValue(xi, yi, zi);
            cubePositions[i] = new Vector3(xi, yi, zi);
        }

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeValues[i] < IsoLevel)
                cubeIndex |= 1 << i;
        }

        int edges = MarchingTable.CubeEdgeFlags[cubeIndex];
        if (edges == 0)
            return;

        Vector3[] edgeVertices = new Vector3[12];
        for (int i = 0; i < 12; i++)
        {
            if ((edges & (1 << i)) != 0)
            {
                int v1 = MarchingTable.EdgeConnection[i, 0];
                int v2 = MarchingTable.EdgeConnection[i, 1];

                edgeVertices[i] = InterpolateEdgeVertex(IsoLevel,
                    cubePositions[v1], cubePositions[v2],
                    cubeValues[v1], cubeValues[v2]);
            }
        }

        for (int i = 0; MarchingTable.TriangleConnectionTable[cubeIndex, i] != -1; i += 3)
        {
            int index0 = MarchingTable.TriangleConnectionTable[cubeIndex, i];
            int index1 = MarchingTable.TriangleConnectionTable[cubeIndex, i + 1];
            int index2 = MarchingTable.TriangleConnectionTable[cubeIndex, i + 2];

            int vertIndex = vertices.Count;

            vertices.Add(edgeVertices[index0]);
            vertices.Add(edgeVertices[index1]);
            vertices.Add(edgeVertices[index2]);

            triangles.Add(vertIndex);
            triangles.Add(vertIndex + 1);
            triangles.Add(vertIndex + 2);

            Vector3 normal = CalculateNormal(edgeVertices[index0], chunk);
            normals.Add(normal);

            normal = CalculateNormal(edgeVertices[index1], chunk);
            normals.Add(normal);

            normal = CalculateNormal(edgeVertices[index2], chunk);
            normals.Add(normal);
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

    private Vector3 CalculateNormal(Vector3 pos, VoxelChunk chunk)
    {
        float dx = SampleDensity(chunk, pos + new Vector3(1, 0, 0))
                 - SampleDensity(chunk, pos - new Vector3(1, 0, 0));

        float dy = SampleDensity(chunk, pos + new Vector3(0, 1, 0))
                 - SampleDensity(chunk, pos - new Vector3(0, 1, 0));

        float dz = SampleDensity(chunk, pos + new Vector3(0, 0, 1))
                 - SampleDensity(chunk, pos - new Vector3(0, 0, 1));

        return -new Vector3(dx, dy, dz).normalized;
    }

    private float SampleDensity(VoxelChunk chunk, Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);

        return chunk.GetVoxelValue(x, y, z);
    }
}
