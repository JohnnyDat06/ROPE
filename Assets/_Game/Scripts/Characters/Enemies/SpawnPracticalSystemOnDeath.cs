using System;
using UnityEngine;

[RequireComponent(typeof(IDamageable))] // Ensure that the GameObject has a component that implements IDamageable
public class SpawnPracticalSystemOnDeath : MonoBehaviour
{
    [SerializeField] private ParticleSystem deathSystem;
    public IDamageable damageable;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    private void OnEnable()
    {
        damageable.OnDeath += Damageable_OnDeath;
    }

    private void Damageable_OnDeath(Vector3 position)
    {
        Instantiate(deathSystem, position, Quaternion.identity);
    }
}
