using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ProjectileBase : MonoBehaviour
{
    bool _isEnemyProjectile;
    float _speed;
    Vector2 _targetPos;
    float _damage;
    float _knockback;

    bool _isInitialized = false;

    /// <summary>
    /// Call whenever spawning projectile base
    /// </summary>
    public void InitProjectile(float Damage, float speed, float knockback, bool isEnemyProjectile, Vector2 target)
    {
        _speed = speed;
        _isEnemyProjectile = isEnemyProjectile;
        _targetPos = target;
        _damage = Damage;
        _knockback = knockback;
        _isInitialized = true;
    }

    void FixedUpdate()
    {
        if (!_isInitialized) return;

        if (_targetPos != (Vector2)transform.position)
        {
            UpdatePosition();
        }
        CheckHit();
    }

    bool CheckHit()
    {
        Collider2D col = GetComponent<Collider2D>();

        List<Collider2D> hit = new List<Collider2D>();
        col.Overlap(hit);

        for (int i = 0; i < hit.Count(); i++)
        {
            IHittable hittable = hit[i].GetComponent<IHittable>();
            if (hittable != null && !hit[i].GetComponent<IHittable>().bHasBeenHitThisInstance)
            {
                if (!_isEnemyProjectile && !hit[i].GetComponent<Player>())
                {
                    Debug.Log("Player projectile hit: " + hit[i]);
                    DoDamage(hit[i].gameObject);
                    return true;
                }
                else if (!hit[i].GetComponent<EnemyBase>() && _isEnemyProjectile)
                {
                    Debug.Log("Enemy projectile hit:" + hit[i]);
                    DoDamage(hit[i].gameObject);
                    return true;
                }
            }
        }
        return false;
    }

    void DoDamage(GameObject hit)
    {
        if (hit)
        {
            hit.GetComponent<IHittable>().TakeDamage(_damage);
            //Knockback(hit); //TODO Implement
            hit.GetComponent<IHittable>().bHasBeenHitThisInstance = false;
            Destroy(this.gameObject);
        }
    }

     protected virtual void Knockback(GameObject hit)
    {
        if (_knockback <= 0) return;
        //Debug.Log("Knockback");
        Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
        // if (rb)
        // {
        //     hit.GetComponent<IHittable>().bIsKnockedBack = true;

        //     //Get direction from attacker to hit
        //     Vector2 dir = (transform.parent.position - hit.transform.position).normalized;

        //     //Set knockback force to -dir * knockback
        //     Vector2 knockbackVector = -dir * _knockback;

        //     //Apply force
        //     rb.AddForce(knockbackVector, ForceMode2D.Impulse);
        // }
    }

    protected virtual void UpdatePosition()
    {
        //TODO: Implement as an override
        // Vector2 trajectoryRange = _targetPos - _trajStartPoint;
        // float nextPosX = transform.position.x + _speed * Time.deltaTime;
        // float nextPosXNormalized = (nextPosX - _trajStartPoint.x) / trajectoryRange.x;
        // float nextPosYNormalized = _trajectory.Evaluate(nextPosXNormalized);
        // float nextPosY = _trajStartPoint.y + nextPosYNormalized * _curveMaxHeight;

        // Vector2 newPos = new Vector2(nextPosX, nextPosY);
        // transform.position = newPos;

        Vector3 moveDirNormalized = (_targetPos - (Vector2)transform.position).normalized;
        transform.position += moveDirNormalized * _speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
        {
            //Reached Target
            if (_isEnemyProjectile) EnemyProjectileReachedTarget();
            else PlayerProjectileReachedTarget();
        }
    }

    protected virtual void EnemyProjectileReachedTarget()
    {
        //TODO Review, can outrun projectiles lol
        Destroy(this.gameObject);
    }

    protected virtual void PlayerProjectileReachedTarget()
    {
        Destroy(this.gameObject);
    }
}
