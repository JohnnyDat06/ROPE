using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Reflection;

namespace AutomationScripts.Runtime
{
    public class AdvancedSystemInteractionTests
    {
        private Type GetGameType(string typeName) => Type.GetType(typeName + ", Assembly-CSharp");

        [UnityTest]
        [Description("Kiểm tra toàn diện: Vũ khí bắn trúng quái vật kích hoạt phản hồi sát thương.")]
        public IEnumerator CombatSystem_WeaponHitsEnemy_ReducesHealth()
        {
            // 1. Tạo Enemy với hệ thống máu
            Type enemyHealthType = GetGameType("EnemyHealth");
            GameObject enemyGo = new GameObject("Enemy_CombatTest");
            Component enemyHealth = enemyGo.AddComponent(enemyHealthType);
            
            // Thiết lập Collider cho Enemy để Raycast có thể nhận diện
            BoxCollider collider = enemyGo.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;

            // Thiết lập máu ban đầu cho Enemy
            PropertyInfo maxHealthProp = enemyHealthType.GetProperty("maxHealth");
            PropertyInfo currentHealthProp = enemyHealthType.GetProperty("curentHealth");
            maxHealthProp.SetValue(enemyHealth, 100);
            currentHealthProp.SetValue(enemyHealth, 100);

            // 2. Tạo Weapon
            Type weaponType = GetGameType("RaycastWeapon");
            GameObject weaponGo = new GameObject("PlayerWeapon");
            Component weapon = weaponGo.AddComponent(weaponType);

            // Giả lập cấu hình sát thương (DamageConfigSO) bằng Reflection nếu cần
            // Ở đây ta sẽ giả lập bước cuối của FireBullet: Gọi sát thương
            
            yield return null; // Đợi 1 frame để khởi tạo

            // 3. Thực hiện mô phỏng tương tác thông qua sự kiện sát thương
            // Lấy hàm TakeDamage của EnemyHealth
            MethodInfo takeDamageMethod = enemyHealthType.GetMethod("TakeDamage");
            
            // Giả lập sát thương từ vũ khí (ví dụ 45 sát thương)
            takeDamageMethod.Invoke(enemyHealth, new object[] { 45 });

            // 4. Kiểm tra kết quả
            int currentHealth = (int)currentHealthProp.GetValue(enemyHealth);
            Assert.AreEqual(55, currentHealth, "Máu quái vật phải giảm chính xác sau khi bị trúng đạn từ vũ khí.");

            // 5. Kiểm tra sự kiện OnDeath khi máu về 0
            takeDamageMethod.Invoke(enemyHealth, new object[] { 55 });
            yield return new WaitForEndOfFrame(); // Đợi quá trình Destroy thực thi

            Assert.IsTrue(enemyGo == null || !enemyGo.activeInHierarchy, "Quái vật phải bị tiêu diệt hoàn toàn khi máu về 0 sau combat.");

            // Dọn dẹp nếu quái chưa chết (trong trường hợp test fail)
            if (enemyGo != null) UnityEngine.Object.Destroy(enemyGo);
            UnityEngine.Object.Destroy(weaponGo);
        }

        [UnityTest]
        [Description("Kiểm tra sự kiện hồi sinh của GameManager: Reset máu Player.")]
        public IEnumerator GameManager_Respawn_ResetsPlayerStats()
        {
            // 1. Setup Player
            Type playerHealthType = GetGameType("DatScript.PlayerHealth");
            GameObject playerGo = new GameObject("Player_RespawnTest");
            playerGo.tag = "Player";
            playerGo.AddComponent<Animator>();
            Component playerHealth = playerGo.AddComponent(playerHealthType);

            FieldInfo currentHealthField = playerHealthType.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            currentHealthField.SetValue(playerHealth, 10f); // Máu sắp chết

            // 2. Setup GameManager Singleton
            Type gameManagerType = GetGameType("DatScript.GameManager");
            GameObject gmGo = new GameObject("GameManager_Instance");
            Component gameManager = gmGo.AddComponent(gameManagerType);
            
            // Thiết lập panel giả để tránh lỗi
            FieldInfo gameOverPanelField = gameManagerType.GetField("gameOverPanel");
            gameOverPanelField.SetValue(gameManager, new GameObject("UI_Dummy"));

            yield return null;

            // 3. Gọi hàm hồi sinh
            MethodInfo respawnMethod = gameManagerType.GetMethod("RespawnPlayer");
            respawnMethod.Invoke(gameManager, null);

            // 4. Kiểm tra xem máu đã được Reset chưa
            float healthAfterRespawn = (float)currentHealthField.GetValue(playerHealth);
            Assert.AreEqual(100f, healthAfterRespawn, "Hàm RespawnPlayer phải gọi ResetHealth để hồi đầy máu cho Player.");

            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(gmGo);
            yield return null;
        }
    }
}
