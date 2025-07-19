using UnityEngine;
using Zenject;

public class PlayerLook : MonoBehaviour
{
    [Inject] private readonly InputActions _inputActions;

    [SerializeField] private float _mouseSensitivity = 2f;
    [SerializeField] private float _maxPitchUp = 60f;
    [SerializeField] private float _maxPitchDown = -60f;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Transform _cameraPivot;

    private Vector2 _lookInput;
    private float _xRotation;

    private void OnEnable()
    {
        CursorManager.Lock(); // is temporary
        _inputActions.Player.Look.performed += OnLookPerformed;
        _inputActions.Player.Look.canceled += OnLookCanceled;
    }

    private void Update()
    {
        float mouseX = _lookInput.x * _mouseSensitivity;
        float mouseY = _lookInput.y * _mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, _maxPitchDown, _maxPitchUp);
        _cameraPivot.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        _playerBody.Rotate(Vector3.up * mouseX);
    }

    private void OnDisable()
    {
        _inputActions.Player.Look.performed -= OnLookPerformed;
        _inputActions.Player.Look.canceled -= OnLookCanceled;
    }

    private void OnLookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnLookCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        _lookInput = Vector2.zero;
    }
}