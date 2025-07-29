using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class MiningTool : MonoBehaviour
{
    [Inject] private readonly LookAtTargetDetector _lookAtTargetDetector;
    [Inject] private readonly InputActions _inputActions;
    [Inject] private readonly PlayerAndToolStats _stats;

    [SerializeField] private float _secondaryDamagePercent = 20f;

    private void OnEnable()
    {
        _inputActions.Gameplay.LMB.performed += OnLMB;
        _inputActions.Gameplay.RMB.performed += OnRMB;
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.LMB.performed -= OnLMB;
        _inputActions.Gameplay.RMB.performed -= OnRMB;
    }

    private void OnLMB(InputAction.CallbackContext ctx)
    {
        TryMine(_stats.MiningToolDestructionDamage, _stats.MiningToolDestructionDamage * _secondaryDamagePercent / 100);
    }

    private void OnRMB(InputAction.CallbackContext ctx)
    {
        TryMine(_stats.MiningToolStabilityDamage * _secondaryDamagePercent / 100, _stats.MiningToolStabilityDamage);
    }

    private void TryMine(float destructionDamage, float stabilityDamage)
    {
        if (_lookAtTargetDetector.TryRaycast(out Ray ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<IMinable>(out var mineable))
            {
                mineable.ApplyStabilityDamage(stabilityDamage);
                mineable.ApplyDurabilityDamage(destructionDamage, hit.point, ray.direction.normalized);
                Debug.Log($"{nameof(destructionDamage)} = {destructionDamage}");
                Debug.Log($"{nameof(stabilityDamage)} = {stabilityDamage}");
            }
            if (hit.collider.TryGetComponent<IVoxelDamageable>(out var voxelDamageable))
            {
                voxelDamageable.ApplyVoxelDamage(hit.point, 3f, stabilityDamage, destructionDamage);
                Debug.Log($"Mesh {nameof(destructionDamage)} = {destructionDamage}");
                Debug.Log($"Mesh {nameof(stabilityDamage)} = {stabilityDamage}");
            }
        }
    }
}
