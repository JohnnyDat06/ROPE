using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StarterAssets;
using DatScript;

namespace ROPE.Tests
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

            // Gọi hàm Start bằng Reflection để tự động GetComponent()
            var startMethod = typeof(PlayerHealth).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            if (startMethod != null) startMethod.Invoke(health, null);

            // Tự thiết lập/đảm bảo giá trị máu ban đầu bằng Reflection theo ý muốn
            var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (maxHealthField != null) maxHealthField.SetValue(health, 100f);
			Assert.AreEqual(100f, maxHealthField.GetValue(health));

            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentHealthField != null) currentHealthField.SetValue(health, 100f);
			Assert.AreEqual(100f, currentHealthField.GetValue(health));


            return health;
        }

        // ── Tests ─────────────────────────────────────────────────────

        // Kiểm tra PlayerHealth: Xác nhận người chơi nhận sát thương và property máu giảm chính xác.
        [Test]
        public void TakeDamageReducesHealth()
        {
            var health = SetupPlayerHealth();
            
            // Giả sử máu tối đa là 100, sát thương 25
            health.TakeDamage(25f);

            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(currentHealthField);
            
            float currentHealth = (float)currentHealthField.GetValue(health);
            Assert.AreEqual(75f, currentHealth);
        }

        // Kiểm tra PlayerHealth: Xác nhận máu không thể giảm xuống dưới 0 khi nhận sát thương chí mạng
        // phần phụ kiện điều khiển của người chơi cũng bị tắt.
        [Test]
        public void TakeDamageLethalTriggersDeath()
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
        public void HealClampsToMaxHealth()
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
        public void ResetHealthRestoresState()
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
        // Kiểm tra PlayerHealth: Nhận sát thương không gây chết sẽ reset các input di chuyển và hành động
        // (Ngắt ngắm bắn, ngắt phím chạy, dừng việc bấm phím di chuyển).
        [Test]
        public void TakeDamageNonLethalResetsInput()
        {
            var health = SetupPlayerHealth();
            var go = health.gameObject;
            var input = go.GetComponent<StarterAssetsInputs>();

            // 1. Giả lập người chơi đang vừa chạy, vừa bắn chéo và bấm phím di chuyển
            input.shoot = true;
            input.sprint = true;
            input.move = new Vector2(1f, 1f);

            // 2. Nhận sát thương xước xát (10 damage, chưa chết)
            health.TakeDamage(10f);

            // 3. Kiểm tra các flags input xem đã được ép dừng lại chưa
            Assert.IsFalse(input.shoot, "Khi bị thương, biến shoot phải được ngắt (bằng false)");
            Assert.IsFalse(input.sprint, "Khi bị thương, biến sprint của người chơi phải dừng lại (bằng false)");
            Assert.AreEqual(Vector2.zero, input.move, "Khi bị thương, di chuyển phải bị reset về Vector2.zero");
        }
    }   
}
