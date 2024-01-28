using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public IA_Default InputActions { get; private set; }

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
        InputActions.Enable();
    }

    private void OnDisable()
    {
        InputActions.Disable();
    }

    //All inputs processed here and passed to appropriate component
    #region Input Management

    void SetupInputs()
    {
        InputActions = new IA_Default(); //Create input action mapping

        //Move
        InputActions.Gameplay.Movement.performed += OnMovementInput;

        //Jump
        InputActions.Gameplay.Jump.performed += OnJumpInput;

        //Sprinting
        InputActions.Gameplay.Sprint.performed += OnSprintInput;
        InputActions.Gameplay.Sprint.canceled += OnSprintInputCancel;
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
        if(playerMovement.bCanJump) playerMovement.DoJump();
    }
    #endregion
}
