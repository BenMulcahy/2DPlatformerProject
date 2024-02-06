using UnityEngine;
using System;

public class PlayerMovementComponent : MonoBehaviour
{
    //TODO: Add Edge Detect so you dont just bonk
    //TODO: Greater Affordance for players intentions with walljumps, currently doesnt feel fully fair -> need to buffer when push from wall
    //TODO: Option to prevent downward motion during Coyote Time?

    #region Enums
    enum EWallState
    {
        onLeftWall, onRightWall, NULL
    };

    #endregion

    #region Events and Delegates
    public delegate void OnPlayerLand();
    public static event OnPlayerLand onPlayerLand;

    public delegate void OnPlayerLandWall();
    public static event OnPlayerLandWall onPlayerLandWall;

    public delegate void OnPlayerJump();
    public static event OnPlayerJump onPlayerJump;

    public delegate void OnPlayerWallJump();
    public static event OnPlayerWallJump onPlayerWallJump;

    public delegate void OnPlayerMove(Vector2 playerVelocity);
    public static event OnPlayerMove onPlayerMove;

    public delegate void OnPlayerWallSlide(Vector2 playerVelocity);
    public static event OnPlayerWallSlide onPlayerWallSlide;
    #endregion

    #region Variables
    public Rigidbody2D RB { get; private set; }
    public bool bIsMovingRight { get; private set; }
    //public bool bIsOnFloor { get; private set; }

    [Header("--- Walk/Run ---")][Space(5)]
    [SerializeField] float _walkSpeed = 18.0f;
    [SerializeField] float _sprintSpeed = 22.0f;
    [SerializeField][Tooltip("(When in air)")] bool _bConserveMomentum = true;
    [SerializeField] float _runningSpeedLerp = 1f;//Smoothing when changing speed/direction
    public bool bIsSprinting { get; private set; }
    [Header("Accel/Deccel")]
    [SerializeField] float _accelleration = 2f; //Time Aprx to get to full speed
    [SerializeField] float _decelleration = 4f; //Time Aprx to stop

    [Header("--- Jumping ---")][Space(5)]
    [SerializeField] float _jumpHeight = 6.0f;
    public int MaxJumps = 1;
    [Tooltip("If MaxJumps > 1, will allow for extra jumps to be used as init jump if in air")]
    [SerializeField] bool _bRequireGroundedJump = true;
    [SerializeField][Range(0.01f, 2f)] float _airAccellerationMod = 1.2f; //multiplied to base accel
    [SerializeField][Range(0.01f, 2f)] float _airDeccelleraionMod = 0.33f; //multipied to base deccel
    [Header("Jump Apex 'Hang'")]
    [SerializeField][Range(0.1f, 1f)] float _jumpApexGravityModifier = 0.3f;
    [SerializeField] float _jumpHangThreshold = 1.75f;
    [SerializeField] float _jumpHangAccelerationMod = 1.45f;
    [SerializeField] float _jumpHangMaxSpeedMod = 1.6f;
    public float MaxFallSpeed = 45.0f;
    public bool bCanJump { get; private set; }
    public int JumpCounter { get; private set; }
    bool _bShortHop;

    [Header("--- Wall Jump/Slide ---")][Space(5)]
    public bool bWallJumpEnabled = true;
    [SerializeField] float _wallClingDuration = 0.33f; // time before letting go of wall if no input recieved
    [SerializeField] int _wallClingJumpRefund = 1;
    [SerializeField] LayerMask _wallMask;
    bool _bOnWall;
    GameObject _previousWall;
    GameObject _currentWall;

    [Header("Wall Jump")]
    [SerializeField][Tooltip("Only affects repeat jumps from same wall")] float _wallJumpCooldown = 1.0f; //See tooltip
    [SerializeField] Vector2 _wallJumpForce = new Vector2(20f, 35f);
    [SerializeField] float _wallJumpDuration = 0.15f; //How long wall jump lasts before giving full control to run
    [SerializeField] float _wallJumpToRunLerp = 0.1f; //lerp to return control to running input during walljump
    RaycastHit2D _currentWallHit;
    EWallState _wallState = EWallState.NULL;

    [Header("Wall Sliding")]
    [SerializeField] float _wallSlideSpeed = -8.0f;
    [SerializeField] float _wallSlideDecelleration = 1.8f;

    [Header("--- Gravity Scales ---")][Space(5)]
    [SerializeField] float _defaultGravityScale = 10.0f;
    [SerializeField] float _fallingGravityScale = 12.0f;
    [SerializeField] float _shortHopGravityScale = 16.0f;
    [SerializeField] float _wallSlideUpGravityScale = 18.0f; //Used to decel the player when sliding up a wall

    [Header("--- Timers ---")][Space(5)]
    [SerializeField] float _jumpFullPressWindowTime = 0.42f;
    [SerializeField] float _coyoteTime = 0.16f;
    [SerializeField] float _wallClingCoyoteTimeMod = 1.0f;
    float _jumpPressedTimer;
    float _wallJumpDurationTimer;
    float _wallJumpCdTimer;
    float _wallClingDurationTimer;
    float _lastGroundedTimer;
    float _lastOnWallTimer;
    bool _bWasGrounded;

    [Header("--- GroundCheck ---")][Space(5)]
    [SerializeField] LayerMask _groundCheckMask;
    #endregion

    #region Unity Functions
    private void OnValidate()
    {
        //Clamp Accel and Deccel values to under max speeds
        _accelleration = Mathf.Clamp(_accelleration, 0.01f, _walkSpeed);
        _decelleration = Mathf.Clamp(_decelleration, 0.01f, _walkSpeed);
        if (_airAccellerationMod * _accelleration > _walkSpeed) _airAccellerationMod = _walkSpeed / _accelleration;
        if (_airDeccelleraionMod * _decelleration > _walkSpeed) _airDeccelleraionMod = _walkSpeed / _decelleration;

        //Only refund up to max amount of jumps
        if (_wallClingJumpRefund > MaxJumps) _wallClingJumpRefund = MaxJumps;

        //Reset Timers
        ResetAllTimers();
    }

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        RB.gravityScale = _defaultGravityScale;
        ResetAllTimers();
    }

    private void FixedUpdate()
    {
        MovementX();
    }

    private void Update()
    {
        UpdateTimers();
        UpdateGravityScale();
        UpdateGroundStatus();
        UpdateWallStatus();

        //Allow for player to use double jump as late single jump if out of Coyote time
        if (_bRequireGroundedJump && MaxJumps > 1 && JumpCounter == 0)
        {
            if (_lastGroundedTimer < 0) JumpCounter = 1;
        }

        if (JumpCounter == 0) //First Jump
        {
            if ((_bRequireGroundedJump ? IsOnFloor() : true) || _wallState != EWallState.NULL) bCanJump = true;
            else bCanJump = false;
        }
        else
        {
            if (JumpCounter < MaxJumps || _wallState != EWallState.NULL) bCanJump = true; //TODO: (Low Prio) add cd between jumps?
            else bCanJump = false;
        }
    }
    #endregion

    #region Timer Management
    void UpdateTimers()
    {
        //TODO: Standardize timers (all count up or down)

        if (_wallJumpDurationTimer < _wallJumpDuration)
        {
            _wallJumpDurationTimer += Time.deltaTime;
        }

        if(_wallClingDurationTimer < _wallClingDuration)
        {
            _wallClingDurationTimer += Time.deltaTime;
        }

        if(_jumpPressedTimer < _jumpFullPressWindowTime)
        {
            _jumpPressedTimer += Time.deltaTime;
        }

        _lastGroundedTimer -= Time.deltaTime;
        _lastOnWallTimer -= Time.deltaTime;
        _wallJumpCdTimer -= Time.deltaTime;

    }

    void ResetAllTimers()
    {
        _jumpPressedTimer = _jumpFullPressWindowTime;
        _wallJumpDurationTimer = _wallJumpDuration;
        _wallClingDurationTimer = _wallClingDuration;
    }

    #endregion


    #region Walk/Running
    private void MovementX()
    {
        /*   Movement   */
        float movementInput = Player.Instance.PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>();

        //Calculate Accel and target speed
        float targetSpeed = movementInput * (bIsSprinting ? _sprintSpeed : _walkSpeed);

        if(_wallJumpDurationTimer < _wallJumpDuration)
        {
            targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, _wallJumpToRunLerp);
            //targetSpeed = 0;
        }
        else
        {
            targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, _runningSpeedLerp);
        }

        //Calculate accel and deccel forces to apply
        float accelForce = ((1 / Time.fixedDeltaTime) * _accelleration) / (bIsSprinting ? _sprintSpeed : _walkSpeed);
        float deccelForce = ((1 / Time.fixedDeltaTime) * _decelleration) / (bIsSprinting ? _sprintSpeed : _walkSpeed);

        //Calcualte accelleration rate
        float accelRate;
        if (IsOnFloor()) accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelForce : deccelForce;
        else accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelForce * _airAccellerationMod : deccelForce * _airDeccelleraionMod;

        if (!IsOnFloor() && Mathf.Abs(RB.velocity.y) < _jumpHangThreshold) //Additional Air hang control
        {
            accelRate *= _jumpHangAccelerationMod;
            targetSpeed *= _jumpHangMaxSpeedMod;
        }


        //Momentum Conservation
        if (_bConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && !IsOnFloor())
        {
            accelRate = 0;
        }


        float movementVal = (targetSpeed - RB.velocity.x) * accelRate;
        RB.AddForce(movementVal * Vector2.right, ForceMode2D.Force);

        onPlayerMove?.Invoke(RB.velocity);

        if (RB.velocity.x < 0) bIsMovingRight = false; else if (RB.velocity.x > 0) bIsMovingRight = true;
    }

    public void SetSprinting(bool bSprinting)
    {
        bIsSprinting = bSprinting;
    }
    #endregion

    #region Jumping/Falling

    public void OnJumpPerformed()
    {
        if (JumpCounter >= MaxJumps) return;

        if (_lastOnWallTimer > 0 && _wallState != EWallState.NULL) 
        {
            if (_wallJumpCdTimer > 0) return;
            DoWallJump();
        }
        else //Normal jump
        {
            DoJump();
        }

        JumpCounter++;
        _bShortHop = false;
        _lastGroundedTimer = 0;
        _jumpPressedTimer = 0;
    }

    public void OnJumpCancelled()
    {
        if(_jumpPressedTimer < _jumpFullPressWindowTime)
        {
            _bShortHop = true;
        }
        else _bShortHop = false;
        _jumpPressedTimer = _jumpFullPressWindowTime;
    }

    void DoJump()
    {
        //Debug.Log("Jump!");
        _lastGroundedTimer = 0;
        RB.velocity = new Vector2(RB.velocityX, 0); //Kill vert velocity before jump
        RB.gravityScale = _defaultGravityScale;
        float jumpForce = Mathf.Sqrt(_jumpHeight * (Physics2D.gravity.y * _defaultGravityScale) * -2) * RB.mass;
        RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        onPlayerJump?.Invoke();
    }

    private void UpdateGravityScale()
    {
        //Set Grav Scale
        if (RB.velocity.y >= 0) //Travelling up
        {
            if(_wallState != EWallState.NULL) RB.gravityScale = _wallSlideUpGravityScale;
            else RB.gravityScale = _bShortHop ? _shortHopGravityScale : _defaultGravityScale;
        }
        else if(JumpCounter > 0 && Mathf.Abs(RB.velocity.y) < _jumpHangThreshold) //Reaching Apex
        {
            RB.gravityScale = _defaultGravityScale * _jumpApexGravityModifier; //Hold jump at apex
        }
        else
        {
            RB.gravityScale = _fallingGravityScale; //Falling
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocityY, -MaxFallSpeed)); //Clamp fall speed to max
        }
    }

    #endregion

    #region WallJump/Hang

    void UpdateWallStatus()
    {
        if (!IsOnFloor())
        {
            if (IsNearWall()) //Hits wall
            {
                if (!_bOnWall) OnWallLand();
                _lastOnWallTimer = _coyoteTime * _wallClingCoyoteTimeMod; //Reset timer;
                _bOnWall = true;

                if (Player.Instance.PlayerInputComponent.actions.FindAction("Movement").ReadValue<float>() != 0)
                {
                    WallSlide();
                }
                else
                {
                    if (_lastOnWallTimer < 0) ReleaseWall();
                }
            }
            else //No wall
            {
                if (_lastOnWallTimer < 0 && _wallState != EWallState.NULL) ReleaseWall();
                _bOnWall = false;
            }
        }
    }

    void WallSlide()
    {
        if (RB.velocity.y < 0)
        {
            RB.gravityScale = 0;
            float speedDif = _wallSlideSpeed - RB.velocity.y;
            float movement = speedDif * _wallSlideDecelleration;
            movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));
            RB.AddForce(movement * Vector2.up);

            onPlayerWallSlide?.Invoke(RB.velocity);

        }
    }

    void OnWallLand()
    {
        //Debug.Log("Landed on wall!");
        _wallState = _currentWallHit.normal.x < 0 ? EWallState.onRightWall : EWallState.onLeftWall; //Set wall state
        _currentWall = _currentWallHit.transform.gameObject;
        if (_previousWall != _currentWall) _wallJumpCdTimer = 0;

        //Reset values
        JumpCounter = Mathf.Clamp(JumpCounter - _wallClingJumpRefund, 0, MaxJumps);
        onPlayerLandWall?.Invoke();
        _bShortHop = false;
        bCanJump = true;
    }

    void ReleaseWall(bool fromWallJump = false)
    {
        if (fromWallJump)
        {
            _lastOnWallTimer = 0;
            onPlayerWallJump?.Invoke();
            _wallJumpCdTimer = _wallJumpCooldown; //Start wall jump timer
            _wallJumpDurationTimer = 0;
        }

        //Debug.Log("Dropped from wall!");
        _wallState = EWallState.NULL;
        _previousWall = _currentWall;
        _currentWall = null;
    }

    bool IsNearWall()
    {
        Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
        Vector2 headPos = new Vector2(transform.position.x, playerBounds.max.y);
        Vector2 feetPos = new Vector2(transform.position.x, playerBounds.min.y);

        bool castToRight = RB.velocityX > 0.0 ? bIsMovingRight : Player.Instance.bIsRightInput;

        //Cast from head and feet in current input direction
        _currentWallHit = Physics2D.Linecast(headPos, castToRight ? headPos + Vector2.right * 0.85f : headPos + Vector2.left * 0.85f, _wallMask);
        RaycastHit2D feetHit = Physics2D.Linecast(feetPos, castToRight ? feetPos + Vector2.right * 0.85f : feetPos + Vector2.left * 0.85f, _wallMask);

        Debug.DrawLine(headPos, castToRight ? headPos + Vector2.right * 0.85f : headPos + Vector2.left * 0.85f, Color.green);
        Debug.DrawLine(feetPos, castToRight ? feetPos + Vector2.right * 0.85f : feetPos + Vector2.left * 0.85f, Color.red);

        if (_currentWallHit && feetHit) return true;

        //safety net for if player has started to change dir
        _currentWallHit = Physics2D.Linecast(headPos, !castToRight ? headPos + Vector2.right * 0.85f : headPos + Vector2.left * 0.85f, _wallMask);
        feetHit = Physics2D.Linecast(feetPos, !castToRight ? feetPos + Vector2.right * 0.85f : feetPos + Vector2.left * 0.85f, _wallMask);
        if(_currentWallHit && feetHit) return true;
        else return false;
    }

    void DoWallJump()
    {
        //Debug.Log("Do wall jump!");

        Vector2 force = _wallJumpForce;
        force.x *= (_wallState == EWallState.onRightWall) ? -1 : 1; //apply force in opposite direction of wall
        if (Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x)) force.x -= RB.velocity.x; //Correct for any x velocity being imparted (e.g coyote time kicked in from leaving wall)

        RB.velocity = new(RB.velocity.x, 0); //kill all downward momentum

        //Debug.Log("Apply: " + force + " Walljump force");
        RB.AddForce(force, ForceMode2D.Impulse);
        ReleaseWall(true);
    }
    #endregion

    #region GroundCheck
    void UpdateGroundStatus()
    {
        if (RaycastToGround(true) || RaycastToGround(false))
        {
            if(!_bWasGrounded)
            {
                OnLand();
            }
            _bWasGrounded = true;
            if(_lastGroundedTimer < 0.1f) _lastGroundedTimer = _coyoteTime;
            return;
        }
        else
        {
            _bWasGrounded = false;
        }
    }

    public bool IsOnFloor()
    {
        return _lastGroundedTimer > 0;
    }

    private bool RaycastToGround(bool castLeftEdge)
    {
        Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
        Vector2 rPos = new Vector2(playerBounds.max.x, playerBounds.min.y);
        Vector2 lPos = playerBounds.min;

        //Debug.DrawLine(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.3f, Color.blue);
        if (Physics2D.Linecast(castLeftEdge? lPos : rPos, castLeftEdge ? lPos + Vector2.down * 0.25f : rPos + Vector2.down * 0.25f, _groundCheckMask))
        {
            return true;
        }
        else return false;
    }
    
    private void OnLand()
    {
        //Debug.Log("Landed");
        onPlayerLand?.Invoke();

        _lastGroundedTimer = _coyoteTime;
        //Reset Values on landing
        JumpCounter = 0;
        _bShortHop = false;

        _wallState = EWallState.NULL;
        _currentWall = null;
        _previousWall = null;
        _wallJumpDurationTimer = _wallJumpDuration;
    }
    #endregion
}