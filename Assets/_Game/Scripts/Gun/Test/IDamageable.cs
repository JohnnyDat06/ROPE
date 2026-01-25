using UnityEngine;

public interface IDamageable
{
    public int curentHealth { get; set; }
    public int maxHealth { get; set; }

    public delegate void TakeDamageEvent(int damage);
    public event TakeDamageEvent OnTakeDamage;

    public delegate void DeathEvent(Vector3 position);
    public event DeathEvent OnDeath;

    public void TakeDamage(int damage);
}
