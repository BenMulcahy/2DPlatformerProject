
using System.Collections;
using UnityEngine;

public class MeleeAttack : Attack
{
    public override void DoAttack()
    {
        if (Data == null) {Debug.LogError("No Attack Data for attack: " + this.name); return;}

        if (!_bIsAttacking) StartCoroutine(Attacking());
        //Debug.Log("Attack!");  
    }
    
    protected IEnumerator Attacking()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        _bIsAttacking = true;

        _attackState = EAttackState.startup;
        _animator.SetTrigger("IsAttacking");

        while (_attackState == EAttackState.startup)
        {
            //Start up logic
            yield return wait;
        }

        while (_attackState == EAttackState.active)
        {
            //Attack Logic
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
            //End lag logic
            yield return wait;
        }

        _bIsAttacking = false;
    }
}

           
