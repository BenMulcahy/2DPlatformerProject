using UnityEngine;

public class HealthComponent : MonoBehaviour, IHittable
{
    public delegate void OnTakeDamage();
    public event OnTakeDamage onTakeDamage;

    public delegate void OnOutOfHealth();
    public event OnOutOfHealth onOutOfHealth;


    public bool bCanTakeDamage = true;
    [SerializeField] float _maxHealth = 100f;
    float currentHealth;

    public bool bHasBeenHitThisInstance { get; set; }

    [field: Header("--- Knockback ---")]
    [field: SerializeField] public bool bCanBeKnockedBack { get; set; }
    [Range(0,0.25f)] [field: SerializeField] public float KnockbackRecoveryLerp { get; protected set; } = 0.1f;
    public bool bIsKnockedBack { get; set; }

    void Start()
    {
        currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (!bCanTakeDamage) return;
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
