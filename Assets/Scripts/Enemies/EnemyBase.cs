using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyBase : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponent<HealthComponent>().onOutOfHealth += Die;
    }

    private void OnDisable()
    {
        GetComponent<HealthComponent>().onOutOfHealth -= Die;
    }

    /// <summary>
    /// When overidden, call base after implementing overridden code
    /// </summary>
    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
