using UnityEngine;
using EasyButtons;
using Pathfinding;
using System;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Seeker))]
public class EnemyBase : MonoBehaviour
{
    //TODO: Jumping
    //TODO: Attacking

    #region Events/Delegates
    public static Action<EnemyBase> onEnemyDeath = delegate { };

    #endregion

    #region Vars

    Transform _target;
    Vector2 _targetPos;
    Vector2 _defaultPos;
    Seeker _seeker;
    Path _path;
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
    EMoveState _movementState;

    Rigidbody2D _rigidbody;

    [Header("Pathfinding")]
    [SerializeField] float _playerDetectionRadius = 6f;
    [SerializeField] float _repathRate = 0.3f; //Time between each pathing calculation
    [SerializeField] float _nextWaypointDist = 3f;
    [SerializeField] float _maxTargetDistance = 2f;
    float _currentDistToTarget;
    float _lastRepathTime;
    int _currentWaypoint;
    bool _bEndOfPath;

    [Header("--- Attacks --- ")]
    [SerializeField] bool _bAttackEnabled = true;
    [SerializeField] Attack _attackObject;

    #endregion

    #region Unity Funcs
    private void Start()
    {
        _seeker = GetComponent<Seeker>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _defaultPos = transform.position;
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
        LookForPlayer();
        DoPathfinding();
        UpdateLookDir();
        CheckAttack();
    }

    private void LookForPlayer()
    {
        if(Vector2.Distance(Player.Instance.transform.position, transform.position) < _playerDetectionRadius)
        {
            SetPathTarget(Player.Instance.transform);
        }
        else
        {
            SetPathTarget(null);
        }
    }

    #region Dying
    /// <summary>
    /// When overidden, call base after implementing overridden code
    /// </summary>
    protected virtual void Die()
    {
        onEnemyDeath?.Invoke(this);
        Destroy(gameObject);
    }
    #endregion

    #region Pathfinding/Movement
    void DoPathfinding()
    {
        if(_target) _currentDistToTarget = Vector2.Distance(transform.position, _targetPos); //Get distance to current target position

        if (Time.time > _lastRepathTime + _repathRate && _seeker.IsDone() && !ReachedTarget())
        {
            _lastRepathTime = Time.time;
            _bEndOfPath = false;
            FindPath();
        }


        if (_path == null) { Debug.Log("No Path!"); return; }

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
        if (target == this.transform) { _target = null; return; } //Ensure target cannot be self
        _target = target;
        if (_target) SetPathTargetPosition(_target.position);
        else SetPathTargetPosition(_defaultPos);
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
                else if (ReachedTarget()) {_bEndOfPath = true; break; }
                else { _bEndOfPath = true; break; }
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
                Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;

                float targetSpeed = dir.x * _moveSpeed;

                if (ReachedTarget()) targetSpeed = 0;

                float accelForce = ((1 / Time.fixedDeltaTime) * _accelleration) / _moveSpeed;
                float deccelForce = ((1 / Time.fixedDeltaTime) * _deccelleration) / _moveSpeed;

                float accelRate;
                accelRate = (Mathf.Abs((dir * _moveSpeed).magnitude) > 0.01f) ? accelForce : deccelForce;

                float movementVal = (targetSpeed - _rigidbody.velocity.x) * accelRate;
                _rigidbody.AddForce(movementVal * Vector2.right, ForceMode2D.Force);
                break;
            case EMovementType.flying:
                break;
            default:
                break;
        }

    }

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
        //Update L/R direction
        if (InRangeOfTarget())
        {
            _bFacingRight = transform.position.x < _targetPos.x ? true : false; //Face target
        }
        else
        {
            if (_rigidbody.velocityX > 0) _bFacingRight = true;
            else if (_rigidbody.velocityX < 0) _bFacingRight = false;
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
        else return false;
        
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


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, _playerDetectionRadius);
    }

    [Button] public void SetMaxTargetDistanceToAttackRange()
    {
        if (!_attackObject) { Debug.LogError("No Attack Object"); return; }
        _maxTargetDistance = _attackObject.GetAttackHitRadius();
    }

}
