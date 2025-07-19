using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OreMineable : MonoBehaviour, IMineable, IPickable
{
    public float Stability => _stability;
    public float Durability => _durability;

    [SerializeField] private float _maxStability = 100;
    [SerializeField] private float _maxDurability = 100;
    [SerializeField] private int _stagesToDestroy = 3;

    private float _stability;
    private float _durability;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _stability = _maxStability;
        _durability = _maxDurability;
    }
    public void ApplyStabilityDamage(float stabilityDamage)
    {
        _stability -= stabilityDamage;
    }

    public void ApplyDurabilityDamage(float durabilityDamage)
    {
        // stability cute damage to object, for best DPS need first decrease stability
        _durability -= durabilityDamage * _stability / _maxStability;
        if (_durability <= 0f)
        {
            _stability = _maxStability;
            _durability = _maxDurability;
            _stagesToDestroy -= 1;
            // here call or create event who destroy part of mesh
        }
        if (_stagesToDestroy == 0)
        {
            //here full delete mesh of ground around ore and apply gravity for ore to drop a ground
            _rb.useGravity = true;
            TryPick();
        }
    }

    public void TryPick()
    {
        // check if player have space in inventory
        // Destroy(this.gameObject);
        // and add to inventory if not, nothing happened
    }
}
