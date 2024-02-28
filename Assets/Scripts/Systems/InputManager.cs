using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    public string InputType { get; private set; }

    [Header("--- Rumble ---")]
    [SerializeField] float _rumbleLowFreq = 0.8f;
    [SerializeField] float _rumbleHighFreq = 1f;

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
        InputType = input.currentControlScheme;
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


    public void ControllerRumble(float intensity, float duration)
    {
        #if !UNITY_EDITOR //Only run when in builds -> allows for easier testing in engine
        #endif
        if (InputType != "Gamepad") return;

        //TODO: Enque or overwrite ongoing rumble if new rumble?
        Gamepad.current.SetMotorSpeeds(_rumbleLowFreq * intensity, _rumbleHighFreq * intensity);
        Invoke(nameof(StopRumble), duration);
    }

    public void StopRumble()
    {
        if(Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0,0);

        }
    }
}
