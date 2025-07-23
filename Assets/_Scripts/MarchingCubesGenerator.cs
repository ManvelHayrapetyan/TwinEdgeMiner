using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesGenerator : MonoBehaviour
{
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private float noiseHeight = 8f;
    [SerializeField] private Vector3 noiseOffset = Vector3.zero;

    [SerializeField] private int width = 16;
    [SerializeField] private int height = 16;
    [SerializeField] private int depth = 16;
    [SerializeField] private float voxelSize = 1f;
    [SerializeField] private float materialScale = 0.1f;
    [SerializeField] private Material material;


    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private float AirDensity = 0f;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        GenerateMesh();
        _meshRenderer.material = material;
    }

    private void GenerateMesh()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();

        float[,,] density = new float[width + 1, height + 1, depth + 1];

        for (int x = 0; x < width + 1; x++)
            for (int y = 0; y < height + 1; y++)
                for (int z = 0; z < depth + 1; z++)
                {
                    float nx = (x + noiseOffset.x) * noiseScale;
                    float nz = (z + noiseOffset.z) * noiseScale;

                    float heightValue = Mathf.PerlinNoise(nx, nz) * noiseHeight;

                    density[x, y, z] = y < heightValue ? 1f : 0f;

                    //density[x, y, z] = 1f;
                    if (x == 0 || y == 0 || z == 0 || x == width || y == height || z == depth)
                    {
                        density[x, y, z] = 0f;
                    }
                }


        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = new Vector3(x, y, z) * voxelSize;
                    MarchCube(position, density, x, y, z, vertices, triangles, normals, uvs);
                }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray(),
            uv = uvs.ToArray()
        };

        mesh.RecalculateBounds();
        _meshFilter.mesh = mesh;
    }

    private void MarchCube(Vector3 position, float[,,] density, int x, int y, int z,
    List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs)
    {
        float isoLevel = 0.5f;

        float[] cubeValues = new float[8];
        Vector3[] cubePositions = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            int xi = x + MarchingTable.VertexOffset[i, 0];
            int yi = y + MarchingTable.VertexOffset[i, 1];
            int zi = z + MarchingTable.VertexOffset[i, 2];

            cubeValues[i] = density[xi, yi, zi];
            cubePositions[i] = new Vector3(xi, yi, zi) * voxelSize;
        }

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeValues[i] < isoLevel)
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

                edgeVertices[i] = InterpolateEdgeVertex(isoLevel,
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

            // Normals and UV
            Vector3 normal = CalculateNormal(edgeVertices[index0], density);
            normals.Add(normal);
            uvs.Add(CalculateUV(edgeVertices[index0], normal));

            normal = CalculateNormal(edgeVertices[index1], density);
            normals.Add(normal);
            uvs.Add(CalculateUV(edgeVertices[index1], normal));

            normal = CalculateNormal(edgeVertices[index2], density);
            normals.Add(normal);
            uvs.Add(CalculateUV(edgeVertices[index2], normal));
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

    private Vector3 CalculateNormal(Vector3 pos, float[,,] density)
    {
        float dx = SampleDensity(density, pos + new Vector3(voxelSize, 0, 0))
                 - SampleDensity(density, pos - new Vector3(voxelSize, 0, 0));

        float dy = SampleDensity(density, pos + new Vector3(0, voxelSize, 0))
                 - SampleDensity(density, pos - new Vector3(0, voxelSize, 0));

        float dz = SampleDensity(density, pos + new Vector3(0, 0, voxelSize))
                 - SampleDensity(density, pos - new Vector3(0, 0, voxelSize));

        return -new Vector3(dx, dy, dz).normalized;
    }

    private float SampleDensity(float[,,] density, Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / voxelSize);
        int y = Mathf.RoundToInt(pos.y / voxelSize);
        int z = Mathf.RoundToInt(pos.z / voxelSize);

        if (x < 0 || x > width || y < 0 || y > height || z < 0 || z > depth)
            return AirDensity;

        return density[x, y, z];
    }

    private Vector2 CalculateUV(Vector3 pos, Vector3 n)
    {
        if (Mathf.Abs(n.y) >= Mathf.Abs(n.x) && Mathf.Abs(n.y) >= Mathf.Abs(n.z))
        {
            // Up/Down
            return new Vector2(pos.x, pos.z) * materialScale;
        }
        else if (Mathf.Abs(n.x) >= Mathf.Abs(n.y) && Mathf.Abs(n.x) >= Mathf.Abs(n.z))
        {
            //  X
            return new Vector2(pos.z, pos.y) * materialScale;
        }
        else
        {
            //  Z
            return new Vector2(pos.x, pos.y) * materialScale;
        }
        //Vector2 result =
        //    new Vector2(pos.z, pos.y) * n.x +
        //    new Vector2(pos.x, pos.z) * n.y +
        //    new Vector2(pos.x, pos.y) * n.z;
        //result.Normalize();
        //return result * materialScale;

        //Vector2 uvX = new Vector2(pos.z, pos.y) * n.x;
        //Vector2 uvY = new Vector2(pos.x, pos.z) * n.y;
        //Vector2 uvZ = new Vector2(pos.x, pos.y) * n.z;

        //float totalWeight = Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
        //if (totalWeight == 0) totalWeight = 1f;

        //Vector2 uv = (uvX + uvY + uvZ) / totalWeight;
        //uv *= materialScale;

        //return uv;
    }
}
