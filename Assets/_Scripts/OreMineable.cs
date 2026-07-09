using UnityEngine;
using Zenject;

public enum OreDamageResult
{
    None,
    CrackChanged,
    LayerDestroyed,
    FullyMined
}

[RequireComponent(typeof(Rigidbody))]
public class OreMineable : MonoBehaviour, IMinable, IPickable
{
    public float Stability => _stability;
    public float Durability => _durability;
    public float MaxStability => _maxStability;
    public float MaxDurability => _maxDurability;

    public float Radius => _groundRadius;

    public Vector3 Center => _center;

    [Inject] private readonly MeshShow _meshShow;

    [SerializeField] private float _maxStability = 100f;
    [SerializeField] private float _maxDurability = 100f;
    [SerializeField] private int _maxStagesToDestroy = 3;
    [SerializeField] private float _groundRadiusScale = 10f;
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
        _center = transform.position;
    }

    public OreDamageResult ApplyDamage(float stabilityDamage, float durabilityDamage)
    {
        if (_canBePicked) 
            return OreDamageResult.None;

        _stability = Mathf.Max(0, _stability - stabilityDamage);
        float durabilityReduction = (_maxStability == 0)
            ? durabilityDamage
            : durabilityDamage * (_maxStability - _stability) / _maxStability;
        _durability = Mathf.Max(0, _durability - durabilityReduction);

        OreDamageResult oreDamageResult = OreDamageResult.CrackChanged;

        if (_durability <= 0f)
        {
            _stability = _maxStability;
            _durability = _maxDurability;
            _stagesToDestroy -= 1;
            oreDamageResult = OreDamageResult.LayerDestroyed;
        }
        if (_stagesToDestroy <= 0)
        {
            oreDamageResult = OreDamageResult.FullyMined;
            _canBePicked = true;
            _rb.isKinematic = false;
        }

        return oreDamageResult;
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
