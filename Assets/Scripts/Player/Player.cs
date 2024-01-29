using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public IA_Default PlayerInputActions { get; private set; }

    [HideInInspector] public PlayerMovementComponent playerMovement;

    private void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(this);

        playerMovement = GetComponent<PlayerMovementComponent>();
        SetupInputs();

    }

    private void OnEnable()
    {
        PlayerInputActions.Enable();
    }

    private void OnDisable()
    {
        PlayerInputActions.Disable();
    }

    //All inputs processed here and passed to appropriate component
    #region Input Management

    void SetupInputs()
    {
        PlayerInputActions = new IA_Default(); //Create input action mapping

        //Move
        PlayerInputActions.Gameplay.Movement.performed += OnMovementInput;

        //Jump
        PlayerInputActions.Gameplay.Jump.performed += OnJumpInput;
        PlayerInputActions.Gameplay.Jump.canceled += OnJumpCancelled;

        //Sprinting
        PlayerInputActions.Gameplay.Sprint.performed += OnSprintInput;
        PlayerInputActions.Gameplay.Sprint.canceled += OnSprintInputCancel;
    }

    /* SPRINTING - Start */
    private void OnSprintInput(InputAction.CallbackContext context)
    {
        playerMovement.SetSprinting(true);
    }

    /* SPRINTING - Stop */
    private void OnSprintInputCancel(InputAction.CallbackContext context)
    {
        playerMovement.SetSprinting(false); 
    }

    /* MOVMENT  */
    private void OnMovementInput(InputAction.CallbackContext value)
    {
        //PlayerMovementComponent reads moveInput value directly in fixed update
    }

    /* JUMP  */
    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if(playerMovement.bCanJump) playerMovement.StartJump();
    }

    /* JUMP - Stop */
    private void OnJumpCancelled(InputAction.CallbackContext context)
    {
        playerMovement.StopJump();
    }
    #endregion
}
