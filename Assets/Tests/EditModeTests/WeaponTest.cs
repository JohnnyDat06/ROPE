using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ROPE.Tests
{
    public class WeaponTest
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

        // Thiết lập nhanh ScriptableObject cấu hình đạn và component Súng
        private (RaycastWeapon, AmmoConfigSO) SetupWeaponAndAmmo()
        {
            var go = CreateTestObject("AssaultRifle");
            go.SetActive(false); // Ngắt Awake/Start để không thiếu các tham chiếu raycastOrigin, flashEffects...

            var weapon = go.AddComponent<RaycastWeapon>();
            var ammoConfig = ScriptableObject.CreateInstance<AmmoConfigSO>();
            
            // Gọi phương thức OnEnable của ScriptableObject để khởi tạo giá trị default (maxAmmo, clipSize)
            var onEnableMethod = typeof(AmmoConfigSO).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onEnableMethod != null) onEnableMethod.Invoke(ammoConfig, null);
            
            weapon.ammoConfig = ammoConfig;
            return (weapon, ammoConfig);
        }

        // ── Tests ─────────────────────────────────────────────────────

        // Kiểm tra RaycastWeapon: Xác nhận cờ CanReload trả về true khi băng đạn chưa đầy và có đạn dự trữ, 
        // ngược lại trả về false khi hết sạch đạn dự trữ.
        [Test]
        public void TestWeaponCanReloadCondition()
        {
            var (weapon, ammo) = SetupWeaponAndAmmo();
            
            // Xả bớt đạn trong băng sao cho thiếu đạn nhưng kho vẫn còn
            ammo.currentClipAmmo = 10; 
            ammo.clipSize = 30;
            ammo.currentAmmo = 90;

            Assert.IsTrue(weapon.CanReload(), "Súng phải có thể reload khi băng đạn vơi và đạn túi còn");

            // Kịch bản đã hết sạch đạn dự trữ
            ammo.currentAmmo = 0;
            Assert.IsFalse(weapon.CanReload(), "Súng không được phép reload khi hết nhẵn đạn dự trữ");
        }

        // Kiểm tra RaycastWeapon: Hàm StartReload thay đổi trạng thái isReloading bật lên 
        // và tự động ngắt cờ đang bắn isFiring xuống.
        [Test]
        public void TestStartReloadUpdatesWeaponState()
        {
            var (weapon, _) = SetupWeaponAndAmmo();
            
            weapon.isFiring = true;
            weapon.isReloading = false;

            weapon.StartReload(); // Bắt đầu quá trình nạp đạn

            Assert.IsTrue(weapon.isReloading, "Trạng thái isReloading phải trở thành true khi bắt đầu nạp đạn");
            Assert.IsFalse(weapon.isFiring, "Súng phải ngừng bắn (isFiring=false) trong lúc nạp đạn");
        }

        // Kiểm tra RaycastWeapon: Hàm RefillAmmo nạp đầy băng đạn từ đạn dự phòng
        // đồng thời tính toán trừ đúng số lượng đạn túi và gỡ bỏ trạng thái isReloading.
        [Test]
        public void TestRefillAmmoUpdatesAmmoMathAndRestoresState()
        {
            var (weapon, ammo) = SetupWeaponAndAmmo();

            ammo.currentClipAmmo = 5;
            ammo.clipSize = 30;
            ammo.currentAmmo = 50;
            weapon.isReloading = true; // Giả sử Animator súng đang chạy Reload trigger

            // Gắn băng đạn và hoàn tất nạp đạn
            weapon.RefillAmmo();

            Assert.IsFalse(weapon.isReloading, "Trạng thái isReloading phải được tắt sau khi nạp đạn xong");
            Assert.AreEqual(30, ammo.currentClipAmmo, "Băng đạn súng phải đầy sau khi nạp lên (5 -> 30)");
            Assert.AreEqual(25, ammo.currentAmmo, "Đạn dự phòng phải bị trừ rỗng đúng 25 viên (50 - 25 = 25)");
        }

        // Kiểm tra RaycastWeapon: Vũ khí chối từ việc bắn khi băng đạn đang rỗng hoặc súng đang nạp đạn.
        [Test]
        public void TestStartFiringShouldBeBlockedWhenEmptyOrReloading()
        {
            var (weapon, ammo) = SetupWeaponAndAmmo();

            // Scenario 1: Đang nạp đạn nhưng người chơi nhấn nút bắn
            weapon.isReloading = true;
            ammo.currentClipAmmo = 30; // dù có đạn
            
            weapon.StartFiring();
            Assert.IsFalse(weapon.isFiring, "Hành động bắn phải bị từ chối nếu súng đang bận nạp đạn");

            // Scenario 2: Hết nhẵn đạn trong băng
            weapon.isReloading = false;
            ammo.currentClipAmmo = 0;

            weapon.StartFiring();
            Assert.IsFalse(weapon.isFiring, "Hành động bắn phải bị từ chối khi không có đạn trong băng");
        }
    }
}
