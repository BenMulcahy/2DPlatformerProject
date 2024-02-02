using System.Collections;
using UnityEngine;

public class PlayerMovementComponent : MonoBehaviour
{
    //TODO: Add Edge Detect so you dont just bonk
    //TODO: Greater Affordance for players intentions with walljumps, currently doesnt feel fully fair -> need to buffer when push from wall
    //TODO: Allow for double jumping
    //TODO: Fix walljump cooldowns
    //TODO: Add setting for init jump being from grounded or not

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
    #endregion

    #region Variables
    public Rigidbody2D RB { get; private set; }
    public bool bIsMovingRight { get; private set; }
    //public bool bIsOnFloor { get; private set; }

    [Header("--- Walk/Run ---")][Space(5)]
    [SerializeField] float _walkSpeed = 15.0f;
    [SerializeField] float _sprintSpeed = 20.0f;
    public bool bIsSprinting { get; private set; }
    [Header("Accel/Deccel")]
    [SerializeField] float _accelleration = 2.5f; //Time Aprx to get to full speed
    [SerializeField] float _decelleration = 4f; //Time Aprx to stop

    [Header("--- Jumping ---")][Space(5)]
    [SerializeField] float _jumpHeight = 4.0f;
    public int MaxJumps = 1;
    [SerializeField][Range(0.01f, 2f)] float _airAccellerationMod = 1.25f; //multiplied to base accel
    [SerializeField][Range(0.01f, 2f)] float _airDeccelleraionMod = 0.8f; //multipied to base deccel
    [Header("Jump Apex 'Hang'")]
    [SerializeField][Range(0.1f, 1f)] float _jumpApexGravityModifier = 0.4f;
    [SerializeField] float _jumpHangThreshold = 1.5f;
    [SerializeField] float _jumpHangAccelerationMod = 1.2f;
    [SerializeField] float _jumpHangMaxSpeedMod = 1.1f;
    public float MaxFallSpeed = 45.0f;
    public bool bCanJump { get; private set; }
    public int JumpCounter { get; private set; }
    bool _bShortHop;

    [Header("--- Wall Jump/Slide ---")][Space(5)]
    public bool bWallJumpEnabled = true;
    [SerializeField] float _wallClingDuration = 0.33f; // time before letting go of wall if no input recieved
    [SerializeField] int _wallClingJumpRefund = 1;
    [SerializeField] LayerMask _wallMask;

    [Header("Wall Jump")]
    [SerializeField][Tooltip("Only affects repeat jumps from same wall")] float _wallJumpCooldown = 1.0f; //See tooltip
    [SerializeField] Vector2 _wallJumpForce = new Vector2(30f, 15f);
    [SerializeField] float _wallJumpDuration = 0.8f; //How long wall jump lasts before giving full control to run
    [SerializeField] float _wallJumpToRunLerp = 0.5f; //lerp to return control to running input during walljump
    RaycastHit2D _currentWallHit;
    EWallState _wallState = EWallState.NULL;
    GameObject _lastWallDeparted;
    GameObject _lastWallLanded;

    [Header("Wall Sliding")]
    [SerializeField] float _wallSlideSpeed = -8.0f;
    [SerializeField] float _wallSlideDecelleration = 1.8f;

    [Header("--- Gravity Scales ---")][Space(5)]
    [SerializeField] float _defaultGravityScale = 10.0f;
    [SerializeField] float _fallingGravityScale = 12.0f;
    [SerializeField] float _shortHopGravityScale = 18.0f;
    [SerializeField] float _wallSlideUpGravityScale = 18.0f; //Used to decel the player when sliding up a wall

    [Header("--- Timers ---")][Space(5)]
    [SerializeField] float _jumpFullPressWindowTime = 0.33f;
    [SerializeField] float _coyoteTime = 0.05f;
    [SerializeField] float _wallClingCoyoteTimeMod = 1.25f;
    float _jumpPressedTimer;
    float _wallJumpDurationTimer;
    float _wallJumpCdTimer;
    float _wallClingDurationTimer;
    float _wallClingCoyoteTimer;
    float _groundedTimer;
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
        UpdateGroundStatus();
        UpdateWallStatus();
        UpdateGravityScale();

        if (IsOnFloor() || _wallState != EWallState.NULL) bCanJump = true;
        else bCanJump = false;

        UpdateTimers();
    }
    #endregion

    #region Timer Management
    void UpdateTimers()
    {
        if (_wallJumpDurationTimer < _wallJumpDuration)
        {
            _wallJumpDurationTimer += Time.deltaTime;
        }

        if(_wallJumpCdTimer < _wallJumpCooldown)
        {
            _wallJumpCdTimer += Time.deltaTime;
        }

        if(_wallClingDurationTimer < _wallClingDuration)
        {
            _wallClingDurationTimer += Time.deltaTime;
        }

        if (_wallClingCoyoteTimer < _coyoteTime * _wallClingCoyoteTimeMod)
        {
            _wallClingCoyoteTimer += Time.deltaTime;
        }

        if(_jumpPressedTimer < _jumpFullPressWindowTime)
        {
            _jumpPressedTimer += Time.deltaTime;
        }

        _groundedTimer -= Time.deltaTime;
    }

    void ResetAllTimers()
    {
        _jumpPressedTimer = _jumpFullPressWindowTime;
        _wallJumpDurationTimer = _wallJumpDuration;
        _wallJumpCdTimer = _wallJumpCooldown;
        _wallClingDurationTimer = _wallClingDuration;
        _wallClingCoyoteTimer = _coyoteTime * _wallClingCoyoteTimeMod;
    }

    #endregion

    #region Walk/Running
    private void MovementX()
    {
        /*   Movement   */
        float movementInput = Player.Instance.PlayerInputActions.Gameplay.Movement.ReadValue<float>();

        //Calculate Accel and target speed
        float targetSpeed = movementInput * (bIsSprinting ? _sprintSpeed : _walkSpeed);

        if(_wallJumpDurationTimer < _wallJumpDuration)
        {
            targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, _wallJumpToRunLerp);
            //targetSpeed = 0;
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

    /*
    public void StartJump()
    {
        bool hasJumped = false;

        JumpCounter++;
        if (JumpCounter >= MaxJumps) bCanJump = false;

        if(_wallState != EWallState.NULL && bWallJumpEnabled && _wallJumpCdTimer >= _wallJumpCooldown)
        {
            DoWallJump();
            hasJumped = true;
        }
        else if(_bCanVertJump)
        {
            //Jump
            _groundCoyoteTimer = _coyoteTime;
            RB.velocity = new Vector2(RB.velocityX, 0); //Kill vert velocity before jump
            RB.gravityScale = _defaultGravityScale;
            float jumpForce = Mathf.Sqrt(_jumpHeight * (Physics2D.gravity.y * _defaultGravityScale) * -2) * RB.mass;
            RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            onPlayerJump?.Invoke();
            hasJumped = true;
            if (JumpCounter >= MaxJumps) _bCanVertJump = false;

        }

        if (hasJumped)
        {
            //Start Jump Timer
            _bShortHop = false; //ensure bshorthop is false
            _jumpPressedTimer = 0;
        }
    }

    public void StopJump()
    {
        //Stop Jump timer
        //StopCoroutine(JumpTimer());
        if (_jumpPressedTimer < _jumpFullPressWindowTime)
        {
            //Apply downward force on player to push back down
            _bShortHop = true;
        }
        else _bShortHop = false;
        _jumpPressedTimer = _jumpFullPressWindowTime;
    }

    */

    public void OnJumpPerformed()
    {
        if (JumpCounter >= MaxJumps) return;

        if (_wallState != EWallState.NULL && bWallJumpEnabled && _wallJumpCdTimer >= _wallJumpCooldown) //If on wall
        {
            DoWallJump();
        }
        else //Normal jump
        {
            DoJump();
        }

        JumpCounter++;
        _bShortHop = false;
        _groundedTimer = 0;
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
        //Jump
        _groundedTimer = _coyoteTime;
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
        else if(_wallState != EWallState.NULL) //Sliding on wall
        {
            RB.gravityScale = 0;
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
        if(!IsOnFloor()) //if not on floor and inputting dir
        {
            if (CheckForWall()) //Is near wall
            {
                float dirInput = Player.Instance.PlayerInputActions.Gameplay.Movement.ReadValue<float>();
                //Debug.Log("Stick to Wall");
                if (_wallState == EWallState.NULL) OnWallLand(); //Only fire if wall state currently null -> results in only firing first time hiting wall
                _wallState = _currentWallHit.normal.x < 0 ? EWallState.onRightWall : EWallState.onLeftWall;

                //Wall Slide
                if (RB.velocity.y < 0)
                {
                    float speedDif = _wallSlideSpeed - RB.velocity.y;
                    float movement = speedDif * _wallSlideDecelleration;
                    movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));
                    RB.AddForce(movement * Vector2.up);
                }

                if (dirInput != 0) //If holding a dir
                {
                    _wallClingDurationTimer = 0;
                }
                else //Go to release wall
                {
                    if (_wallClingDurationTimer >= _wallClingDuration && _wallState != EWallState.NULL) ReleaseWall();
                }
                _wallClingCoyoteTimer = 0;
            }
            else
            {
                //Debug.Log("No Wall");
                if (_wallClingCoyoteTimer >= _coyoteTime * _wallClingCoyoteTimeMod) ReleaseWall();
            }
        }
    }
    void DoWallJump()
    {
        Vector2 force = _wallJumpForce;
        force.x *= (_wallState == EWallState.onRightWall) ? -1 : 1; //apply force in opposite direction of wall
        if (Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x)) force.x -= RB.velocity.x; //Correct for any x velocity being imparted (e.g coyote time kicked in from leaving wall)

        RB.velocity = new(RB.velocity.x, 0); //kill all downward momentum

        //Debug.Log("Apply: " + force + " Walljump force");
        RB.AddForce(force, ForceMode2D.Impulse);

        //Ensure you are no longer registered as on the wall
        _wallJumpDurationTimer = 0;
        ReleaseWall();

        _wallJumpCdTimer = 0;

        _lastWallDeparted = _lastWallLanded;


        onPlayerWallJump?.Invoke();
    }

    void ReleaseWall()
    {
        _wallState = EWallState.NULL;
        //Debug.Log("Release the wall");
    }

    /// <summary>
    /// Checks if there is a wall in front of player and returns true or false, will also set _wallHit cast
    /// </summary>
    /// <returns></returns>
    private bool CheckForWall()
    {
        //Debug.Log("Check Wall");

        //Using bounding check to get feet and head pos -> used in ledge detect
        Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
        Vector2 headPos = new Vector2(transform.position.x, playerBounds.max.y);
        Vector2 feetPos = new Vector2(transform.position.x, playerBounds.min.y);

        bool castToRight = RB.velocityX > 0.05 ? bIsMovingRight : Player.Instance.bIsRightInput;

        //Cast from head and feet in current input direction
        _currentWallHit = Physics2D.Linecast(headPos, castToRight ? headPos + Vector2.right : headPos + Vector2.left, _wallMask);
        RaycastHit2D feetHit = Physics2D.Linecast(feetPos, castToRight ? feetPos + Vector2.right : feetPos + Vector2.left, _wallMask);

        Debug.DrawLine(headPos, castToRight ? headPos + Vector2.right : headPos + Vector2.left, Color.green);
        Debug.DrawLine(feetPos, castToRight ? feetPos + Vector2.right : feetPos + Vector2.left, Color.red);

        if (_currentWallHit && feetHit)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void OnWallLand()
    {
        Debug.Log("Land Wall");

        _lastWallLanded = _currentWallHit.transform.gameObject;

        if (_lastWallDeparted != _lastWallLanded)
        {
            _wallJumpCdTimer = _wallJumpCooldown;
        }
        onPlayerLandWall?.Invoke();
        JumpCounter = Mathf.Clamp(JumpCounter - _wallClingJumpRefund, 0, MaxJumps);
        _bShortHop = false;
        _wallJumpDurationTimer = _wallJumpDuration;
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
            _groundedTimer = _coyoteTime;
            return;
        }
        else
        {
            _bWasGrounded = false;
        }
    }

    public bool IsOnFloor()
    {
        return _groundedTimer > 0;
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
        Debug.Log("Landed");
        onPlayerLand?.Invoke();

        //Reset Values on landing
        JumpCounter = 0;
        _bShortHop = false;
        _wallState = EWallState.NULL;
        _wallJumpDurationTimer = _wallJumpDuration;
        _wallJumpCdTimer = _wallJumpCooldown;
    }
    #endregion
}
