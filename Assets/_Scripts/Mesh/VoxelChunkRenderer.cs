using NUnit.Framework.Internal;
using Unity.Collections;
using UnityEngine;

public class VoxelChunkRenderer : MonoBehaviour, IVoxelDamageable
{
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
        _meshGenerator = new(8, 8, 8, 64);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        _material = _meshRenderer.material;

        _manager = GetComponentInParent<VoxelChunkManager>();
    }
    public void UpdateMesh()
    {
        _mesh = _meshGenerator.GenerateMesh(_voxelChunk);

        if (_mesh == null)
        {
            _meshFilter.sharedMesh = null;
            _meshCollider.sharedMesh = null;
            return;
        }
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
    }

    public void Init(VoxelChunk VoxelChunk)
    {
        _voxelChunk = VoxelChunk;
        _material.SetFloat("_VoxelSize", _voxelChunk.VoxelSize);
        _material.SetVector("_BoundsMax", new Vector3(
            _voxelChunk.Width,
            _voxelChunk.Height,
            _voxelChunk.Depth));

        _crackTex = new Texture3D(_voxelChunk.Width,
                                _voxelChunk.Height,
                                _voxelChunk.Depth,
                                TextureFormat.RGBA32, false);
        _crackColors = new Color[_voxelChunk.Width * _voxelChunk.Height * _voxelChunk.Depth];
        _crackTex.SetPixels(_crackColors);
        _crackTex.Apply();
        _material.SetTexture("_CrackTex", _crackTex);
    }
    public void UpdateGO(VoxelChunk VoxelChunk)
    {
        _voxelChunk = VoxelChunk;
    }

    public void UpdateGO(VoxelChunk VoxelChunk, NativeArray<Color32> color)
    {
        _crackTex.SetPixelData(color, 0);
        _crackTex.Apply(false);
        _material.SetTexture("_CrackTex", _crackTex);
        _material.SetFloat("_VoxelSize", VoxelChunk.VoxelSize);
        _material.SetVector("_BoundsMax", new Vector3(
                VoxelChunk.Width,
                VoxelChunk.Height,
                VoxelChunk.Depth));
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