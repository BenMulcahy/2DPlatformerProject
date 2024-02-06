using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnInputRemoved;
        InputSystem.onLayoutChange += OnLayoutChanged;
        if (Player.Instance) Player.Instance.PlayerInputComponent.onControlsChanged += OnPlayerControlChanged;
        else Debug.LogWarning(this + " - Player not found during enable");
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnInputRemoved;
        InputSystem.onLayoutChange -= OnLayoutChanged;
        if (Player.Instance) Player.Instance.PlayerInputComponent.onControlsChanged -= OnPlayerControlChanged;
        else Debug.LogWarning(this + " - Player not found during disable");
    }

    private void OnPlayerControlChanged(PlayerInput input)
    {
        Debug.Log("Controls changed, now using: " + input.currentControlScheme);
    }

    private void OnLayoutChanged(string arg1, InputControlLayoutChange change)
    {
        Debug.Log("Layout Change!");
    }

    private void OnInputRemoved(InputDevice device, InputDeviceChange change)
    {
        if(change == InputDeviceChange.Removed)
        {
            Debug.Log("Device Removed: " +  device);
            //TODO: Pause Game
            GameManager.Instance.PauseGame();
        }
    }
}
