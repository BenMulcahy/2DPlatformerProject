using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;
using NUnit.Framework.Constraints;
using System.Resources;

public class Attack : MonoBehaviour
{
    //TODO: Ranged attacks 

    #region Events/Delegates
    /// <summary>
    /// Called whenever attack hits something
    /// </summary>
    public delegate void OnAttackHit();
    public event OnAttackHit onAttackHit;

    /// <summary>
    /// Called when attack state changes to startup
    /// </summary>
    public delegate void OnAttackStartup();
    public event OnAttackStartup onAttackStartup;

    /// <summary>
    /// Called when attack state changes to active
    /// </summary>
    public delegate void OnAttackActive();
    public event OnAttackActive onAttackActive;

    /// <summary>
    /// Called when attack state changes to end
    /// </summary>
    public delegate void OnAttackEnd();
    public event OnAttackEnd onAttackEnd;
    #endregion

    #region Vars
    [field:SerializeField] public AttackData Data { get; private set; }
    [SerializeField] bool _bFlipSpriteWithDir;
    bool _bIsFlipped;
    bool _bShouldAttackRight = true;
    float _atkCDTimer;
    bool _bIsAttacking;

    List<Collider2D> _hits = new List<Collider2D>();

    Animator _animator;
    SpriteRenderer _spriteRenderer;
    Vector2 _defaultSpritePos;
    Vector3 _defaultScale;

    public enum EAttackState
    {
        startup, active, ending, NULL
    }

    EAttackState _attackState = EAttackState.NULL;
    #endregion



    private void OnValidate()
    {
        SetData();
    }

    private void Start()
    {
        SetData();
        _atkCDTimer = 0;

        if (GetComponentInParent<HealthComponent>())
        {
            GetComponentInParent<HealthComponent>().onTakeDamage += ResetAtkCD;
        }
    }

    void SetData()
    {
        if (!Data) Data = Resources.Load<AttackData>("DefaultAttack");
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _spriteRenderer.sprite = Data.Sprite;
        _animator = GetComponent<Animator>();
        _defaultSpritePos = _spriteRenderer.transform.localPosition;
        _defaultScale = transform.localScale;
        _attackState = EAttackState.NULL;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var hitBox in Data.HitSphereBounds)
        {
            Vector2 hitpos = new Vector2(transform.position.x + (_bShouldAttackRight ? hitBox.x : -hitBox.x), transform.position.y + hitBox.y);
            Gizmos.DrawWireSphere((Vector3)hitpos, hitBox.z);
        }

        //Draw attack range
        Gizmos.color = new Color(255, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.parent.position, GetAttackHitRadius());
    }

    private void Update()
    {
        if (!_bIsAttacking) _atkCDTimer -= Time.deltaTime;
    }


    #region Attacking Logic

    public virtual void DoAttack()
    {
        if (Data == null) {Debug.LogError("No Attack Data for attack: " + this.name); return;}

        if (!_bIsAttacking) StartCoroutine(Attacking());
        //Debug.Log("Attack!");
        
    }

    public virtual IEnumerator Attacking()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        _bIsAttacking = true;

        _attackState = EAttackState.startup;
        _animator.SetTrigger("IsAttacking");

        // StartCoroutine(SetIFrames());

        while (_attackState == EAttackState.startup)
        {
            //Start up logic
            yield return wait;
        }

        while (_attackState == EAttackState.active)
        {
            if (AttackHitCheck())
            {
                //Debug.Log("hit: " + _hits.Count + " entities");
                for (int i = 0; i < _hits.Count; i++)
                {
                    /* -------- DEAL DAMAGE ------- */
                    if (_hits[i] != null) //check that hit is valid (enemy hasnt died)
                    {
                        IHittable hittable = _hits[i].gameObject.GetComponent<IHittable>();
                        if (hittable != null && !hittable.bHasBeenHitThisInstance)
                        {
                            AttackHit(hittable, i);
                        }
                    }
                }
            }
            yield return wait;
        }

        while (_attackState == EAttackState.ending)
        {
            //TODO End lag logic
            yield return wait;
        }

        // StopCoroutine(SetIFrames());
        _bIsAttacking = false;
    }

    void AttackHit(IHittable hit, int index)
    {
        //Debug.Log("do hittin");
        hit.TakeDamage(Data.Damage);
        if (hit.bCanBeKnockedBack)
        {
            Knockback(_hits[index].gameObject);
        }

        //Cam shake
        if (!CameraManager.Instance.bIsCameraShaking) CameraManager.Instance.DoCameraShake(Data.CameraShakeIntensity, Data.CameraShakeDuration, Data.CameraShakeNoiseSettings ? Data.CameraShakeNoiseSettings : null);
        hit.bHasBeenHitThisInstance = true;
        Hitstop();

        onAttackHit?.Invoke();
    }

    //Reset Hit state on all items hit
    public void ResetHits()
    {
        foreach (Collider2D hit in _hits)
        {
            if (!hit) break; //in case hit isnt valid (enemy has died)
            if(hit.GetComponent<IHittable>() != null) //Redundancy check
            {
                hit.GetComponent<IHittable>().bHasBeenHitThisInstance = false;
                hit.GetComponent<IHittable>().bIsKnockedBack = false;
            } 
        }
        _hits.Clear();
    }

    public bool CanAttack()
    {
        return _atkCDTimer < 0 && !_bIsAttacking;
    }

    bool AttackHitCheck()
    {
        //Debug.Log("Check for hits");
        //_hits.Clear();
        foreach (var hitBox in Data.HitSphereBounds)
        {
            Vector2 hitpos = new Vector2(transform.position.x + (_bShouldAttackRight ? hitBox.x : -hitBox.x), transform.position.y + hitBox.y);

            RaycastHit2D[] tmp = Physics2D.CircleCastAll(hitpos, hitBox.z, transform.forward,Mathf.Infinity,Data.AttackLayer);
            //Debug.DrawRay(hitpos, _bShouldAttackRight ? Vector2.right : Vector2.left, Color.cyan, 0.2f);

            if (tmp.Length > 0)
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!_hits.Contains(tmp[i].collider)) _hits.Add(tmp[i].collider);
                    //else Debug.LogWarning("Already in hit list");
                }
            }
        }

        if (_hits.Count != 0) return true;
        else return false;
    }

    public void SetAttackState(EAttackState newState)
    {
        _attackState = newState;

        switch (_attackState)
        {
            case EAttackState.startup:
                onAttackStartup?.Invoke();
                break;
            case EAttackState.active:
                onAttackActive?.Invoke();
                break;
            case EAttackState.ending:
                onAttackEnd?.Invoke();
                break;
            case EAttackState.NULL:
                ResetHits();
                ResetAtkCD();
                break;
            default:
                break;
        }

        //Debug.Log("Attack Set to " + _attackState);
    }

    public void SetIFrameBool(int NewValue)
    {
        switch (NewValue)
        {
            case 0:
                InvincibleFrames(false);
                break;
            case 1:
                InvincibleFrames(true);
                break;
            default:
                Debug.LogError("Incorrect I frame setup!");
                break;
        }
    }

    /// <summary>
    /// Gets the furthest point from the entity to hit box areas
    /// </summary>
    /// <returns>Distance from entity point to further hitbox point</returns>
    public float GetAttackHitRadius()
    {
        Vector2 entityPos = transform.parent.position;
        Vector2 allHitBoxesFurthestPoint = entityPos;


        //Get furthest hitbox
        foreach (var hitBox in Data.HitSphereBounds)
        {
            Vector2 hitboxPos = new Vector2(transform.position.x + (_bShouldAttackRight ? hitBox.x : -hitBox.x), transform.position.y + hitBox.y);
            //Vector2.Angle(transform.parent.position, hitboxPos);
            float hitBoxDist = Vector2.Distance(entityPos, hitboxPos);

            Vector2 hitBoxFurthestPoint = entityPos + (hitboxPos - entityPos) * (hitBoxDist + hitBox.z) / hitBoxDist;

            if (Vector2.Distance(entityPos, allHitBoxesFurthestPoint) < Vector2.Distance(entityPos, hitBoxFurthestPoint))
            {
                allHitBoxesFurthestPoint = hitBoxFurthestPoint;
            }
        }

        Debug.DrawLine(entityPos, allHitBoxesFurthestPoint, Color.cyan);
        return Vector2.Distance(entityPos, allHitBoxesFurthestPoint);
    }

/// <summary>
/// Set within animation triggers - prevents player taking damage during 'IFrame regions'
/// </summary>
/// <param name="enabled"></param>
    void InvincibleFrames(bool enabled)
    {
        if (Data.bEnableIFrames)
        {
            Debug.Log("Iframes: " + enabled);
            GetComponentInParent<HealthComponent>().bCanTakeDamage = !enabled;
        }
    }


    #endregion

    #region Visuals/Gamefeel

    public virtual void SetAttackDir(bool attackRight)
    {
        _bShouldAttackRight = attackRight;
        if (_bFlipSpriteWithDir)
        {
            if (attackRight && _bIsFlipped)
            {
                transform.localScale = _defaultScale;
                _bIsFlipped = false;
            }

            if (!attackRight && !_bIsFlipped)
            {
                transform.localScale = new Vector3(_defaultScale.x * -1, _defaultScale.y, _defaultScale.z);
                _bIsFlipped = true;
            }
        }
        else
        {
            if (attackRight) _spriteRenderer.transform.localPosition = _defaultSpritePos;
            else _spriteRenderer.transform.localPosition = new Vector2(-_defaultSpritePos.x, _defaultSpritePos.y);
        }
    }

    protected virtual void Hitstop()
    {
        if (Data.HitstopDuration <= 0) return;
        if (!GameManager.Instance.bIsTimeFrozen) GameManager.Instance.DoHitstop(Data.HitstopDuration);
    }

    protected virtual void Knockback(GameObject hit)
    {
        if (Data.Knockback <= 0) return;
        //Debug.Log("Knockback");
        Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
        if (rb)
        {
            hit.GetComponent<IHittable>().bIsKnockedBack = true;

            //Get direction from attacker to hit
            Vector2 dir = (transform.parent.position - hit.transform.position).normalized;

            //Set knockback force to -dir * knockback
            Vector2 knockbackVector = -dir * Data.Knockback;

            //Apply force
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);
        }
    }
    #endregion

    public void ResetAtkCD()
    {
        _atkCDTimer = Data.Cooldown;
    }
}