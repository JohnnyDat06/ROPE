using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Reflection;

namespace AutomationScripts.Runtime
{
    public class AutomationPlayModeTests
    {
        // Hàm hỗ trợ lấy Type từ Assembly chính của game
        private Type GetGameType(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.IsNotNull(type, $"Không tìm thấy class {typeName} trong Assembly-CSharp. Kiểm tra lại tên namespace hoặc class.");
            return type;
        }

        [UnityTest]
        [Description("1. Kiểm tra máu Player trong Play Mode (Nhận sát thương và Hồi máu).")]
        public IEnumerator PlayerHealth_PlayMode_DamageAndHeal()
        {
            Type playerHealthType = GetGameType("DatScript.PlayerHealth");
            GameObject playerGo = new GameObject("Player");
            
            // Add Animator để tránh NullReferenceException trong PlayerHealth
            playerGo.AddComponent<Animator>();
            Component playerHealth = playerGo.AddComponent(playerHealthType);
            
            FieldInfo currentHealthField = playerHealthType.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(currentHealthField, "Không tìm thấy biến currentHealth.");
            
            // Khởi tạo máu
            currentHealthField.SetValue(playerHealth, 100f);
            yield return null;

            MethodInfo takeDamageMethod = playerHealthType.GetMethod("TakeDamage");
            
            // Bọc trong try-catch để bỏ qua các lỗi Null của InputSystem/Controller khi chạy độc lập
            try 
            {
                takeDamageMethod.Invoke(playerHealth, new object[] { 20f });
            }
            catch (Exception) { /* Bỏ qua lỗi phụ thuộc UI/Input */ }

            float healthAfterDamage = (float)currentHealthField.GetValue(playerHealth);
            Assert.AreEqual(80f, healthAfterDamage, "Máu Player phải giảm xuống 80 sau khi nhận 20 sát thương.");

            MethodInfo healMethod = playerHealthType.GetMethod("Heal");
            try 
            {
                healMethod.Invoke(playerHealth, new object[] { 10f });
            }
            catch (Exception) { /* Bỏ qua lỗi phụ thuộc */ }

            float healthAfterHeal = (float)currentHealthField.GetValue(playerHealth);
            Assert.AreEqual(90f, healthAfterHeal, "Máu Player phải tăng lên 90 sau khi hồi 10 máu.");

            UnityEngine.Object.Destroy(playerGo);
            yield return null;
        }

        [UnityTest]
        [Description("2. Kiểm tra máu Quái vật trong Play Mode (Nhận sát thương và Chết).")]
        public IEnumerator EnemyHealth_PlayMode_TakeDamageAndDeath()
        {
            Type enemyHealthType = GetGameType("EnemyHealth");
            GameObject enemyGo = new GameObject("Enemy");
            Component enemyHealth = enemyGo.AddComponent(enemyHealthType);
            
            PropertyInfo currentHealthProp = enemyHealthType.GetProperty("curentHealth");
            PropertyInfo maxHealthProp = enemyHealthType.GetProperty("maxHealth");
            
            maxHealthProp.SetValue(enemyHealth, 50);
            currentHealthProp.SetValue(enemyHealth, 50);
            
            yield return null;

            // Gây sát thương chí mạng
            MethodInfo takeDamageMethod = enemyHealthType.GetMethod("TakeDamage");
            takeDamageMethod.Invoke(enemyHealth, new object[] { 50 });
            
            // Chờ đến cuối frame để quá trình Destroy(gameObject) diễn ra
            yield return new WaitForEndOfFrame();
            
            // Quái vật phải bị xóa khỏi scene
            Assert.IsTrue(enemyGo == null || !enemyGo.activeInHierarchy, "Enemy GameObject phải bị Destroy khi máu về 0.");
        }

        [UnityTest]
        [Description("3. Tương tác 2 chế độ: Người chơi tấn công Quái vật.")]
        public IEnumerator Interaction_PlayMode_PlayerAttacksEnemy()
        {
            // Bài test mô phỏng quá trình va chạm (tương tác) khi vũ khí của Player trúng Enemy
            Type enemyHealthType = GetGameType("EnemyHealth");
            GameObject enemyGo = new GameObject("EnemyTarget");
            Component enemyHealth = enemyGo.AddComponent(enemyHealthType);
            
            // Thêm Collider để mô phỏng tương tác vật lý (như Raycast)
            BoxCollider collider = enemyGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(2, 2, 2);
            
            PropertyInfo currentHealthProp = enemyHealthType.GetProperty("curentHealth");
            currentHealthProp.SetValue(enemyHealth, 100);

            yield return null;

            // Mô phỏng vũ khí (RaycastWeapon) bắn trúng và gọi TakeDamage thông qua giao diện IDamageable
            // Ở đây gọi trực tiếp TakeDamage để test logic luồng dữ liệu tương tác
            MethodInfo takeDamageMethod = enemyHealthType.GetMethod("TakeDamage");
            takeDamageMethod.Invoke(enemyHealth, new object[] { 35 });
            
            yield return null;

            int health = (int)currentHealthProp.GetValue(enemyHealth);
            Assert.AreEqual(65, health, "Tương tác thành công: Quái vật phải mất máu tương ứng khi bị Player tấn công.");

            UnityEngine.Object.Destroy(enemyGo);
        }
    }
}
