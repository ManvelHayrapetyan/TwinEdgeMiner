using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class OreMineable : MonoBehaviour, IMinable, IPickable
{
    public float Stability => _stability;
    public float Durability => _durability;

    [Inject] private readonly MeshShow _meshShow;

    [SerializeField] private float _maxStability = 100f;
    [SerializeField] private float _maxDurability = 100f;
    [SerializeField] private int _maxStagesToDestroy = 3;
    [SerializeField] private float _groundRadiusScale = 4f;
    [SerializeField] private ItemSO _itemSO;

    private int _stagesToDestroy;
    private float _stability;
    private float _durability;
    private Rigidbody _rb;
    private bool _canBePicked = false;

    private Collider _col;
    private float _groundRadius;
    private Vector3 _center;

    private Vector3Int[] _groundVoxels;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _stability = _maxStability;
        _durability = _maxDurability;
        _stagesToDestroy = _maxStagesToDestroy;

        _col = GetComponent<Collider>();
        Vector3 size = _col.bounds.size;
        _groundRadius = Mathf.Max(size.x, size.y, size.z) * _groundRadiusScale;
        _center = transform.transform.position;
    }

    private void Start()
    {
        _groundVoxels = _meshShow.OreGroundInitialize(_center, _groundRadius);
    }

    public void ApplyDamage(Vector3 hitPoint, float stabilityDamage, float durabilityDamage)
    {
        if (_canBePicked) return;
        _stability = Mathf.Clamp(_stability - stabilityDamage, 0, _maxStability);
        if (_maxStability == 0)
            _durability = Mathf.Clamp(_durability - durabilityDamage,
                0, _maxDurability);
        else
            _durability = Mathf.Clamp(_durability - durabilityDamage *
                (_maxStability - _stability) / _maxStability,
                0, _maxDurability);

        // Here call change crack level
        _meshShow.ApplyCrack(hitPoint, _center, _stability, _maxStability, _groundVoxels);

        if (_durability <= 0f)
        {
            Debug.Log("Halo durability ijava ara");
            _stability = _maxStability;
            _durability = _maxDurability;
            _stagesToDestroy -= 1;
            // here call or create event who destroy part of mesh
            _meshShow.DestroyVoxelShellLayer(hitPoint, _center, _groundVoxels);
        }
        if (_stagesToDestroy <= 0)
        {
            Debug.Log("Ore Fully Mined");
            _meshShow.DestroyAllVoxels(_groundVoxels);
            _canBePicked = true;
            _rb.isKinematic = false;
        }
    }

    public void TryPick(Inventory inventory)
    {
        // check if player have space in inventory
        // Destroy(this.gameObject);
        // and add to inventory if not, nothing happened
        if (!_canBePicked) return;
        bool picked = inventory.TryAddItem(_itemSO);
        if (picked)
        {

            Destroy(gameObject);
        }
        else
        {
            // Play "inventory full" sound / feedback
        }
    }
}
