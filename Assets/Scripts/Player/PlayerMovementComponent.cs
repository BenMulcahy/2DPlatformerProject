using System.Collections;
using UnityEngine;

public class PlayerMovementComponent : MonoBehaviour
{
    //TODO: Add Walljumps

    #region Events and Delegates
    public delegate void OnPlayerLand();
    public static event OnPlayerLand onPlayerLand;

    public delegate void OnPlayerJump();
    public static event OnPlayerJump onPlayerJump;

    public delegate void OnPlayerMove(Vector2 playerVelocity);
    public static event OnPlayerMove onPlayerMove;
    #endregion

    #region Variables
    public Rigidbody2D RB { get; private set; }
    public bool bIsFacingRight { get; private set;}

    [Header("--- Walk/Run ---")][Space(5)]
    [SerializeField] float _walkSpeed = 15.0f;
    [SerializeField] float _sprintSpeed = 20.0f;
    public bool bIsSprinting { get; private set; }
    [Header("Accel/Deccel")]
    [SerializeField] float _accelleration = 2.5f; //Time Aprx to get to full speed
    [SerializeField] float _decelleration = 4f; //Time Aprx to stop

    [Header("--- Jumping ---")][Space(5)]
    [SerializeField] float _jumpHeight = 4.0f;
    public int maxJumps = 1;
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
    [SerializeField] float _maxFallSpeed = 40.0f;

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
    }

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        bCanJump = true;
    }

    private void Start()
    {
        StartCoroutine(GroundCheck());
        RB.gravityScale = _defaultGravityScale;
        bIsFacingRight = true;
    }

    private void FixedUpdate()
    {
        MovementX();
        UpdateGravityScale();
    }

    #region Walk/Running
    private void MovementX()
    {
        /*    New Movement   */
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

        //Set Right Facing
        if (RB.velocity.x < 0) bIsFacingRight = false; else if (RB.velocity.x > 0) bIsFacingRight = true;
    }

    public void SetSprinting(bool bSprinting)
    {
        bIsSprinting = bSprinting;
    }
    #endregion

    #region Jumping/Falling
    public void StartJump()
    {
        JumpCounter++;
        if (JumpCounter >= maxJumps) bCanJump = false;

        //Jump
        float jumpForce = Mathf.Sqrt(_jumpHeight * (Physics2D.gravity.y * _defaultGravityScale) * -2) * RB.mass;
        RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        onPlayerJump?.Invoke();

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
            RB.gravityScale = _bShortHop ? _shortHopGravityScale : _defaultGravityScale;
        }
        else if(JumpCounter > 0 && Mathf.Abs(RB.velocity.y) < _jumpHangThreshold) //Reaching Apex
        {
            RB.gravityScale = _defaultGravityScale * _jumpApexGravityModifier; //Hold jump at apex
        }
        else
        {
            RB.gravityScale = _fallingGravityScale; //Falling
            RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocityY, -_maxFallSpeed)); //Clamp fall speed to max
        }
    }

    private void OnLand()
    {
        onPlayerLand?.Invoke();

        //Reset Values on landing
        bIsOnFloor = true;
        JumpCounter = 0;
        bCanJump = true;
        _bShortHop = false;
    }
    #endregion

    #region GroundCheck
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
    #endregion
}
