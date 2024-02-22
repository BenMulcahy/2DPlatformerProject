using Cinemachine;
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

    [field: SerializeField] public EAttackType AttackType { get; private set; } = EAttackType.melee;
    [field: SerializeField] public float Damage { get; private set; } = 5f;
    [field: SerializeField] public float Knockback { get; private set; } = 0f;
    [field: SerializeField] public float Cooldown { get; private set; } = 0.8f;
    [field: SerializeField] public float HitstopDuration { get; private set; } = 0f;
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public AnimationClip AttackAnimation { get; private set; }
    [field: SerializeField] public Vector3[] HitSphereBounds { get; private set; } = { Vector3.forward };
    [field: SerializeField] public LayerMask AttackLayer { get; private set; } = ~0;
    [field: SerializeField] public float CameraShakeIntensity { get; private set; } = 0f;
    [field: SerializeField] public float CameraShakeDuration { get; private set; }
    [field: SerializeField] public NoiseSettings CameraShakeNoiseSettings { get; private set; }

    public float Duration { get; private set; }

    private void OnEnable()
    {
        if(AttackAnimation)
        {
            Duration = AttackAnimation.length;
        }
    }
}
