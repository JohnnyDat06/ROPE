using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ROPE.Tests
{
    public class EnemyHitboxTest
    {
        private readonly System.Collections.Generic.List<Object> m_TestObjects =
            new System.Collections.Generic.List<Object>();

		[SetUp]
		public void SetUp()
		{
			//EventManager.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (Object obj in m_TestObjects)
			{
				if (obj != null)
				{
					Object.DestroyImmediate(obj);
				}
			}
			m_TestObjects.Clear();

			// Dọn dẹp Events
			//EventManager.Clear();
		}

		private GameObject CreateTestObject(string name)
		{
			var obj = new GameObject(name);
			m_TestObjects.Add(obj);
			return obj;
		}

        private (EnemyHitbox, EnemyHealth) SetupEnemyHitbox(EnemyHitbox.HitboxType type)
        {
            // 1. Tạo Enemy
            var enemyGo = CreateTestObject("Enemy_Root");
            var health = enemyGo.AddComponent<EnemyHealth>();
            
            // Khởi tạo máu qua Reflection
            health.maxHealth = 100;
            var onEnableMethod = typeof(EnemyHealth).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onEnableMethod != null) onEnableMethod.Invoke(health, null);

            // 2. Tạo Hitbox (Child)
            var hitboxGo = CreateTestObject("Hitbox_" + type.ToString());
            hitboxGo.transform.SetParent(enemyGo.transform);
            
            var hitbox = hitboxGo.AddComponent<EnemyHitbox>();
            hitbox.MainHealth = health;
            hitbox.Type = type;

            // 3. Tạo WeakPoint visual nếu là loại điểm yếu
            if (type == EnemyHitbox.HitboxType.WeakPoint)
            {
                var wpVisual = CreateTestObject("WeakPoint_Visual");
                wpVisual.transform.SetParent(hitboxGo.transform);
                
                // Gán vào field private 'WeakPoint' qua Reflection
                var wpField = typeof(EnemyHitbox).GetField("WeakPoint", BindingFlags.NonPublic | BindingFlags.Instance);
                if (wpField != null) wpField.SetValue(hitbox, wpVisual);
                
                hitbox.WeakPointMaxHealth = 50;
                
                // Gọi Start() thủ công để khởi tạo máu điểm yếu
                var startMethod = typeof(EnemyHitbox).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                if (startMethod != null) startMethod.Invoke(hitbox, null);
            }

            return (hitbox, health);
        }

        // ── Tests ─────────────────────────────────────────────────────

        // 1. Kiểm tra sát thương loại Normal: Sát thương truyền đi phải giữ nguyên tỉ lệ 1:1
        [Test]
        public void TestNormalDamageMultiplier()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.Normal);

            // Bắn 10 sát thương vào hitbox thường
            hitbox.TakeDamage(10);

            // Kiểm tra máu của Enemy qua property 'curentHealth'
            Assert.AreEqual(90, health.curentHealth, "Sát thương Normal phải trừ đúng 10 máu (100 - 10 = 90)");
        }

        // 2. Kiểm tra sát thương loại Critical: Sát thương phải được nhân lên theo hệ số DamageMultiplier
        [Test]
        public void TestCriticalDamageMultiplier()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.Critical);
            hitbox.DamageMultiplier = 2.5f; // Thiết lập hệ số 2.5x

            // Bắn 10 sát thương
            hitbox.TakeDamage(10);

            // 10 * 2.5 = 25 damage
            Assert.AreEqual(75, health.curentHealth, "Sát thương Critical 2.5x phải trừ 25 máu (100 - 25 = 75)");
        }

        // 3. Kiểm tra logic phá hủy điểm yếu (Weak Point): Phải bật cờ IsBroken và ẩn GameObject visual
        [Test]
        public void TestWeakPointBreakingState()
        {
            var (hitbox, health) = SetupEnemyHitbox(EnemyHitbox.HitboxType.WeakPoint);
            
            // Lấy reference visual object để kiểm tra sau này
            var wpField = typeof(EnemyHitbox).GetField("WeakPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            GameObject wpVisual = (GameObject)wpField.GetValue(hitbox);

            // Bắn sát thương bằng đúng máu điểm yếu (50)
            hitbox.TakeDamage(50);

            // Kiểm tra trạng thái
            Assert.IsTrue(hitbox.IsBroken, "Biến IsBroken phải là true sau khi điểm yếu hết máu");
            Assert.IsFalse(wpVisual.activeSelf, "GameObject WeakPoint phải bị ẩn đi (SetActive false)");
            
            // Kiểm tra máu Enemy vẫn bị trừ (điểm yếu vẫn tính sát thương cho máu chính)
            Assert.AreEqual(50, health.curentHealth, "Enemy vẫn phải nhận 50 sát thương vào máu chính");
        }
    }
}
