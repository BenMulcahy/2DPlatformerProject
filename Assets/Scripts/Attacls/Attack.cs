using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class Attack : MonoBehaviour
{
    [SerializeField] AttackData _attackData;
    [SerializeField] Vector3[] _hitSphereBounds = { Vector3.forward };
    [SerializeField] LayerMask _attackLayer = ~0;

    List<Collider2D> _hits = new List<Collider2D>();
    SpriteRenderer _spriteRenderer;

    private void OnValidate()
    {
        SetData();
    }

    private void Start()
    {
        SetData();
    }

    void SetData()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = _attackData.Sprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var hitBox in _hitSphereBounds)
        {
            Gizmos.DrawWireSphere(new Vector3(transform.position.x + hitBox.x, transform.position.y + hitBox.y, 0), hitBox.z);
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.J)) { DoAttack(); }
    }

    void DoAttack()
    {
        if (_attackData == null) {Debug.LogError("No Attack Data for attack: " + this.name); return;}
        
        if (AttackHitCheck())
        {
            Debug.Log("hit: " + _hits.Count + " enemies");
            for (int i = 0; i < _hits.Count; i++)
            {
                //Deal Damage
                IHittable hittable = _hits[i].gameObject.GetComponent<IHittable>();
                if (hittable != null)
                {
                    Debug.Log("Deal Damage");
                    hittable.TakeDamage(_attackData.Damage);
                }
                else Debug.Log("hit not hittable");

            }
        }
    }

    bool AttackHitCheck()
    {
        _hits.Clear();
        Debug.Log("Check for hits");

        foreach (var hitBox in _hitSphereBounds)
        {
            RaycastHit2D[] tmp = Physics2D.CircleCastAll(new Vector2(transform.position.x + hitBox.x, transform.position.y + hitBox.y), hitBox.z, transform.forward, _attackLayer);
            if (tmp.Length > 0)
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!_hits.Contains(tmp[i].collider)) _hits.Add(tmp[i].collider);
                    else Debug.LogWarning("Already in hit list");
                }
            }
        }

        if (_hits.Count != 0) return true;
        else return false;
    }
}
