using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HealthComponent : MonoBehaviour, IHittable
{
    public delegate void OnTakeDamage();
    public event OnTakeDamage onTakeDamage;

    [SerializeField] float _maxHealth = 100f;
    float currentHealth;

    void Start()
    {
        currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log(transform.gameObject.name + " Took " +  damage + " damage");
        currentHealth -= damage;
        onTakeDamage?.Invoke();
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
