using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AutomationScripts.Editor
{
    public class AutomationEditModeTests
    {
        // Hàm hỗ trợ lấy Type từ Assembly chính của game
        private Type GetGameType(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.IsNotNull(type, $"Không tìm thấy class {typeName} trong Assembly-CSharp. Kiểm tra lại tên namespace hoặc class.");
            return type;
        }

        [Test]
        [Description("1. Kiểm tra Component PlayerHealth: Các chỉ số máu mặc định.")]
        public void PlayerHealth_EditMode_CheckDefaults()
        {
            Type playerHealthType = GetGameType("DatScript.PlayerHealth");

            GameObject go = new GameObject("TestPlayer");
            Component healthComp = go.AddComponent(playerHealthType);
            
            FieldInfo maxHealthField = playerHealthType.GetField("maxHealth");
            Assert.IsNotNull(maxHealthField, "Không tìm thấy biến maxHealth trong PlayerHealth.");

            float maxHealth = (float)maxHealthField.GetValue(healthComp);
            Assert.AreEqual(100f, maxHealth, "Máu tối đa mặc định của Player phải là 100.");
            
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        [Description("2. Kiểm tra Component EnemyHealth: Các chỉ số quái vật mặc định.")]
        public void EnemyHealth_EditMode_CheckDefaults()
        {
            Type enemyHealthType = GetGameType("EnemyHealth");

            GameObject go = new GameObject("TestEnemy");
            Component healthComp = go.AddComponent(enemyHealthType);
            
            FieldInfo isBossField = enemyHealthType.GetField("_isBoss", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(isBossField, "Không tìm thấy biến _isBoss trong EnemyHealth.");
            
            bool isBoss = (bool)isBossField.GetValue(healthComp);
            Assert.IsFalse(isBoss, "Mặc định quái vật không phải là Boss.");

            FieldInfo maxHealthField = enemyHealthType.GetField("_maxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(maxHealthField, "Không tìm thấy biến _maxHealth trong EnemyHealth.");
            
            int maxHealth = (int)maxHealthField.GetValue(healthComp);
            Assert.AreEqual(100, maxHealth, "Máu tối đa mặc định của Enemy phải là 100.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        [Description("3. Kiểm tra logic trừ máu cơ bản của EnemyHealth mà không cần chạy Game.")]
        public void EnemyHealth_EditMode_TakeDamage_UpdatesHealth()
        {
            Type enemyHealthType = GetGameType("EnemyHealth");
            GameObject go = new GameObject("TestEnemy");
            Component healthComp = go.AddComponent(enemyHealthType);
            
            PropertyInfo currentHealthProp = enemyHealthType.GetProperty("curentHealth");
            Assert.IsNotNull(currentHealthProp, "Không tìm thấy property curentHealth.");
            
            // Gán máu hiện tại là 100
            currentHealthProp.SetValue(healthComp, 100);

            // Lấy hàm TakeDamage
            MethodInfo takeDamageMethod = enemyHealthType.GetMethod("TakeDamage");
            Assert.IsNotNull(takeDamageMethod, "Không tìm thấy hàm TakeDamage.");

            // Gọi hàm TakeDamage(20) - Máu còn 80
            try 
            {
                takeDamageMethod.Invoke(healthComp, new object[] { 20 });
            }
            catch (Exception ex)
            {
                Assert.Fail($"Lỗi khi gọi TakeDamage: {ex.InnerException?.Message ?? ex.Message}");
            }
            
            int health80 = (int)currentHealthProp.GetValue(healthComp);
            Assert.AreEqual(80, health80, "Máu của Enemy phải giảm xuống 80 sau khi nhận 20 sát thương.");

            // Gọi thêm TakeDamage(79) - Máu còn 1 (Tránh về 0 để không bị Destroy trong Edit Mode)
            takeDamageMethod.Invoke(healthComp, new object[] { 79 });
            int health1 = (int)currentHealthProp.GetValue(healthComp);
            Assert.AreEqual(1, health1, "Máu của Enemy phải giảm xuống 1 sau khi nhận thêm 79 sát thương.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        [Description("4. Kiểm tra EnemyHitbox: Điểm yếu phải bị phá hủy khi hết máu.")]
        public void EnemyHitbox_WeakPoint_BrokenWhenHealthZero()
        {
            Type enemyHitboxType = GetGameType("EnemyHitbox");
            Type enemyHealthType = GetGameType("EnemyHealth");
            Type hitboxTypeEnum = enemyHitboxType.GetNestedType("HitboxType");

            // 1. Setup Enemy và Health
            GameObject enemyGo = new GameObject("TestEnemy");
            Component healthComp = enemyGo.AddComponent(enemyHealthType);
            
            // 2. Setup Hitbox điểm yếu
            GameObject hitboxGo = new GameObject("WeakPointHitbox");
            hitboxGo.transform.SetParent(enemyGo.transform);
            Component hitboxComp = hitboxGo.AddComponent(enemyHitboxType);

            // 3. Thiết lập các thông số qua Reflection
            // Type = WeakPoint (Giá trị enum là 2)
            FieldInfo typeField = enemyHitboxType.GetField("Type");
            typeField.SetValue(hitboxComp, Enum.ToObject(hitboxTypeEnum, 2));

            // MainHealth = healthComp
            FieldInfo mainHealthField = enemyHitboxType.GetField("MainHealth");
            mainHealthField.SetValue(hitboxComp, healthComp);

            // Gán WeakPoint GameObject (Mô phỏng phần nhìn thấy của điểm yếu)
            GameObject weakPointModel = new GameObject("WeakPointModel");
            weakPointModel.transform.SetParent(hitboxGo.transform);
            FieldInfo weakPointField = enemyHitboxType.GetField("WeakPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            weakPointField.SetValue(hitboxComp, weakPointModel);

            // Thiết lập máu điểm yếu
            FieldInfo maxHealthField = enemyHitboxType.GetField("WeakPointMaxHealth");
            maxHealthField.SetValue(hitboxComp, 50);
            FieldInfo currentHealthField = enemyHitboxType.GetField("_currentWeakPointHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            currentHealthField.SetValue(hitboxComp, 50);

            // 4. Thực hiện (Act): Gây sát thương phá hủy điểm yếu
            MethodInfo takeDamageMethod = enemyHitboxType.GetMethod("TakeDamage");
            takeDamageMethod.Invoke(hitboxComp, new object[] { 50 });

            // 5. Kiểm tra (Assert)
            PropertyInfo isBrokenProp = enemyHitboxType.GetProperty("IsBroken");
            bool isBroken = (bool)isBrokenProp.GetValue(hitboxComp);
            
            Assert.IsTrue(isBroken, "Biến IsBroken phải là true sau khi điểm yếu hết máu.");
            Assert.IsFalse(weakPointModel.activeSelf, "GameObject WeakPoint phải bị tắt (Deactivate).");
            Assert.IsFalse(hitboxGo.activeSelf, "Chính GameObject chứa Hitbox cũng phải bị tắt.");

            // Dọn dẹp
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }
    }
}
