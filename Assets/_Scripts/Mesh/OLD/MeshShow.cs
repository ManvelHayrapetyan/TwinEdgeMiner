using System;
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
    [SerializeField, Range(0f, 180f)] private float _alpha;

    private VoxelData _voxelData;
    private MarchingCubesMeshGenerator _meshGenerator = new();
    private CubeVoxelGenerator _cubeVoxelGenerator = new();

    private Mesh _mesh;
    private MeshFilter _meshFilter  ;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Material _material;

    private Texture3D _crackTex;
    private Color[] _crackColors;
    private int texW => _voxelData.Width;
    private int texH => _voxelData.Height * _voxelData.Depth;

    private void Awake()
    {
        _voxelData = new VoxelData(_width, _height, _depth, _voxelSize, _maxStability, _maxDurability);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        _material = _meshRenderer.material;
        _material.SetFloat("_VoxelSize", _voxelSize);
        _material.SetVector("_BoundsMax", new Vector3(_width, _height, _depth));

        _cubeVoxelGenerator.Fill(_voxelData);
    }

    private void Start()
    {
        _crackTex = new Texture3D(_width, _height, _depth, TextureFormat.RGBA32, false);
        _crackColors = new Color[_width * _height * _depth];
        _crackTex.SetPixels(_crackColors);
        _crackTex.Apply();
        _material.SetTexture("_CrackTex", _crackTex);
        UpdateMesh();
    }

    public void ApplyVoxelDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        _voxelData.ApplyDamage(localPosition, radius, stabilityDamage, durabilityDamage, out bool OreTouched);
        if (OreTouched)
        {
            Collider[] nearby = Physics.OverlapSphere(worldPosition, radius);
            foreach (var col in nearby)
            {
                if (col.TryGetComponent<IMinable>(out var ore))
                {
                    ore.ApplyDamage(stabilityDamage, durabilityDamage);
                }
            }
        }
        UpdateMesh();
    }

    public Vector3Int[] OreGroundInitialize(Vector3 center, float radius)
    {
        return _voxelData.AdjacentOreIndexInitialize(transform.InverseTransformPoint(center), radius);
    }

    private void UpdateMesh()
    {
        _mesh = _meshGenerator.GenerateMesh(_voxelData);
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
    }

    public void ApplyCrack(Vector3 hitPoint, Vector3 center, float stability, float maxStability, Vector3Int[] groundVoxels)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localCenter = transform.InverseTransformPoint(center);
        Vector3 hitDirection = localHitPoint - localCenter;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        foreach (Vector3Int voxel in groundVoxels)
        {
            Vector3 voxelPos = (Vector3)voxel * _voxelSize + Vector3.one * (_voxelSize / 2f);
            Vector3 toVoxel = voxelPos - localCenter;
            if (Vector3.Dot(hitDirection, toVoxel) >= angle * hitDirection.magnitude * toVoxel.magnitude)
            {
                float crackPercent = maxStability != 0 ? stability / maxStability : 0;
                _voxelData.ApplyCrackToVoxel(1 - crackPercent, voxel);
                _crackColors[voxel.x + voxel.y * _width + voxel.z * _width * _height] = new Color(1 - crackPercent, 0,0,0);
            }
        }
        _crackTex.SetPixels(_crackColors);
        _crackTex.Apply();
        _material.SetTexture("_CrackTex", _crackTex);
    }

    internal void DestroyVoxelShellLayer(Vector3 hitPoint, Vector3 center, Vector3Int[] groundVoxels)
    {
        Debug.Log("Destroy layer");
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localCenter = transform.InverseTransformPoint(center);
        Vector3 hitDirection = localHitPoint - localCenter;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        foreach (Vector3Int voxel in groundVoxels)
        {
            Vector3 voxelPos = (Vector3)voxel * _voxelSize + Vector3.one * (_voxelSize / 2f);
            Vector3 toVoxel = (voxelPos - localCenter).normalized;
            if (Vector3.Dot(hitDirection, toVoxel) >= angle * hitDirection.magnitude * toVoxel.magnitude)
            {
                _voxelData.SetDurability(voxel.x, voxel.y, voxel.z, 0);
            }
            else
            {
                _voxelData.DestroyCracked(voxel);
            }
        }
    }

    internal void DestroyAllVoxels(Vector3Int[] groundVoxels)
    {
        foreach (Vector3Int voxel in groundVoxels)
        {
            _voxelData.SetDurability(voxel.x, voxel.y, voxel.z, 0);
        }
    }
}