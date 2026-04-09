using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StarterAssets;
using DatScript;

namespace EditModeTests
{
    public class PlayerHealthTest
    {
        // ── Shared state ──────────────────────────────────────────────
        readonly System.Collections.Generic.List<Object> m_TestObjects =
            new System.Collections.Generic.List<Object>();

        // ── Lifecycle ─────────────────────────────────────────────────
        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_TestObjects)
                if (obj != null) Object.DestroyImmediate(obj);
            m_TestObjects.Clear();
        }

        // ── Helper ────────────────────────────────────────────────────
        GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        private PlayerHealth SetupPlayerHealth()
        {
            var go = CreateTestObject("Player");
            go.SetActive(false); // Ngắt Awake/Start mặc định

            // Phải Add các Component bắt buộc vì hàm TakeDamage gọi trực tiếp (chứ không check null)
            go.AddComponent<StarterAssetsInputs>();
            go.AddComponent<ThirdPersonController>();
            go.AddComponent<ActiveWeapon>();
            go.AddComponent<Animator>();
            var health = go.AddComponent<PlayerHealth>();

            // Dùng Reflection gọi Start thủ công trên PlayerHealth để gán các instance tham chiếu bên trong
            var startMethod = typeof(PlayerHealth).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            if (startMethod != null) startMethod.Invoke(health, null);

            return health;
        }

        // ── Tests ─────────────────────────────────────────────────────

        // Kiểm tra PlayerHealth: Xác nhận người chơi nhận sát thương và property máu giảm chính xác.
        [Test]
        public void TestTakeDamageReducesHealth()
        {
            var health = SetupPlayerHealth();
            
            // Giả sử máu tối đa là 100, sát thương 25
            health.TakeDamage(25f);

            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(currentHealthField, "Field private currentHealth phải ở đây");
            
            float currentHealth = (float)currentHealthField.GetValue(health);
            Assert.AreEqual(75f, currentHealth, "Máu hiện tại phải giảm xuống 75 sau khi dính 25 damage");
        }

        // Kiểm tra PlayerHealth: Xác nhận máu không thể giảm xuống dưới 0 khi nhận sát thương chí mạng
        // phần phụ kiện điều khiển của người chơi cũng bị tắt.
        [Test]
        public void TestTakeDamageBelowZeroTriggersDeathStateAndClampsHealth()
        {
            var health = SetupPlayerHealth();
            var go = health.gameObject;
            
            // Sát thương vượt mức máu tối đa (chết luôn)
            health.TakeDamage(999f);

            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            float currentHealth = (float)currentHealthField.GetValue(health);

            Assert.AreEqual(0f, currentHealth, "Máu phải bị chặn (Clamp) ở mức 0 và không kéo số âm");

            // Kiểm tra trạng thái disable logic
            var controller = go.GetComponent<ThirdPersonController>();
            var input = go.GetComponent<StarterAssetsInputs>();
            var weapon = go.GetComponent<ActiveWeapon>();

            Assert.IsFalse(controller.enabled, "ThirdPersonController phải bị vô hiệu hóa khi nhân vật đã chết");
            Assert.IsFalse(input.enabled, "StarterAssetsInputs phải mất sóng vô hiệu hóa");
            Assert.IsFalse(weapon.enabled, "ActiveWeapon Súng phải bị cất đi/disable");
        }

        // Kiểm tra PlayerHealth: Hàm Heal có chức năng tăng máu nhưng ghim lại không cho vượt trần maxHealth.
        [Test]
        public void TestHealIncreasesHealthButRestrictsToMax()
        {
            var health = SetupPlayerHealth();
            
            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Cưỡng bách máu tụt xuống 50 để test chức năng bơm máu
            currentHealthField.SetValue(health, 50f);

            // Bơm 20 máu
            health.Heal(20f);
            Assert.AreEqual(70f, (float)currentHealthField.GetValue(health), "Hồi 20hp sẽ làm máu tăng lên 70");

            // Bơm tiếp lượng y tế quá lố (vượt giới hạn)
            health.Heal(500f);
            Assert.AreEqual(100f, (float)currentHealthField.GetValue(health), "Lượng máu không được khôi phục vượt qua maxHealth ban đầu (100)");
        }

        // Kiểm tra PlayerHealth: Hàm ResetHealth phục hồi trạng thái sống cùng toàn quyền di chuyển.
        [Test]
        public void TestResetHealthRestoresComponentsAndHealth()
        {
            var health = SetupPlayerHealth();
            var go = health.gameObject;

            // Chết đi
            health.TakeDamage(100f);

            // Cải tử hoàn sinh
            health.ResetHealth();

            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(100f, (float)currentHealthField.GetValue(health), "ResetHealth phải trả máu lại vị trí đầy (100)");

            // Controller có lấy lại nhận diện hay không?
            var controller = go.GetComponent<ThirdPersonController>();
            var input = go.GetComponent<StarterAssetsInputs>();
            
            Assert.IsTrue(controller.enabled, "ThirdPersonController phải được bật lại hệ thống di chuyển");
            Assert.IsTrue(input.enabled, "StarterAssetsInputs phải được kích hoạt trở lại làm việc");
        }
    }
}
