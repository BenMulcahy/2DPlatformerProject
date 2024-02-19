using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Seeker))]
public class EnemyBase : MonoBehaviour
{
    //TODO: Jumping
    //TODO: Attacking

    #region Vars

    Transform _target;
    Seeker _seeker;
    Path _path;

    enum EMovementType
    {
        walking, flying
    };

    [Header("--- Movement ---")]
    [SerializeField] EMovementType _movementType = EMovementType.walking;
    [SerializeField] float _moveSpeed = 10f;
    [SerializeField] float _accelleration = 3f;
    [SerializeField] float _deccelleration = 3f;

    Rigidbody2D _rigidbody;

    [Header("Pathfinding")]
    [SerializeField] float _repathRate = 0.3f; //Time between each pathing calculation
    [SerializeField] float _nextWaypointDist = 3f;
    [SerializeField] float _maxTargetDistance = 2f;
    [SerializeField] float _agentAvoidanceDistance = 3f;
    float _currentDistToTarget;
    float _lastRepathTime;
    int _currentWaypoint;
    bool _endOfPath;

    [Header("--- Attacks --- ")]
    [SerializeField] bool _bCanAttack;
    [SerializeField] Attack _attackObject;

    #endregion

    private void Start()
    {
        _seeker = GetComponent<Seeker>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        GetComponent<HealthComponent>().onOutOfHealth += Die;
    }

    private void OnDisable()
    {
        GetComponent<HealthComponent>().onOutOfHealth -= Die;
    }


    private void Update()
    {
        DoPathfinding();

    }

    #region Dying
    /// <summary>
    /// When overidden, call base after implementing overridden code
    /// </summary>
    protected virtual void Die()
    {
        Destroy(gameObject);
    }
    #endregion

    #region Pathfinding
    void FindPath()
    {
        Debug.Log("Find Path");
        _target = Player.Instance.transform;
        _seeker.StartPath(transform.position, _target.position, OnPathComplete);
    }

    void DoPathfinding()
    {
        if (Time.time > _lastRepathTime + _repathRate && _seeker.IsDone())
        {
            _lastRepathTime = Time.time;
            FindPath();
        }

        if (_target) _currentDistToTarget = Vector2.Distance(transform.position, _target.position);

        if (_path == null) { Debug.Log("No Path!"); return; }

        MoveTowardsNextWaypoint();
    }

    void MoveTowardsNextWaypoint()
    {
        _endOfPath = false;
        float distToWaypoint;
        while (true)
        {
            Debug.Log("Check waypoint distance");
            distToWaypoint = Vector3.Distance(transform.position, _path.vectorPath[_currentWaypoint]);
            if (distToWaypoint < _nextWaypointDist)
            {
                Debug.Log("Check for end of path");
                if (_currentWaypoint + 1 < _path.vectorPath.Count) _currentWaypoint++;
                else if (_currentDistToTarget <= _maxTargetDistance) { _endOfPath = true; break; }
                else { _endOfPath = true; break; }
            }
            else break;
        }

        MoveX();
    }

    protected virtual void MoveX()
    {
        switch (_movementType)
        {
            case EMovementType.walking:
                Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;

                float targetSpeed = dir.x * _moveSpeed;

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

    void OnPathComplete(Path pth)
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

    #endregion

    #region Attacking
    bool IsWantingToAttack()
    {
        return _currentDistToTarget <= _attackObject.Data.AttackRange;
    }

    #endregion
}
