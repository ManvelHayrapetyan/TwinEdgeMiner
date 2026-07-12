using Unity.Collections;
using UnityEngine;

public class VoxelChunkRenderer : MonoBehaviour, IVoxelDamageable
{
    private static readonly Unity.Profiling.ProfilerMarker UpdateMeshMarker = new("Voxel.Renderer.UpdateMesh");
    private static readonly Unity.Profiling.ProfilerMarker SetMeshFilterMarker = new("Voxel.Renderer.SetMeshFilter");
    private static readonly Unity.Profiling.ProfilerMarker SetMeshColliderMarker = new("Voxel.Renderer.SetMeshCollider");
    private static readonly Unity.Profiling.ProfilerMarker UpdateCrackTextureMarker = new("Voxel.Renderer.UpdateCrackTexture");
    private const int MeshGenerationJobCount = 64;

    private VoxelChunk _voxelChunk;
    private VoxelChunkManager _manager;
    private MarchingCubesChunkMeshGenerator _meshGenerator;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Material _material;

    private Texture3D _crackTex;
    private Color[] _crackColors;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        _material = _meshRenderer.material;

        _manager = GetComponentInParent<VoxelChunkManager>();
    }

    public void UpdateMesh()
    {
        using Unity.Profiling.ProfilerMarker.AutoScope _ = UpdateMeshMarker.Auto();

        _mesh = _meshGenerator.GenerateMesh(_voxelChunk);

        if (_mesh == null)
        {
            using (SetMeshFilterMarker.Auto())
            {
                _meshFilter.sharedMesh = null;
            }

            using (SetMeshColliderMarker.Auto())
            {
                _meshCollider.sharedMesh = null;
            }
            return;
        }

        using (SetMeshFilterMarker.Auto())
        {
            _meshFilter.mesh = _mesh;
        }

        using (SetMeshColliderMarker.Auto())
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
        }
    }
    public void Init(VoxelChunk voxelChunk)
    {
        _voxelChunk = voxelChunk;
        _meshGenerator?.Dispose();
        _meshGenerator = new MarchingCubesChunkMeshGenerator(_voxelChunk.VoxelsPerChunk, MeshGenerationJobCount);

        _material.SetFloat("_VoxelSize", _voxelChunk.VoxelSize);
        _material.SetVector("_BoundsMax", Vector3.one * _voxelChunk.VoxelsPerChunk);

        _crackTex = new Texture3D(
            _voxelChunk.VoxelsPerChunk,
            _voxelChunk.VoxelsPerChunk,
            _voxelChunk.VoxelsPerChunk,
            TextureFormat.RGBA32,
            false);
        _crackColors = new Color[_voxelChunk.VoxelsPerChunk * _voxelChunk.VoxelsPerChunk * _voxelChunk.VoxelsPerChunk];
        _crackTex.SetPixels(_crackColors);
        _crackTex.Apply();
        _material.SetTexture("_CrackTex", _crackTex);
    }

    public void UpdateGO(VoxelChunk voxelChunk)
    {
        _voxelChunk = voxelChunk;
    }

    public void UpdateGO(VoxelChunk voxelChunk, NativeArray<Color32> color)
    {
        _crackTex.SetPixelData(color, 0);
        _crackTex.Apply(false);
        _material.SetTexture("_CrackTex", _crackTex);
        _material.SetFloat("_VoxelSize", voxelChunk.VoxelSize);
        _material.SetVector("_BoundsMax", Vector3.one * voxelChunk.VoxelsPerChunk);
    }

    public void ApplyVoxelDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        _manager.ApplyDamage(worldPosition, radius, stabilityDamage, durabilityDamage);
    }

    private void OnDestroy()
    {
        _meshGenerator?.Dispose();
    }
}








