using Palmmedia.ReportGenerator.Core;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class OreMeshShow : MonoBehaviour, IMinable
{
    [SerializeField] private int _width = 32;
    [SerializeField] private int _height = 32;
    [SerializeField] private int _depth = 32;
    [SerializeField] private float _voxelSize = 0.5f;
    [SerializeField] private float _maxStability = 0f;
    [SerializeField] private float _maxDurability = 20f;
    [SerializeField] private Material _material;
    [SerializeField] private OreMineable _oreMineable;
    [SerializeField, Min(0f)] private float _angleScore = 1;
    [SerializeField, Min(0f)] private float _distanceScore = 1;
    [SerializeField, Min(0f)] private float _surfaceDistanceScore = 1;

    private VoxelShellData _voxelShellData;
    private MarchingCubesMeshGenerator _meshGenerator = new();
    private CubeVoxelGenerator _cubeVoxelGenerator = new();

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Mesh _mesh;

    private void Awake()
    {
        _voxelShellData = new VoxelShellData(_width, _height, _depth, _voxelSize, _maxStability, _maxDurability,
            _angleScore, _distanceScore, _surfaceDistanceScore);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer.material = _material;
    }

    private void Start()
    {
        _cubeVoxelGenerator.Fill(_voxelShellData);
        UpdateMesh();
    }
    public void BreakVoxelShellLayer(Vector3 hitPoint, Vector3 hitDirection, int roundsToDestroy)
    {
        Vector3 localPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 voxelFloatPos = localPoint / _voxelShellData.VoxelSize;
        Vector3Int voxelStart = new(
            Mathf.Clamp(Mathf.FloorToInt(voxelFloatPos.x), 0, _voxelShellData.Width - 1),
            Mathf.Clamp(Mathf.FloorToInt(voxelFloatPos.y), 0, _voxelShellData.Height - 1),
            Mathf.Clamp(Mathf.FloorToInt(voxelFloatPos.z), 0, _voxelShellData.Depth - 1)
        );
        int targetCount = _width * _depth * _height / roundsToDestroy;

        _voxelShellData.BreakShellLayer(voxelStart, hitDirection, targetCount);
        UpdateMesh();
    }

    public void ApplyStabilityDamage(float amount)
    {
        _oreMineable.ApplyStabilityDamage(amount);
    }

    public void ApplyDurabilityDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        _oreMineable.ApplyDurabilityDamage(amount, hitPoint, hitDirection);
    }

    private void UpdateMesh()
    {
        _mesh = _meshGenerator.GenerateMesh(_voxelShellData);
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
    }
}