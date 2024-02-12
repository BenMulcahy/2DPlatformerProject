using UnityEngine;

public class HealthComponent : MonoBehaviour, IHittable
{
    public delegate void OnTakeDamage();
    public event OnTakeDamage onTakeDamage;

    public delegate void OnOutOfHealth();
    public event OnOutOfHealth onOutOfHealth;


    [SerializeField] float _maxHealth = 100f;
    float currentHealth;

    public bool bHasBeenHitThisInstance { get; set; }

    void Start()
    {
        currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log(transform.gameObject.name + " Took " +  damage + " damage");

        bHasBeenHitThisInstance = true;

        currentHealth -= damage;
        onTakeDamage?.Invoke();

        if (currentHealth <= 0) onOutOfHealth?.Invoke();
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
