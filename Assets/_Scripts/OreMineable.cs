using Unity.VisualScripting;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class OreMineable : MonoBehaviour, IMinable, IPickable
{
    public float Stability => _stability;
    public float Durability => _durability;

    [Inject] private readonly SignalBus _signalBus;

    [SerializeField] private float _maxStability = 100;
    [SerializeField] private float _maxDurability = 100;
    [SerializeField] private int _stagesToDestroy = 3;
    [SerializeField] private ItemSO _itemSO;

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
    }
    public void ApplyStabilityDamage(float stabilityDamage)
    {
        Debug.Log($"{_stability}  R  {stabilityDamage}");
        _stability -= stabilityDamage;
        _stability = Mathf.Max(0, _stability);
        Debug.Log($"{_stability}  R  {_durability}");
    }

    public void ApplyDurabilityDamage(float durabilityDamage)
    {
        // stability cute damage to object, for best DPS need first decrease stability
        _durability -= durabilityDamage * (_maxStability - _stability) / _maxStability;
        Debug.Log($"{_durability}  R  {durabilityDamage}");
        Debug.Log($"{_stability}  L  {_durability}");
        if (_durability <= 0f)
        {
            _stability = _maxStability;
            _durability = _maxDurability;
            _stagesToDestroy -= 1;
            // here call or create event who destroy part of mesh
        }
        if (_stagesToDestroy <= 0)
        {
            Debug.Log('a');
            //here full delete mesh of ground around ore and apply gravity for ore to drop a ground
            _canBePicked = true;
            _rb.isKinematic = false;
        }
    }

    public void TryPick(Inventory inventory)
    {
        // check if player have space in inventory
        // Destroy(this.gameObject);
        // and add to inventory if not, nothing happened
        bool picked = inventory.TryAddItem(_itemSO);
        if (picked && _canBePicked)
        {
            Destroy(gameObject);
            _signalBus.Fire(new SignalItemPicked(_itemSO));
        }
        else
        {
            // Play "inventory full" sound / feedback
        }
    }
}
