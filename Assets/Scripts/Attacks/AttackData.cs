using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Data", menuName = "ScritableObjects/New Attack")]
public class AttackData : ScriptableObject
{
    public enum EAttackType
    {
        melee,ranged
    }

    public enum EAttackState
    {
        startup, active, ending, NULL
    }

    [field: SerializeField] public EAttackType AttackType { get; private set; } = EAttackType.melee;
    public EAttackState AttackState = EAttackState.NULL;
    [field: SerializeField] public float Damage { get; private set; } = 5f;
    [field: SerializeField] public float Cooldown { get; private set; } = 0.8f;
    [field: SerializeField] public float IFrameDuration { get; private set; } = 0f; //if 0 -> no i frames
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public AnimationClip AttackAnimation { get; private set; }
    [field: SerializeField] public Vector3[] HitSphereBounds { get; private set; } = { Vector3.forward };
    [field: SerializeField] public LayerMask AttackLayer { get; private set; } = ~0;

    public float Duration { get; private set; }

    private void OnEnable()
    {
        if(AttackAnimation)
        {
            Duration = AttackAnimation.length;
        }
    }
}
