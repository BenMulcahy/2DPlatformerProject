using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IHittable
{
    [SerializeField] float _maxHealth = 100f;
    float currentHealth;

    void Start()
    {
        currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
    }
    public void RecoverHealth(float healthRestored)
    {
        currentHealth += healthRestored;
    }
    public void ResetHealth()
    {
        currentHealth = _maxHealth;
    }
    public float GetCurrentHealth() { return currentHealth; }
}
