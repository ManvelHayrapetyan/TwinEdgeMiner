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
    [SerializeField] private Material _material;
    [SerializeField, Range(0f, 180f)] private float _alpha;

    private VoxelData _voxelData;
    private MarchingCubesMeshGenerator _meshGenerator = new();
    private CubeVoxelGenerator _cubeVoxelGenerator = new();

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    private Texture3D _crackTex;
    private int texW => _voxelData.Width;
    private int texH => _voxelData.Height * _voxelData.Depth;

    private void Awake()
    {
        _voxelData = new VoxelData(_width, _height, _depth, _voxelSize, _maxStability, _maxDurability);

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        _material.SetTexture("CrackTex", _crackTex);
        _material.SetVector("VoxelCount", new Vector4(_voxelData.Width, _voxelData.Height, _voxelData.Depth, 0));
        _material.SetFloat("VoxelSize", _voxelSize);

        _meshRenderer.material = _material;
        _voxelData.Test();
        _cubeVoxelGenerator.Fill(_voxelData);
    }

    private void Start()
    {
        UpdateCrackTextureTest();
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
                    ore.ApplyDamage(worldPosition, stabilityDamage, durabilityDamage);
                }
            }
        }
        UpdateMesh();
    }

    public Vector3Int[] OreGroundInitialize(Vector3 center, float radius)
    {
        Debug.Log("OreGroundInitialize");
        return _voxelData.AdjacentOreIndexInitialize(transform.InverseTransformPoint(center), radius);
    }

    private void UpdateMesh()
    {
        _mesh = _meshGenerator.GenerateMesh(_voxelData);
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
        _voxelData.Test();
    }

    public void ApplyCrack(Vector3 hitPoint, Vector3 center, float stability, float maxStability, Vector3Int[] groundVoxels)
    {
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localCenter = transform.InverseTransformPoint(center);
        Vector3 hitDirection = (localHitPoint - localCenter).normalized;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        foreach (Vector3Int voxel in groundVoxels)
        {
            Vector3 voxelPos = (Vector3)voxel * _voxelSize + Vector3.one * (_voxelSize / 2f);
            Vector3 toVoxel = (voxelPos - localCenter).normalized;
            if (Vector3.Dot(hitDirection, toVoxel) >= angle)
            {
                float crackPercent = maxStability != 0 ? stability / maxStability : 0;
                _voxelData.ApplyCrackToVoxel(1 - crackPercent, voxel);
                Debug.Log(_voxelData.GetCrackPercent(voxel.x, voxel.y, voxel.z));
            }
        }
        UpdateCrackTexture();
    }

    internal void DestroyVoxelShellLayer(Vector3 hitPoint, Vector3 center, Vector3Int[] groundVoxels)
    {
        Debug.Log("Destroy layer");
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localCenter = transform.InverseTransformPoint(center);
        Vector3 hitDirection = (localHitPoint - localCenter).normalized;
        float angle = Mathf.Cos(_alpha * Mathf.Deg2Rad);
        foreach (Vector3Int voxel in groundVoxels)
        {
            Vector3 voxelPos = (Vector3)voxel * _voxelSize + Vector3.one * (_voxelSize / 2f);
            Vector3 toVoxel = (voxelPos - localCenter).normalized;
            if (Vector3.Dot(hitDirection, toVoxel) >= angle)
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

    void UpdateCrackTexture()
    {
        Color[] colors = new Color[_width * _height * _depth];

        int i = 0;
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                for (int z = 0; z < _depth; z++, i++)
                {
                    float v = _voxelData.GetCrackPercent(x, y, z);
                    colors[i] = new Color(v, v, v, 1);
                    if (v != 0)
                        Debug.Log("UpdateCrackTexture" + v);
                }

        _crackTex = new Texture3D(_width, _height, _depth, TextureFormat.RFloat, false);
        _crackTex.SetPixels(colors);
        _crackTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        _crackTex.anisoLevel = 0;
        _crackTex.wrapMode = TextureWrapMode.Clamp;
        _crackTex.filterMode = FilterMode.Point;

        _material.SetTexture("CrackTex", _crackTex);
        _material.SetFloat("VoxelSize", _voxelSize);
        _material.SetVector("WorldSize", new Vector3(_width, _height, _depth) * _voxelSize);

    }

    void UpdateCrackTextureTest()
    {

        //ComputeBuffer voxelDataBuffer = new ComputeBuffer(count, sizeof(float));

        Color[] colors = new Color[_width * _height * _depth];

        int i = 0;
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                for (int z = 0; z < _depth; z++, i++)
                {
                    colors[i] = new Color(1, 1, 0, 1);
                }

        _crackTex = new Texture3D(_width, _height, _depth, TextureFormat.RFloat, false);
        _crackTex.SetPixels(colors);

        _crackTex.wrapMode = TextureWrapMode.Clamp;
        _crackTex.filterMode = FilterMode.Bilinear;
        _crackTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        _crackTex.anisoLevel = 0;
        _crackTex.wrapMode = TextureWrapMode.Clamp;
        _crackTex.filterMode = FilterMode.Point;

        _material.SetTexture("CrackTex", _crackTex);
        _material.SetFloat("VoxelSize", _voxelSize);
        _material.SetVector("WorldSize", new Vector3(_width, _height, _depth) * _voxelSize);
        _material.SetVector("WorldSize2", new Vector3(_width, _height, _depth));


        Texture2D _crackTex2D = new Texture2D(_width, _height * _depth, TextureFormat.RGBA32, false);
        _crackTex2D.wrapMode = TextureWrapMode.Clamp;
        _crackTex2D.filterMode = FilterMode.Bilinear;

        i = 0;
        for (int z = 0; z < _depth; z++)
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++, i++)
                {
                    _crackTex2D.SetPixel(x, y + z * _height, new Color(1, 0, 0, 1));
                }
        _crackTex2D.Apply();
        _material.SetTexture("_CrackTex2D", _crackTex2D);

        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < 256; x++)
        {
            float t = x / (float)(256 - 1);
            Color color = Color.Lerp(Color.red, Color.blue, t);
            for (int y = 0; y < 256; y++)
            {
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        _material.SetTexture("_Test", tex);
        _material.SetTexture("_Test2", tex);

        Texture3D texTry = new Texture3D(16, 16, 16, TextureFormat.RGBA32, false);
        texTry.wrapMode = TextureWrapMode.Clamp;
        texTry.filterMode = FilterMode.Point;

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    texTry.SetPixel(x, y, z, new Color(0, y / 16f, z / 16f, 1));
                }
            }
        }

        texTry.Apply();
        _material.SetTexture("_TestTry3d", texTry);

    }
}