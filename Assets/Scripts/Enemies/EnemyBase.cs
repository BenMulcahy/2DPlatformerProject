using UnityEngine;
using EasyButtons;
using Pathfinding;
using System;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEditor;

[RequireComponent(typeof(HealthComponent))]
public class EnemyBase : MonoBehaviour
{
    //TODO: Flying movement type
    //TODO: 'Search' for player? (Low Prio)

    #region Events/Delegates
    public static Action<EnemyBase> onEnemyDeath = delegate { };

    #endregion

    #region Vars

    bool _bFacingRight;

    enum EMovementType
    {
        walking, flying
    };

    public enum EMoveState
    {
        standing, moving, falling, jumping
    };

    [Header("--- Movement ---")]
    [SerializeField] EMovementType _movementType = EMovementType.walking;
    [SerializeField] float _moveSpeed = 10f;
    [SerializeField] float _accelleration = 3f;
    [SerializeField] float _deccelleration = 3f;
    [Header("Jumping")]
    [SerializeField] bool _bJumpEnabled = true;
    [SerializeField] float _jumpHeight = 5f;
    [SerializeField] float _jumpThreshold = 2.0f;
    EMoveState _movementState;
    bool _bWasGrounded;
    float _lastGroundedTimer;
    [SerializeField] LayerMask _groundCheckMask;

    Rigidbody2D _rigidbody;

    [Header("--- Pathfinding --- ")]
    [SerializeField] float _playerDetectionRadius = 6f;
    [SerializeField] float _repathRate = 0.3f; //Time between each pathing calculation
    [SerializeField] float _nextWaypointDist = 3f;
    [SerializeField] float _maxTargetDistance = 2f;
    [SerializeField] Vector2[] _targetPositions = { Vector2.right * 2, Vector2.left * 2 };
    [SerializeField] float _searchTime = 1f; //Time before giving up looking for target (most often player) when lost LOS
    Vector2[] _runtimeTargetPositions;
    int _defaultTargetIndex = 0;
    float _targetSearchTimer;
    Transform _target;
    Vector2 _targetPos;
    Vector2 _startPos;
    Seeker _seeker;
    Path _path;

    //LOS Bool
    [SerializeField]
    [Tooltip("Whether enemy needs to be able to see the player in order to pathfind to them within detection radius")]
    bool _bRequireLOS = true;

    float _currentDistToTarget;
    float _lastRepathTime;
    int _currentWaypoint;

    [Header("--- Attacks --- ")]
    [SerializeField] bool _bAttackEnabled = true;
    [SerializeField] Attack _attackObject;

    #endregion

    #region Unity Funcs
    private void Start()
    {
        _seeker = GetComponent<Seeker>();
        _rigidbody = GetComponent<Rigidbody2D>();
        
        UpdateDefaultPositions();
        if (!_attackObject && _bAttackEnabled) _attackObject = GetComponentInChildren<Attack>();
    }

    private void OnEnable()
    {
        GetComponent<HealthComponent>().onOutOfHealth += Die;
    }

    private void OnDisable()
    {
        GetComponent<HealthComponent>().onOutOfHealth -= Die;
    }


    #endregion

    private void Update()
    {
        if (LookForPlayer())
        {
            _targetSearchTimer = _bRequireLOS ? _searchTime : 0; //Dont use search time if LOS not required
            SetPathTarget(Player.Instance.transform);
        }
        else if(_targetSearchTimer <= 0)
        {
            SetPathTarget(null);
        }

        UpdateGroundStatus();
        DoPathfinding();
        UpdateLookDir();
        CheckAttack();

        UpdateTimers();
    }
    
    void UpdateTimers()
    {
        _targetSearchTimer -= Time.deltaTime;
        _lastGroundedTimer -= Time.deltaTime;
    }

    protected virtual bool LookForPlayer()
    {

        if (Vector2.Distance(Player.Instance.transform.position, transform.position) < _playerDetectionRadius)
        {
            if (_bRequireLOS)
            {
                //Create layer mask to exclude current object layer (should be enemy)
                LayerMask mask = ~0;
                mask &= ~(1 << gameObject.layer);

                RaycastHit2D hit = Physics2D.Linecast(transform.position, Player.Instance.transform.position, mask);
                Debug.DrawLine(transform.position, hit.point, hit.transform.gameObject == Player.Instance.gameObject ? Color.green : Color.red);
                if (hit.transform.gameObject == Player.Instance.gameObject)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else return true;
        }
        else
        {
            return false;
        }
    }

    #region Dying
    /// <summary>
    /// When overidden, call base after implementing overridden code
    /// </summary>
    protected virtual void Die()
    {
        onEnemyDeath?.Invoke(this);
        _attackObject.ResetHits(); //ensures that any knocback etc applied to player is removed
        Destroy(gameObject);
    }
    #endregion

    #region Pathfinding/Movement
    void DoPathfinding()
    {
        if (_target) _currentDistToTarget = Vector2.Distance(transform.position, _targetPos); //Get distance to current target position
        else _currentDistToTarget = Vector2.Distance(transform.position, _startPos + _runtimeTargetPositions[_defaultTargetIndex]);

        if (Time.time > _lastRepathTime + (IsOnFloor() ? _repathRate : _repathRate * 0.2f) && _seeker.IsDone() && !ReachedTarget())
        {
            _lastRepathTime = Time.time;
            FindPath();
        }


        if (_path == null) return;

        MoveTowardsNextWaypoint();
    }

    /// <summary>
    /// Calculate Path to Target
    /// </summary>
    void FindPath()
    {
        //Debug.Log("Find Path");
        _seeker.StartPath(transform.position, _targetPos, OnFindPathComplete);
    }

    /// <summary>
    /// Set the enemy pathfinding target, if given null target will set target pos to start position
    /// </summary>
    /// <param name="target"></param>
    public void SetPathTarget(Transform target)
    {
        _target = target == this.transform ? null : target; //Ensure target cannot be self
        if (_target)
        {
            SetPathTargetPosition(_target.position);
        }
        else
        {
            SetPathTargetPosition(_startPos + _runtimeTargetPositions[_defaultTargetIndex]);
            
            if (ReachedTarget()) _defaultTargetIndex++;
            _defaultTargetIndex %= _runtimeTargetPositions.Length;
            
        }
    }

    void SetPathTargetPosition(Vector2 position)
    {
        _targetPos = position;
    }


    /// <summary>
    /// Returns true if current distance to target is less than the max distance
    /// </summary>
    /// <returns></returns>
    bool ReachedTarget()
    {
        if (_path == null) return false;
        SetMovementState(EMoveState.standing);
        return _currentDistToTarget <= _maxTargetDistance;
    }


    /// <summary>
    /// Checks for next waypoint, ensuring its lower than the next waypoint dist then moves to waypoint, also checks if end of path
    /// </summary>
    void MoveTowardsNextWaypoint()
    {
        float distToWaypoint;
        while (true)
        {
            //Debug.Log("Check waypoint distance");
            distToWaypoint = Vector3.Distance(transform.position, _path.vectorPath[_currentWaypoint]);
            if (distToWaypoint < _nextWaypointDist)
            {
                //Debug.Log("Check for end of path");
                if (_currentWaypoint + 1 < _path.vectorPath.Count) _currentWaypoint++;
                else { ReachedTarget(); break; }
            }
            else break;
        }

        Movement();
    }

    protected virtual void Movement()
    {
        
        switch (_movementType)
        {
            case EMovementType.walking:
                
                //X
                
                Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;

                float targetSpeed = dir.x * _moveSpeed;

                if (GetComponent<IHittable>().bIsKnockedBack)
                {
                    targetSpeed = Mathf.Lerp(_rigidbody.linearVelocity.x, targetSpeed, GetComponent<HealthComponent>().KnockbackRecoveryLerp);
                }
                else
                {
                    if (ReachedTarget()) targetSpeed = 0;
                }


                float accelForce = ((1 / Time.fixedDeltaTime) * _accelleration) / _moveSpeed;
                float deccelForce = ((1 / Time.fixedDeltaTime) * _deccelleration) / _moveSpeed;

                float accelRate;
                accelRate = (Mathf.Abs((dir * _moveSpeed).magnitude) > 0.01f) ? accelForce : deccelForce;

                float movementVal = (targetSpeed - _rigidbody.linearVelocity.x) * accelRate;
                _rigidbody.AddForce(movementVal * Vector2.right, ForceMode2D.Force);

                if (_bJumpEnabled && WantsToJump())
                {
                    DoJump();
                }

                break;
            case EMovementType.flying:
                break;
            default:
                break;
        }

    }

    #region Jumping

    //TODO: Reconsider
    bool WantsToJump()
    {
        if (IsOnFloor())
        {
            //if walking off edge && if target pos == same height or higher than current
            if (!RaycastToGround(!_bFacingRight) && _targetPos.y >= transform.position.y)
            {
                Debug.Log("Auto-jump from edge detect");
                return true;
            }

            // check if next (jumpHeight) waypoint(s) is above threshold
            //int waypointTmp = _currentWaypoint;
            for (int i = 0; i < _jumpHeight; i++)
            {
                if (_currentWaypoint + i < _path.vectorPath.Count && _path.vectorPath[_currentWaypoint + i].y >= transform.position.y)
                {
                    if((_path.vectorPath[_currentWaypoint + i].y - transform.position.y) > (LookForPlayer() ? Player.Instance.GetComponent<PlayerMovementComponent>().JumpHeight + 0.5f : _jumpThreshold))
                    {
                        Debug.Log("Auto-jump from waypoint Y");
                        return true;
                    }
                }
            }
        }

        return false;
    }

    protected virtual void DoJump()
    {
        //Physics Jump
        _lastGroundedTimer = 0;
        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocityX, 0); //Kill vert velocity before jump
        float jumpForce = Mathf.Sqrt(_jumpHeight * (Physics2D.gravity.y * _rigidbody.gravityScale) * -2) * _rigidbody.mass;
        _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
    #endregion

    void SetMovementState(EMoveState moveState)
    {
        _movementState = moveState;
        switch (_movementState)
        {
            case EMoveState.moving:
                break;
            case EMoveState.standing:
                break;
            case EMoveState.falling:
                break;
            case EMoveState.jumping:
                break;
            default:
                break;
        }
    }

    void OnFindPathComplete(Path pth)
    {
        pth.Claim(this);
        if (!pth.error)
        {
            if (_path != null) _path.Release(this);
            _path = pth;
            _currentWaypoint = 0;
        }
        else { Debug.LogWarning("Path calculated with error: " + pth.error); pth.Release(this); }
    }

    protected virtual void UpdateLookDir()
    {
        if (GetComponent<HealthComponent>().bIsKnockedBack) return;

        //Update L/R direction
        if (InRangeOfTarget())
        {
            _bFacingRight = transform.position.x < _targetPos.x ? true : false; //Face target
        }
        else
        {
            if (_rigidbody.linearVelocityX > 0) _bFacingRight = true;
            else if (_rigidbody.linearVelocityX < 0) _bFacingRight = false;
        }

        if(_attackObject) _attackObject.SetAttackDir(_bFacingRight);
    }

    #endregion

    #region Attacking
    bool InRangeOfTarget()
    {
        if (_target && _target.gameObject != this.gameObject && _target.GetComponent<IHittable>() != null)
        {
            return _currentDistToTarget <= _attackObject.GetAttackHitRadius();
        }
        else
        {
            _attackObject.ResetAtkCD(); //Keep atk on cooldown if not in range -> prevents attacks triggers as soon as in range
            return false;
        }
        
    }

    protected virtual void CheckAttack()
    {
        if (_bAttackEnabled && InRangeOfTarget() && _attackObject.CanAttack())
        {
            Attack();
        }
    }

    protected virtual void Attack()
    {
        _attackObject.DoAttack();
    }

    #endregion

    #region GroundCheck
    void UpdateGroundStatus()
    {
        if (RaycastToGround(true) || RaycastToGround(false))
        {
            if (!_bWasGrounded)
            {
                OnLand();
            }
            _bWasGrounded = true;
            if (_lastGroundedTimer < 0.25f) _lastGroundedTimer = 0f;
            return;
        }
        else
        {
            _bWasGrounded = false;
        }
    }

    public bool IsOnFloor()
    {
        return _lastGroundedTimer >= 0;
    }

    private bool RaycastToGround(bool castLeftEdge)
    {
        Bounds bounds = GetComponent<Collider2D>().bounds;
        Vector2 rPos = new Vector2(bounds.max.x, bounds.min.y);
        Vector2 lPos = bounds.min;

        //Debug.DrawLine(castLeftEdge? lPos : rPos, castLeftEdge ? lPos : rPos + Vector2.down * 0.3f, Color.blue);
        if (Physics2D.Linecast(castLeftEdge ? lPos : rPos, castLeftEdge ? lPos + Vector2.down * 0.25f : rPos + Vector2.down * 0.25f, _groundCheckMask))
        {
            return true;
        }
        else return false;
    }

    private void OnLand()
    {
        Debug.Log(name + " Landed");
        _lastGroundedTimer = 0f;
    }
    #endregion


    #region Editor
    #if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, _playerDetectionRadius);
        
        Gizmos.color = new Color(250, 250,0,0.85f);
        Gizmos.DrawWireSphere(transform.position, _maxTargetDistance);

        Gizmos.color = Color.cyan;

        if (!EditorApplication.isPlaying)
        {
            foreach (Vector2 pos in _targetPositions)
            {
                Gizmos.DrawWireSphere((Vector2)transform.position + pos, 0.3f);
            }
        }
    }
    #endif

    [Button] public void SetMaxTargetDistanceToAttackRange()
    {
        if (!_attackObject) { Debug.LogError("No Attack Object"); return; }
        _maxTargetDistance = _attackObject.GetAttackHitRadius();
    }

    [Button] public void UpdateDefaultPositions()
    {
        _startPos = transform.position;
        _runtimeTargetPositions = null;
        _runtimeTargetPositions = _targetPositions;
    }
    #endregion
}
