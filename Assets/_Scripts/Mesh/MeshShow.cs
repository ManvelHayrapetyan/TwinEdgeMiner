using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MeshShow : MonoBehaviour, IVoxelDamageable
{
    [SerializeField] private int _width = 32;
    [SerializeField] private int _height = 32;
    [SerializeField] private int _depth = 32;
    [SerializeField] private float _voxelSize = 0.5f;
    [SerializeField] private float _maxStability = 0f;
    [SerializeField] private float _maxDurability = 20f;
    [SerializeField] private Material _material;

    private VoxelData _voxelData;
    private MarchingCubesMeshGenerator _meshGenerator = new();
    private CubeVoxelGenerator _cubeVoxelGenerator = new();

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _voxelData = new VoxelData(_width, _height, _depth, _voxelSize, _maxStability, _maxDurability);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer.material = _material;
    }

    private void Start()
    {
        _cubeVoxelGenerator.Fill(_voxelData);
        UpdateMesh();
    }

    public void ApplyVoxelDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        _voxelData.ApplyDamage(localPosition, radius, stabilityDamage, durabilityDamage);
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        _mesh = _meshGenerator.GenerateMesh(_voxelData);
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
    }
}