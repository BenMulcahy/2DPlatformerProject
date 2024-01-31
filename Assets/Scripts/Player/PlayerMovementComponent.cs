using System.Collections;
using UnityEngine;

public class PlayerMovementComponent : MonoBehaviour
{
    //TODO: Add Walljumps
    //TODO: Add Edge Detect so you dont just bonk

    #region Enums
    enum EWallState
    {
        onLeftWall, onRightWall,NULL
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
    [SerializeField][Range(0.01f,2f)]float _airAccellerationMod = 1.25f; //multiplied to base accel
    [SerializeField][Range(0.01f, 2f)] float _airDeccelleraionMod = 0.8f; //multipied to base deccel
    [Header("Gravity Scales")][Space(2)]
    [SerializeField] float _defaultGravityScale = 10.0f;
    [SerializeField] float _fallingGravityScale = 12.0f;
    [SerializeField] float _shortHopGravityScale = 18.0f;
    [Header("Jump Apex 'Hang'")][Space(2)]
    [SerializeField][Range(0.1f,1f)] float _jumpApexGravityModifier = 0.4f;
    [SerializeField] float _jumpHangThreshold = 1.5f;
    [SerializeField] float _jumpHangAccelerationMod = 1.2f;
    [SerializeField] float _jumpHangMaxSpeedMod = 1.1f;
    public float MaxFallSpeed = 45.0f;

    [Header("--- Wall Jump/Cling ---")][Space(5)]
    public bool bCanWallJump = true;
    EWallState _wallState = EWallState.NULL;
    [SerializeField] Vector2 _wallJumpForce;
    [SerializeField] LayerMask _wallMask;
    [SerializeField] int _wallClingJumpRefund = 1;
    [Header("Gravity Scales")][Space(2)]
    [SerializeField] float _wallSlideUpGravityScale = 18.0f; //Used to decel the player when sliding up a wall
    [SerializeField] float _wallSlideDownGravityScale = 5.0f;

    [Header("Timers")][Space(2)]
    [SerializeField] float _jumpFullPressWindowTime = 0.33f;
    [SerializeField] float _coyoteTime = 0.05f;
    public bool bCanJump { get; private set; }
    public bool bIsOnFloor { get; private set; }
    public int JumpCounter { get; private set; }
    float _jumpPressedTimer;
    bool _bShortHop;

    [Header("GroundCheck")]
    [SerializeField] LayerMask _groundCheckMask;
    #endregion

    private void OnValidate()
    {
        //Clamp Accel and Deccel values to under max speeds
        _accelleration = Mathf.Clamp(_accelleration, 0.01f, _walkSpeed);
        _decelleration = Mathf.Clamp(_decelleration, 0.01f, _walkSpeed);
        if (_airAccellerationMod * _accelleration > _walkSpeed) _airAccellerationMod = _walkSpeed / _accelleration;
        if (_airDeccelleraionMod * _decelleration > _walkSpeed) _airDeccelleraionMod = _walkSpeed / _decelleration;

        //Only refund up to max amount of jumps
        if (_wallClingJumpRefund > MaxJumps) _wallClingJumpRefund = MaxJumps;
    }

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        bCanJump = true;
    }

    private void Start()
    {
        StartCoroutine(GroundCheck());
        StartCoroutine(WallCheck());
        RB.gravityScale = _defaultGravityScale;
        bIsMovingRight = true;
    }

    private void FixedUpdate()
    {
        MovementX();
        UpdateGravityScale();
    }

    #region Walk/Running
    private void MovementX()
    {
        /*   Movement   */
        float movementInput = Player.Instance.PlayerInputActions.Gameplay.Movement.ReadValue<float>();

        //Calculate Accel and target speed
        float targetSpeed = movementInput * (bIsSprinting ? _sprintSpeed : _walkSpeed);

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
        //Debug.Log("Jump!");

        JumpCounter++;
        if (JumpCounter >= MaxJumps) bCanJump = false;

        if(_wallState != EWallState.NULL)
        {
            DoWallJump();
            onPlayerWallJump?.Invoke();
        }
        else
        {
            //Jump
            RB.velocity = new Vector2(RB.velocityX, 0); //Kill vert velocity before jump
            RB.gravityScale = _defaultGravityScale;
            float jumpForce = Mathf.Sqrt(_jumpHeight * (Physics2D.gravity.y * _defaultGravityScale) * -2) * RB.mass;
            RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            onPlayerJump?.Invoke();
        }

        //Start Jump Timer
        _bShortHop = false; //ensure bshorthop is false
        StartCoroutine(JumpTimer());
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
            RB.gravityScale = _defaultGravityScale * _jumpApexGravityModifier; //Hold jump at apex
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
        _bShortHop = false;
        _wallState = EWallState.NULL;
    }
    #endregion

    #region WallJump/Hang

    IEnumerator WallCheck()
    {
        while (true)
        {
            if (!bIsOnFloor)
            {
                if (CastToWall())
                {
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    yield return new WaitForSeconds(_coyoteTime); //Buffer leaving wall by coyoteTime
                    _wallState = EWallState.NULL;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private bool CastToWall()
    {
        if(Player.Instance.PlayerInputActions.Gameplay.Movement.ReadValue<float>() != 0)
        {
            //Using bounding check to get feet and head pos -> used in ledge detect
            Bounds playerBounds = Player.Instance.GetComponent<Collider2D>().bounds;
            Vector2 headPos = new Vector2(transform.position.x, playerBounds.max.y);
            Vector2 feetPos = new Vector2(transform.position.x, playerBounds.min.y);

            //Cast from head and feet in current input direction
            RaycastHit2D headHit = Physics2D.Linecast(headPos, Player.Instance.bIsRightInput ? headPos + Vector2.right : headPos + Vector2.left, _wallMask);
            RaycastHit2D feetHit = Physics2D.Linecast(feetPos, Player.Instance.bIsRightInput ? feetPos + Vector2.right : feetPos + Vector2.left, _wallMask);

            if (headHit && feetHit)
            {
                //Both hit -> Wall cling
                if(_wallState == EWallState.NULL) OnWallCling(); //Only fire if wall state currently null -> results in only firing first time hiting wall
                _wallState = headHit.normal.x > 0 ? EWallState.onRightWall : EWallState.onLeftWall;
                return true;
            }
            /* Possibilty for corner detection?
            else
            {
                if (headHit)
                {
                    //if only head
                }
                else if (feetHit)
                {
                    //if only feet
                }
            }
            */
            else
            {
                //TODO: Buffer Reset of wallState
                return false;
            }
        }
        else
        {
            //TODO: Buffer Reset of wallState
            return false;
        }
        //Debug.Log(_wallState);
    }

    void OnWallCling()
    {
        onPlayerLandWall?.Invoke();
        JumpCounter = Mathf.Clamp(JumpCounter -_wallClingJumpRefund,0,MaxJumps);
        if(JumpCounter < MaxJumps) bCanJump = true;
        _bShortHop = false;
        Debug.Log("OnWallCling: " + _wallState);
    }

    void DoWallJump()
    {
        //TODO: Implement
        Debug.LogWarning("Wall jump not yet implemented!!");
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
