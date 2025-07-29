using System;
using UnityEngine;

public class InputService : IDisposable
{

    private readonly InputActions _inputActions;

    public InputService(InputActions inputActions)
    {
        _inputActions = inputActions;
        _inputActions.Enable();
    }

    public void SwitchToGameplay()
    {
        _inputActions.UI.Disable();
        _inputActions.Gameplay.Enable();
        MouseLock();
    }

    public void SwitchToUI()
    {
        _inputActions.Gameplay.Disable();
        _inputActions.UI.Enable();
        MouseUnlock();
    }

    public void Dispose()
    {
        _inputActions.Disable();
    }

    private void MouseLock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void MouseUnlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
