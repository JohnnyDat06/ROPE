using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _curentHealth;

    public int curentHealth { get => _curentHealth; set => _curentHealth = value; }
    public int maxHealth { get => _maxHealth; set => _maxHealth = value; }

    public event IDamageable.TakeDamageEvent OnTakeDamage;
    public event IDamageable.DeathEvent OnDeath;

    private void OnEnable()
    {
        curentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        int damageTaken = Mathf.Clamp(damage, 0, curentHealth);

        curentHealth -= damageTaken;

        if (damageTaken != 0)
        {
            OnTakeDamage?.Invoke(damageTaken);
        }

        if (curentHealth == 0 && damageTaken != 0)
        {
            OnDeath?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }
}
