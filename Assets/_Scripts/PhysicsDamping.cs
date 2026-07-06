using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsDamping : MonoBehaviour
{
    [SerializeField] private float _drag = 1f;
    [SerializeField] private float _angularDrag = 1f;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearDamping = _drag;
        _rb.angularDamping = _angularDrag;
    }

}