using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    //public IA_Default PlayerInputActions { get; private set; }
    public PlayerMovementComponent playerMovement { get; private set; }
    public PlayerInput PlayerInputComponent { get; private set; }


    [Header("--- Input Buffers ---")]
    [SerializeField] float _jumpInputBuffer = 0.02f;
    float _jumpInputBufferTimer;
    bool _bWantsToJump = false;
    public bool bIsRightInput { get; private set; }

    private void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(this);

        playerMovement = GetComponent<PlayerMovementComponent>();
    }

    private void OnEnable()
    {
        SetupInputs(true);
        //PlayerInputActions.Enable();
        PlayerInputComponent.actions.Enable();
    }

    private void OnDisable()
    {
        SetupInputs(false);
        //PlayerInputActions.Disable();
        PlayerInputComponent.actions.Disable();
    }

    private void Update()
    {
        //Check for L/R Input
        if (PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>() > 0) bIsRightInput = true;
        else if (PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>() < 0) bIsRightInput = false;

        CheckInputBuffers();
    }

    //All inputs processed here and passed to appropriate component
    #region Input Management

    void SetupInputs(bool enabled)
    {
        //if(PlayerInputActions == null) PlayerInputActions = new IA_Default(); //Create input action mapping
        if (PlayerInputComponent == null) PlayerInputComponent = GetComponent<PlayerInput>();

        if (enabled)
        {
            //Move
            //PlayerInputActions.Gameplay.Movement.performed += OnMovementInput;
            PlayerInputComponent.actions.FindAction("Movement").performed += OnMovementInput;

            //Jump
            PlayerInputComponent.actions.FindAction("Jump").performed += OnJumpInput;
            PlayerInputComponent.actions.FindAction("Jump").canceled += OnJumpCancelled;


            //Sprinting
            PlayerInputComponent.actions.FindAction("Sprint").performed += OnSprintInput;
            PlayerInputComponent.actions.FindAction("Sprint").canceled += OnSprintInputCancel;
        }

        if (!enabled)
        {
            //Move
            PlayerInputComponent.actions.FindAction("Movement").performed -= OnMovementInput;

            //Jump
            PlayerInputComponent.actions.FindAction("Jump").performed -= OnJumpInput;
            PlayerInputComponent.actions.FindAction("Jump").canceled -= OnJumpCancelled;


            //Sprinting
            PlayerInputComponent.actions.FindAction("Sprint").performed -= OnSprintInput;
            PlayerInputComponent.actions.FindAction("Sprint").canceled -= OnSprintInputCancel;
        }

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
        if (!playerMovement.bCanJump)  //If cant jump set buffer timer
        {
            _bWantsToJump = true;
            _jumpInputBufferTimer = _jumpInputBuffer; 
        }
        else if (playerMovement.bCanJump) playerMovement.OnJumpPerformed();
    }

    /* JUMP - Stop */
    private void OnJumpCancelled(InputAction.CallbackContext context)
    {
        if(playerMovement.JumpCounter > 0) playerMovement.OnJumpCancelled();
    }

    private void CheckInputBuffers()
    {
        //Jumping Input Buffer
        if (_bWantsToJump)
        {
            if (_jumpInputBufferTimer > 0 && !playerMovement.bCanJump)
            {
                //Debug.Log("Using Buffer Currently at: " + _jumpInputBufferTimer);
                _jumpInputBufferTimer -= Time.deltaTime;
            }
            else
            {
                _bWantsToJump = false;
                _jumpInputBufferTimer = 0;
                if (playerMovement.bCanJump) playerMovement.OnJumpPerformed();
                if (!PlayerInputComponent.actions.FindAction("Jump").IsPressed())
                {
                    //If short hop from buffer
                    //Debug.Log("Short hop buffer protect");
                    playerMovement.OnJumpCancelled();
                }
            }
        }
    }
   
    #endregion
}