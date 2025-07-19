using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Inject] private readonly InputActions _inputActions;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 5f;

    private Rigidbody _rb;
    private CapsuleCollider _capsuleCollider;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void OnEnable()
    {
        _inputActions.Player.Move.performed += OnMovePerformed;
        _inputActions.Player.Move.canceled += OnMoveCanceled;
        _inputActions.Player.Jump.performed += Jump;
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMovePerformed;
        _inputActions.Player.Move.canceled -= OnMoveCanceled;
        _inputActions.Player.Jump.performed -= Jump;
    }

    private void Move()
    {
        Vector3 moveDir = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        Vector3 targetVelocity = moveDir * _moveSpeed;
        Vector3 velocity = _rb.velocity;
        Vector3 velocityChange = targetVelocity - velocity;
        velocityChange.y = 0f;

        _rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }


    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveInput = Vector2.zero;
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (IsGrounded())
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        return Physics.SphereCast(transform.position, _capsuleCollider.radius,
            Vector3.down, out _, _capsuleCollider.height / 2 + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
}