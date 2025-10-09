using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

//TODO: Add min/max aim angles for AI
//TODO: Piercing shots

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileBase : MonoBehaviour
{
    bool _isEnemyProjectile;
    float _force;
    float _range;
    Vector2 _targetPos;
    Vector2 _startPos;
    float _damage;
    float _knockback;
    Rigidbody2D _rb;

    bool _isInitialized = false;

    /// <summary>
    /// Call whenever spawning projectile base
    /// </summary>
    public void InitProjectile(float Damage, float force, float range, float knockback, float ProjectileGravityScale, bool isEnemyProjectile, Vector2 target)
    {
        _force = force;
        _isEnemyProjectile = isEnemyProjectile;
        _targetPos = target;
        _damage = Damage;
        _range = range;
        _knockback = knockback;
        _startPos = transform.position;
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;
        _rb.gravityScale = ProjectileGravityScale;
        _rb.linearDamping = 0f;

        Debug.Log("Projectile Init with: " + "Damage: " + _damage + " Knockback: " + _knockback + " Range: " + _range);
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
            else
            {
                DoDestroy();
                return true;
            }
        }
        return false;
    }

    void DoDamage(GameObject hit)
    {
        if (hit)
        {
            hit.GetComponent<IHittable>().TakeDamage(_damage);
            Knockback(hit);
            hit.GetComponent<IHittable>().bHasBeenHitThisInstance = false;
            hit.GetComponent<IHittable>().bIsKnockedBack = false;
            Destroy(this.gameObject);
        }
    }

    protected virtual void Knockback(GameObject hit)
    {
        if (_knockback <= 0) return;
        //Debug.Log("Knockback");
        Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
        if (rb)
        {
            hit.GetComponent<IHittable>().bIsKnockedBack = true;

            //Get direction from attacker to hit
            Vector2 dir = (transform.position - hit.transform.position).normalized;

            //Set knockback force to -dir * knockback
            Vector2 knockbackVector = -dir * _knockback;

            //Apply force
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);
        }
    }

    protected virtual void UpdatePosition()
    {
        Vector2 dir = (_targetPos - _startPos) / Vector2.Distance(_targetPos, _startPos); //Normalized direction to target
        _rb.linearVelocity = dir * _force; //REVIEW - Is this best practice? Projectiles will accell over time (COULD use same logic as player movement for greater variability)

        if (Vector3.Distance(_startPos, transform.position) >= _range)
        {
            DoDestroy();
        }
    }

    void DoDestroy()
    {
        Destroy(gameObject);
    }
}
