using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditModeTests
{
    public class MockHealth
    {
        public float MaxHealth;
        public float CurrentHealth;
        public bool Invincible;

        bool m_IsDead;

        public bool IsDead => m_IsDead;

        public MockHealth(float maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float damage, GameObject damageSource)
        {
            if (Invincible) return;

            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

            HandheldDeath();
        }

        public void Kill()
        {
            CurrentHealth = 0f;
            HandheldDeath();
        }

        public bool canPickup() => CurrentHealth < MaxHealth;

        public float GetRatio() => CurrentHealth / MaxHealth;

        public bool IsCritical() => GetRatio() <= 0.3f;

        void HandheldDeath()
        {
            if (m_IsDead) return;

            if (CurrentHealth <= 0f)
            {
                m_IsDead = true;
            }
        }
    }
}
//public class FPSMicrogameTests
//{
//    // A Test behaves as an ordinary method
//    [Test]
//    public void FPSMicrogameTestsSimplePasses()
//    {
//        // Use the Assert class to test conditions
//    }

//    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
//    // `yield return null;` to skip a frame.
//    [UnityTest]
//    public IEnumerator FPSMicrogameTestsWithEnumeratorPasses()
//    {
//        // Use the Assert class to test conditions.
//        // Use yield to skip a frame.
//        yield return null;
//    }
//}
