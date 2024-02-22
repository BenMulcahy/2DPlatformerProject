using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public PlayerMovementComponent playerMovement { get; private set; }
    public PlayerInput PlayerInputComponent { get; private set; }

    #region Delegates and Events
    public delegate void OnPlayerAttack();
    public static event OnPlayerAttack onPlayerAttack;
    #endregion

    #region Vars
    [Header("--- Attacking ---")]
    public bool bAttackEnabled = true;
    public Attack AttackObject { get; private set; }


    [Header("--- Input Buffers ---")]
    [SerializeField] float _jumpInputBuffer = 0.02f;
    float _jumpInputBufferTimer;
    bool _bWantsToJump = false;

    [SerializeField] float _atkInputBuffer = 0.01f;
    float _atkBufferTimer;
    bool _bWantsToAttack = false;

    [HideInInspector] public bool bIsRightInput { get; private set; }
    [HideInInspector] public bool bFacingRight { get; private set; }
    #endregion

    private void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(this);

        playerMovement = GetComponent<PlayerMovementComponent>();
        if (!AttackObject) AttackObject = GetComponentInChildren<Attack>();
    }

    private void Start()
    {
        //if (!AttackObject) AttackObject = GetComponentInChildren<Attack>();
    }

    private void OnEnable()
    {
        SetupInputs(true);
        PlayerInputComponent.actions.Enable();
        GetComponent<HealthComponent>().onOutOfHealth += OutOfHealth;
    }


    private void OnDisable()
    {
        SetupInputs(false);
        PlayerInputComponent.actions.Disable();
        GetComponent<HealthComponent>().onOutOfHealth -= OutOfHealth;
    }

    private void Update()
    {
        //Check for L/R Input
        if (PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>() > 0) bIsRightInput = true;
        else if (PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>() < 0) bIsRightInput = false;

        SetLookDir();

        CheckInputBuffers();
    }

    private void SetLookDir()
    {
        if (GetComponent<HealthComponent>().bIsKnockedBack) return;

        //TODO: Setup facing right based on both input and current mov dir
        bFacingRight = bIsRightInput;
        AttackObject.SetAttackDir(bFacingRight);
    }

    //All inputs processed here and passed to appropriate component
    #region Input Management

    void SetupInputs(bool enabled)
    {
        if (PlayerInputComponent == null) PlayerInputComponent = GetComponent<PlayerInput>();

        if (enabled)
        {
            //Move
            PlayerInputComponent.actions.FindAction("Movement").performed += OnMovementInput;

            //Jump
            PlayerInputComponent.actions.FindAction("Jump").performed += OnJumpInput;
            PlayerInputComponent.actions.FindAction("Jump").canceled += OnJumpCancelled;


            //Sprinting
            PlayerInputComponent.actions.FindAction("Sprint").performed += OnSprintInput;
            PlayerInputComponent.actions.FindAction("Sprint").canceled += OnSprintInputCancel;

            //Attack
            PlayerInputComponent.actions.FindAction("Attack").performed += OnAttackInput;
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

            //Attack
            PlayerInputComponent.actions.FindAction("Attack").performed -= OnAttackInput;

        }

    }

    /* ATTACK */
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!bAttackEnabled) return;

        if (!AttackObject.CanAttack())
        {
            _bWantsToAttack = true;
            _atkBufferTimer = _atkInputBuffer;
        }
        else
        {
            //Do Attack
            AttackObject.DoAttack();
            onPlayerAttack?.Invoke();
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
        else if (playerMovement.bCanJump) playerMovement.OnJumpPerformed(); //Dont need to elif here?
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

        //Attack input buffer
        if (_bWantsToAttack)
        {
            if (_atkBufferTimer > 0 && !AttackObject.CanAttack())
            {
                _atkBufferTimer -= Time.deltaTime;
            }
            else
            {
                _bWantsToAttack = false;
                _atkBufferTimer = 0;
                if (bAttackEnabled && AttackObject.CanAttack()) AttackObject.DoAttack();
            }
        }
    }
    #endregion

    #region Health
    protected virtual void OutOfHealth()
    {
        throw new NotImplementedException();
    }


    #endregion

}