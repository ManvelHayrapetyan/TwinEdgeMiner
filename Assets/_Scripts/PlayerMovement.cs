using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Inject] private readonly InputActions _inputActions;

    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private float _jumpForce = 5f;

    private Rigidbody _rb;
    private CapsuleCollider _capsuleCollider;
    private Vector2 _moveInput;
    private bool _isRunning = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

    }

    private void OnEnable()
    {
        _inputActions.Gameplay.Move.performed += OnMovePerformed;
        _inputActions.Gameplay.Move.canceled += OnMoveCanceled;
        _inputActions.Gameplay.Run.performed += OnRunPerformed;
        _inputActions.Gameplay.Run.canceled += OnRunCanceled;
        _inputActions.Gameplay.Jump.performed += Jump;
    }
    private void FixedUpdate()
    {
        Move();
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.Move.performed -= OnMovePerformed;
        _inputActions.Gameplay.Move.canceled -= OnMoveCanceled;
        _inputActions.Gameplay.Run.performed -= OnRunPerformed;
        _inputActions.Gameplay.Run.canceled -= OnRunCanceled;
        _inputActions.Gameplay.Jump.performed -= Jump;
    }

    private void Move()
    {
        float speed = _isRunning ? _runSpeed : _walkSpeed;
        Vector3 moveDir = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        Vector3 targetVelocity = moveDir * speed;
        Vector3 velocity = _rb.linearVelocity;
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

    private void OnRunPerformed(InputAction.CallbackContext ctx)
    {
        _isRunning = true;
    }

    private void OnRunCanceled(InputAction.CallbackContext ctx)
    {
        _isRunning = false;
    }

    private void Jump(InputAction.CallbackContext ctx)
    {
        if (IsGrounded())
        {
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        return Physics.SphereCast(
            transform.position + Vector3.up * 0.1f,
            _capsuleCollider.radius * 0.95f, 
            Vector3.down, 
            out _, 
            _capsuleCollider.bounds.extents.y + 0.05f,
            ~LayerMask.GetMask("Player"),
            QueryTriggerInteraction.Ignore);
    }
}