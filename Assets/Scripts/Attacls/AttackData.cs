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

    public EAttackType AttackType = EAttackType.melee; 
    public float Damage = 5f;
    public float Duration = 0.5f;
    public float Cooldown = 0.8f;
    public float IFrameDuration = 0f; //if 0 -> no i frames
    public Sprite Sprite;
}
