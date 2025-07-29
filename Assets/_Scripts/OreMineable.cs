using Unity.VisualScripting;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class OreMineable : MonoBehaviour, IMinable, IPickable
{
    [SerializeField] private OreMeshShow _oreMeshShowTest;
    [SerializeField] private float _maxStability = 100;
    [SerializeField] private float _maxDurability = 100;
    [SerializeField] private int _stagesToDestroy = 3;
    [SerializeField] private ItemSO _itemSO;

    private int _maxStagesToDestroy;
    private float _stability;
    private float _durability;
    private Rigidbody _rb;
    private bool _canBePicked = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _stability = _maxStability;
        _durability = _maxDurability;
        _maxStagesToDestroy = _stagesToDestroy;
    }
    public void ApplyStabilityDamage(float stabilityDamage)
    {
        _stability = Mathf.Clamp(_stability - stabilityDamage, 0, _maxStability);
    }

    public void ApplyDurabilityDamage(float durabilityDamage, Vector3 hitPoint, Vector3 hitDirection)
    {
        // stability cute damage to object, for best DPS need first decrease stability
        if (_stability == 0)
            _durability = Mathf.Clamp(_durability - durabilityDamage, 0, _maxDurability);
        else
            _durability = Mathf.Clamp(_durability - durabilityDamage *
                (_maxStability - _stability) / _maxStability,
                0, _maxDurability);

        if (_durability <= 0f)
        {
            _stability = _maxStability;
            _durability = _maxDurability;
            _stagesToDestroy -= 1;
            // here call or create event who destroy part of mesh
            _oreMeshShowTest.BreakVoxelShellLayer(hitPoint, hitDirection, _maxStagesToDestroy);
        }
        if (_stagesToDestroy <= 0)
        {
            Debug.Log('a');
            //here full delete mesh of ground around ore and apply gravity for ore to drop a ground
            Destroy(_oreMeshShowTest.gameObject);
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
