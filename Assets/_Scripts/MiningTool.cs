using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class MiningTool : MonoBehaviour
{
    [Inject] private readonly InputActions _inputActions;

    [SerializeField] private float _range = 3f;
    [SerializeField] private float _destructionDamage = 5f; // LMB
    [SerializeField] private float _stabilityDamage = 10f; // RMB
    [SerializeField] private float _secondaryDamagePercent = 20f;
    [SerializeField] private Camera _camera;


    private void OnEnable()
    {
        _inputActions.Player.LMB.performed += OnLMB;
        _inputActions.Player.RMB.performed += OnRMB;
    }

    private void OnDisable()
    {
        _inputActions.Player.LMB.performed -= OnLMB;
        _inputActions.Player.RMB.performed -= OnRMB;
    }

    private void OnLMB(InputAction.CallbackContext ctx)
    {
        TryMine(_destructionDamage, _destructionDamage * _secondaryDamagePercent / 100);
    }

    private void OnRMB(InputAction.CallbackContext ctx)
    {
        TryMine(_stabilityDamage * _secondaryDamagePercent / 100, _stabilityDamage);
    }

    private void TryMine(float destructionDamage, float stabilityDamage)
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, _range))
        {
            if (hit.collider.TryGetComponent<IMineable>(out var mineable))
            {
                mineable.ApplyStabilityDamage(stabilityDamage);
                mineable.ApplyDurabilityDamage(destructionDamage);
                Debug.Log($"{nameof(destructionDamage)} = {destructionDamage}");
                Debug.Log($"{nameof(stabilityDamage)} = {stabilityDamage}");
            }
        }
    }
}
