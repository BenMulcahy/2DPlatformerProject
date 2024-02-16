using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Seeker))]
public class EnemyBase : MonoBehaviour
{
    Vector2 _targetPosition;
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
    float _lastRepathTime;
    int _currentWaypoint;
    bool _endOfPath;

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
        _targetPosition = Player.Instance.transform.position;
        _seeker.StartPath(transform.position, _targetPosition, OnPathComplete);
    }

    void DoPathfinding()
    {
        if (Time.time > _lastRepathTime + _repathRate && _seeker.IsDone())
        {
            _lastRepathTime = Time.time;
            FindPath();
        }

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
                else { _endOfPath = true; break; }
            }
            else break;
        }

        //TODO: Redo with custom movement 
        /* MOVEMENT OLD 
        // Slow down smoothly upon approaching the end of the path
        // This value will smoothly go from 1 to 0 as the agent approaches the last waypoint in the path.
        var speedFactor = _endOfPath ? Mathf.Sqrt(distToWaypoint / _nextWaypointDist) : 1f;

        // Direction to the next waypoint
        // Normalize it so that it has a length of 1 world unit
        Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;
        // Multiply the direction by our desired speed to get a velocity
        Vector3 velocity = dir * _moveSpeed * speedFactor;

        // If you are writing a 2D game you may want to remove the CharacterController and instead modify the position directly
        Debug.Log(this.name + ": Do Move");
        transform.position += (velocity * Time.deltaTime);
        */

        Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;

        float targetSpeed = dir.x * _moveSpeed;

        float accelForce = ((1 / Time.fixedDeltaTime) * _accelleration) / _moveSpeed;
        float deccelForce = ((1 / Time.fixedDeltaTime) * _deccelleration) / _moveSpeed;

        float accelRate;
        accelRate = (Mathf.Abs((dir * _moveSpeed).magnitude) > 0.01f) ? accelForce : deccelForce;

        float movementVal = (targetSpeed - _rigidbody.velocity.x) * accelRate;
        _rigidbody.AddForce(movementVal * Vector2.right, ForceMode2D.Force);
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
}
