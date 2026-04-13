using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace ROPE.Tests
{
    public class EnemyHitboxTest
    {
        private readonly System.Collections.Generic.List<Object> m_TestObjects = new System.Collections.Generic.List<Object>();

        // Cleanup
        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_TestObjects)
                if (obj != null) Object.DestroyImmediate(obj);
            m_TestObjects.Clear();
        }

        private GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        // Setup
        private (EnemyHitbox, EnemyHealth) SetupEnemyHitbox(EnemyHitbox.HitboxType type, int maxHealth = 100, int wpMaxHealth = 50)
        {
            var enemyGo = CreateTestObject("Enemy_Root");
            var health = enemyGo.AddComponent<EnemyHealth>();
            health.maxHealth = maxHealth;
            health.curentHealth = maxHealth;

            var hitboxGo = CreateTestObject($"Hitbox_{type}");
            hitboxGo.transform.SetParent(enemyGo.transform);
            
            var hitbox = hitboxGo.AddComponent<EnemyHitbox>();
            hitbox.MainHealth = health;
            hitbox.Type = type;
            hitbox.WeakPointMaxHealth = wpMaxHealth;

            if (type == EnemyHitbox.HitboxType.WeakPoint)
            {
                var wpVisual = CreateTestObject("WeakPoint_Visual");
                wpVisual.transform.SetParent(hitboxGo.transform);
                
                typeof(EnemyHitbox).GetField("WeakPoint", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(hitbox, wpVisual);
                typeof(EnemyHitbox).GetField("_currentWeakPointHealth", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(hitbox, wpMaxHealth);
            }

            return (hitbox, health);
        }

        // Test: Kiểm tra tỉ lệ nhân sát thương (Normal x1, Critical x2, x3.5)
        [TestCase(EnemyHitbox.HitboxType.Normal, 1.0f, 10, 90)]
        [TestCase(EnemyHitbox.HitboxType.Critical, 2.0f, 10, 80)]
        [TestCase(EnemyHitbox.HitboxType.Critical, 3.5f, 10, 65)]
        public void TestDamageMultipliers(EnemyHitbox.HitboxType type, float multiplier, int damage, int expectedHealth)
        {
            var (hitbox, health) = SetupEnemyHitbox(type);
            hitbox.DamageMultiplier = multiplier;

            hitbox.TakeDamage(damage);

            Assert.AreEqual(expectedHealth, health.curentHealth);
        }

        // Test: Kiểm tra logic phá hủy điểm yếu (ẩn visual và vô hiệu hóa hitbox)
        [Test]
        public void TestWeakPointBreakingLogic()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.WeakPoint, 100, 50);
            var wpVisual = (GameObject)typeof(EnemyHitbox).GetField("WeakPoint", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hitbox);

            hitbox.TakeDamage(50);

            Assert.IsTrue(hitbox.IsBroken);
            Assert.IsFalse(wpVisual.activeSelf);
            Assert.IsFalse(hitbox.gameObject.activeSelf);
        }

        // Test: Kiểm tra sát thương sau khi điểm yếu đã hỏng (phải quay về sát thương Normal)
        [Test]
        public void TestWeakPointDamageAfterBroken()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.WeakPoint, 200, 50);
            
            hitbox.TakeDamage(50);
            Assert.IsTrue(hitbox.IsBroken);

            hitbox.gameObject.SetActive(true); 
            hitbox.TakeDamage(20);

            Assert.AreEqual(130, health.curentHealth);
            
            int wpHealth = (int)typeof(EnemyHitbox).GetField("_currentWeakPointHealth", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hitbox);
            Assert.AreEqual(0, wpHealth);
        }

        // Test: Kiểm tra tính an toàn, không gây crash nếu thiếu MainHealth
        [Test]
        public void TestHitboxSafetyWhenHealthIsNull()
        {
            var hitbox = CreateTestObject("LoneHitbox").AddComponent<EnemyHitbox>();
            hitbox.MainHealth = null;

            Assert.DoesNotThrow(() => hitbox.TakeDamage(10));
        }

        // Test: Kiểm tra sát thương vượt ngưỡng (Overkill) - Máu ghim về 0
        [Test]
        public void TestOverkillDamageClampsAtZero()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.Normal, 100);
            health.curentHealth = 60;
            
            hitbox.TakeDamage(80);

            Assert.AreEqual(0, health.curentHealth);
        }
    }
}