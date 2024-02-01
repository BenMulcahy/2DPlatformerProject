using System.Collections;
using System.Xml;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class PlayerMovementComponent : MonoBehaviour
{
    //TODO: Add Edge Detect so you dont just bonk
    //TODO: Greater Affordance for players intentions with walljumps, currently doesnt feel fully fair
    //TODO: Slow player Y Vel on wall cling to max value 

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
    bool _bCanVertJump = true;
    bool _bShortHop;



    [Header("--- Wall Jump/Slide ---")][Space(5)]
    public bool bWallJumpEnabled = true;
    [SerializeField] float _wallClingDuration = 0.33f; // time before letting go of wall if no input recieved
    [SerializeField] float _wallJumpCooldown = 1.0f;
    [SerializeField] Vector2 _wallJumpForce = new Vector2(30f, 15f);
    [SerializeField] float _wallJumpDuration = 0.8f; //How long wall jump lasts before giving full control to run
    [SerializeField] float _wallJumpToRunLerp = 0.5f; //lerp to return control to running input during walljump
    [SerializeField] LayerMask _wallMask;
    [SerializeField] int _wallClingJumpRefund = 1;
    RaycastHit2D _wallHit;
    EWallState _wallState = EWallState.NULL;

    [Header("--- Gravity Scales ---")][Space(5)]
    [SerializeField] float _defaultGravityScale = 10.0f;
    [SerializeField] float _fallingGravityScale = 12.0f;
    [SerializeField] float _shortHopGravityScale = 18.0f;
    [SerializeField] float _wallSlideUpGravityScale = 18.0f; //Used to decel the player when sliding up a wall
    [SerializeField] float _wallSlideDownGravityScale = 5.0f;

    [Header("--- Timers ---")][Space(5)]
    [SerializeField] float _jumpFullPressWindowTime = 0.33f;
    [SerializeField] float _coyoteTime = 0.05f;
    [SerializeField] float _wallClingCoyoteTimeMod = 1.25f;
    public bool bCanJump { get; private set; }
    public bool bIsOnFloor { get; private set; }
    public int JumpCounter { get; private set; }
    float _jumpPressedTimer;
    float _wallJumpDurationTimer;
    float _wallJumpCdTimer;
    float _wallClingDurationTimer;
    float _wallClingCoyoteTimer;

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

        StartCoroutine(GroundCheck());
        //StartCoroutine(WallCheck());

    }

    private void FixedUpdate()
    {
        MovementX();
    }

    private void Update()
    {
        UpdateWallStatus();
        UpdateGravityScale();
        UpdateTimers();
    }
    #endregion

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

        if(_wallClingCoyoteTimer < _coyoteTime * _wallClingCoyoteTimeMod)
        {
            _wallClingCoyoteTimer += Time.deltaTime;
        }
    }

    void ResetAllTimers()
    {
        _wallJumpDurationTimer = _wallJumpDuration;
        _wallJumpCdTimer = _wallJumpCooldown;
        _wallClingDurationTimer = _wallClingDuration;
        _wallClingCoyoteTimer = _coyoteTime * _wallClingCoyoteTimeMod;
    }

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
        if (bIsOnFloor) accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelForce : deccelForce;
        else accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelForce * _airAccellerationMod : deccelForce * _airDeccelleraionMod;

        if (!bIsOnFloor && Mathf.Abs(RB.velocity.y) < _jumpHangThreshold) //Additional Air hang control
        {
            accelRate *= _jumpHangAccelerationMod;
            targetSpeed *= _jumpHangMaxSpeedMod;
        }

        float movementVal = (targetSpeed - RB.velocity.x) * accelRate;
        RB.AddForce(movementVal * Vector2.right, ForceMode2D.Force);

        onPlayerMove?.Invoke(RB.velocity);

        //Set Right Facing TODO: Review this
        if (RB.velocity.x < 0) bIsMovingRight = false; else if (RB.velocity.x > 0) bIsMovingRight = true;
    }

    public void SetSprinting(bool bSprinting)
    {
        bIsSprinting = bSprinting;
    }
    #endregion

    #region Jumping/Falling
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
            StartCoroutine(JumpTimer());
        }
    }

    public void StopJump()
    {
        //Stop Jump timer
        StopCoroutine(JumpTimer());
        if (_jumpPressedTimer < _jumpFullPressWindowTime)
        {
            //Apply downward force on player to push back down
            _bShortHop = true;
        }
        else _bShortHop = false;
    }

    IEnumerator JumpTimer()
    {
        _jumpPressedTimer = 0;
        while(_jumpPressedTimer <= _jumpFullPressWindowTime)
        {
            _jumpPressedTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
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
            if (_wallState != EWallState.NULL) RB.gravityScale = _wallSlideDownGravityScale;
            else RB.gravityScale = _defaultGravityScale * _jumpApexGravityModifier; //Hold jump at apex
        }
        else
        {
            if (_wallState != EWallState.NULL) RB.gravityScale = _wallSlideDownGravityScale;
            else RB.gravityScale = _fallingGravityScale; //Falling
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocityY, -MaxFallSpeed)); //Clamp fall speed to max
        }
    }

    private void OnLand()
    {
        //Debug.Log("Landed");

        onPlayerLand?.Invoke();
        //Reset Values on landing
        bIsOnFloor = true;
        JumpCounter = 0;
        bCanJump = true;
        _bCanVertJump = true;
        _bShortHop = false;
        _wallState = EWallState.NULL;
        _wallJumpDurationTimer = _wallJumpDuration;
        _wallJumpCdTimer = _wallJumpCooldown;
    }
    #endregion

    #region WallJump/Hang
    void UpdateWallStatus()
    {
        if(!bIsOnFloor) //if not on floor and inputting dir
        {
            if (CheckForWall()) //Is near wall
            {
                float dirInput = Player.Instance.PlayerInputActions.Gameplay.Movement.ReadValue<float>();
                //Debug.Log("Wall");
                if (dirInput != 0) //If holding a dir
                {
                    Debug.Log("Stick to Wall");
                    if (_wallState == EWallState.NULL) OnWallLand(); //Only fire if wall state currently null -> results in only firing first time hiting wall
                    _wallState = _wallHit.normal.x < 0 ? EWallState.onRightWall : EWallState.onLeftWall;
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
                Debug.Log("No Wall");
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

        //TODO: Only care for walljump CD when same wall
        _wallJumpCdTimer = 0;
        onPlayerWallJump?.Invoke();
    }

    void ReleaseWall()
    {
        Debug.Log("Release the wall");
        _wallState = EWallState.NULL;
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

        //Cast from head and feet in current input direction
        _wallHit = Physics2D.Linecast(headPos, Player.Instance.bIsRightInput ? headPos + Vector2.right : headPos + Vector2.left, _wallMask);
        RaycastHit2D feetHit = Physics2D.Linecast(feetPos, Player.Instance.bIsRightInput ? feetPos + Vector2.right : feetPos + Vector2.left, _wallMask);

        Debug.DrawLine(headPos, Player.Instance.bIsRightInput ? headPos + Vector2.right : headPos + Vector2.left, Color.green);
        Debug.DrawLine(feetPos, Player.Instance.bIsRightInput ? feetPos + Vector2.right : feetPos + Vector2.left, Color.red);

        if (_wallHit && feetHit)
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
        onPlayerLandWall?.Invoke();
        JumpCounter = Mathf.Clamp(JumpCounter - _wallClingJumpRefund, 0, MaxJumps);
        if (JumpCounter < MaxJumps) bCanJump = true;
        _bShortHop = false;
        _wallJumpDurationTimer = _wallJumpDuration;
    }

    #endregion

    #region GroundCheck
    public IEnumerator GroundCheck()
    {
        while (true)
        {
            //Edge Detect L
            if (RaycastToGround(true))
            {
                yield return new WaitForFixedUpdate();
            }
            //Edge detect R
            else if (RaycastToGround(false))
            {
                yield return new WaitForFixedUpdate();
            }
            else
            {
                //if not on floor
                yield return new WaitForSeconds(_coyoteTime);
                bIsOnFloor = false;
            }
        }
    }

    private bool RaycastToGround(bool castLeftEdge)
    {
        Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
        Vector2 rPos = new Vector2(playerBounds.max.x, playerBounds.min.y);
        Vector2 lPos = playerBounds.min;

        //Debug.DrawLine(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.3f, Color.blue);
        if (Physics2D.Linecast(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.25f, _groundCheckMask))
        {
            if(!bIsOnFloor) OnLand();
            return true;
        }
        else return false;
    }
    
    #endregion
}
