using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerInteract : MonoBehaviour
{
    [Inject] private readonly GameData _gameData;
    [Inject] private readonly LookAtTargetDetector _lookAtTargetDetector;
    [Inject] private readonly InputActions _inputActions;

    private void OnEnable()
    {
        _inputActions.Gameplay.Interact.performed += Interact;
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.Interact.performed -= Interact;
    }

    private void Interact(InputAction.CallbackContext ctx)
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (_lookAtTargetDetector.TryRaycast(out _, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<IPickable>(out var pickable))
            {
                pickable.TryPick(_gameData.Inventory);
            }
            else if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }
}
