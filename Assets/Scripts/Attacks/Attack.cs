using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
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

    private void OnValidate()
    {
        SetData();
    }

    private void Start()
    {
        SetData();
        _atkCDTimer = 0;
    }

    void SetData()
    {
        if (!Data) Data = Resources.Load<AttackData>("DefaultAttack");
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _spriteRenderer.sprite = Data.Sprite;
        _animator = GetComponent<Animator>();
        _defaultSpritePos = _spriteRenderer.transform.localPosition;
        _defaultScale = transform.localScale;
        Data.AttackState = AttackData.EAttackState.NULL;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var hitBox in Data.HitSphereBounds)
        {
            Vector2 hitpos = new Vector2(transform.position.x + (_bShouldAttackRight ? hitBox.x : -hitBox.x), transform.position.y + hitBox.y);
            Gizmos.DrawWireSphere((Vector3)hitpos, hitBox.z);
        }
    }

    private void Update()
    {
        if(!_bIsAttacking) _atkCDTimer -= Time.deltaTime;
    }

    
    public virtual void DoAttack()
    {
        if (Data == null) {Debug.LogError("No Attack Data for attack: " + this.name); return;}

        _animator.SetTrigger("IsAttacking");
        if(!_bIsAttacking) StartCoroutine(Attacking());
        //Debug.Log("Attack!");
    }
    

    public virtual IEnumerator Attacking()
    {
        _bIsAttacking = true;

        while(Data.AttackState == AttackData.EAttackState.startup)
        {
            //Start up logic
            yield return null;
        }

        while(Data.AttackState == AttackData.EAttackState.active)
        {
            if (AttackHitCheck())
            {
                Debug.Log("hit: " + _hits.Count + " enemies");
                for (int i = 0; i < _hits.Count; i++)
                {
                    //Deal Damage
                    IHittable hittable = _hits[i].gameObject.GetComponent<IHittable>();
                    if (hittable != null && !hittable.bHasBeenHitThisInstance)
                    {
                        hittable.TakeDamage(Data.Damage);
                        hittable.bHasBeenHitThisInstance = true;
                    }
                }
            }
            yield return null;
        }

        while(Data.AttackState == AttackData.EAttackState.ending)
        {
            //End lag logic
            yield return null;
        }

        _bIsAttacking = false;
    }

    //Reset Hit state on all items hit
    void ResetHits()
    {
        foreach (Collider2D hit in _hits)
        {
            if(hit.GetComponent<IHittable>() != null) //Redundancy check
            {
                hit.GetComponent<IHittable>().bHasBeenHitThisInstance = false;
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
        _hits.Clear();
        foreach (var hitBox in Data.HitSphereBounds)
        {
            Vector2 hitpos = new Vector2(transform.position.x + (_bShouldAttackRight ? hitBox.x : -hitBox.x), transform.position.y + hitBox.y);

            RaycastHit2D[] tmp = Physics2D.CircleCastAll(hitpos, hitBox.z, transform.forward, Data.AttackLayer);
            Debug.DrawRay(hitpos, _bShouldAttackRight ? Vector2.right : Vector2.left, Color.cyan, 0.2f);

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

    public void SetAttackState(AttackData.EAttackState newState)
    {
        Data.AttackState = newState;
        if (newState == AttackData.EAttackState.NULL)
        {
            ResetHits();
            _atkCDTimer = Data.Cooldown;
        }
    }

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
}
