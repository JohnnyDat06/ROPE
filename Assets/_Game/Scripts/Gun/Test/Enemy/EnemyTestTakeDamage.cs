using System;
using UnityEngine;

public class EnemyTestTakeDamage : MonoBehaviour
{
    public EnemyHealth enemyHealth;

    private void Start()
    {
        enemyHealth.OnTakeDamage += EnemyHealth_OnTakeDamage;
        enemyHealth.OnDeath += Death_OnDeath;
    }

    private void EnemyHealth_OnTakeDamage(int damage)
    {
        // Handle reactions to taking damage for testing purposes
        // Handle animations, sounds, etc.
    }

    private void Death_OnDeath(Vector3 position)
    {
        // Stop all actions on death for testing purposes
        // Handle animations, movements, etc.
    }
}
