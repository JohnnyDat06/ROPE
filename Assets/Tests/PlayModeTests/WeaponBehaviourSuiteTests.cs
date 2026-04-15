using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayModeTests
{
    public class WeaponBehaviourSuiteTests
    {
        readonly List<Object> m_TestObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_TestObjects)
            {
                if (obj is RaycastWeapon weapon)
                {
                    foreach (object bulletObject in GetBullets(weapon))
                    {
                        var tracerField = bulletObject.GetType().GetField("tracer", BindingFlags.Instance | BindingFlags.Public);
                        var tracer = tracerField?.GetValue(bulletObject) as TrailRenderer;
                        if (tracer != null)
                        {
                            Object.DestroyImmediate(tracer.gameObject);
                        }
                    }
                }
            }

            foreach (Object obj in m_TestObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            m_TestObjects.Clear();
        }

        GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        T CreateTestAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            m_TestObjects.Add(asset);
            return asset;
        }

        static void InvokeNonPublic(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Cannot find method {methodName} on {instance.GetType().Name}");
            method.Invoke(instance, null);
        }

        static void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Cannot find field {fieldName} on {instance.GetType().Name}");
            field.SetValue(instance, value);
        }

        static IList GetBullets(RaycastWeapon weapon)
        {
            FieldInfo bulletsField = typeof(RaycastWeapon).GetField("bullets", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(bulletsField);
            return (IList)bulletsField.GetValue(weapon);
        }

        AmmoConfigSO CreateAmmoConfig(int clipAmmo = 30, int clipSize = 30, int reserveAmmo = 90)
        {
            var ammo = CreateTestAsset<AmmoConfigSO>();
            InvokeNonPublic(ammo, "OnEnable");
            ammo.clipSize = clipSize;
            ammo.currentClipAmmo = clipAmmo;
            ammo.currentAmmo = reserveAmmo;
            return ammo;
        }

        RaycastWeapon CreateConfiguredWeapon(
            string name = "Pistol",
            int clipAmmo = 30,
            int clipSize = 30,
            int reserveAmmo = 90,
            bool keepInactive = true)
        {
            var weaponObject = CreateTestObject(name);
            weaponObject.SetActive(!keepInactive ? true : false);

            var weapon = weaponObject.AddComponent<RaycastWeapon>();
            weapon.weaponName = name;
            weapon.ammoConfig = CreateAmmoConfig(clipAmmo, clipSize, reserveAmmo);
            weapon.fireRate = 10;

            var origin = CreateTestObject($"{name}_Origin").transform;
            var destination = CreateTestObject($"{name}_Destination").transform;
            origin.position = Vector3.zero;
            destination.position = Vector3.forward * 10f;

            var tracerPrefabObject = CreateTestObject($"{name}_TracerPrefab");
            tracerPrefabObject.SetActive(false);
            var tracer = tracerPrefabObject.AddComponent<TrailRenderer>();

            SetPrivateField(weapon, "muzzleFlash", new ParticleSystem[0]);
            SetPrivateField(weapon, "raycastOrigin", origin);
            SetPrivateField(weapon, "tracerEffect", tracer);
            weapon.raycastDestination = destination;

            return weapon;
        }

        [UnityTest]
        public IEnumerator ClickShoot_ShouldSpawnBullet()
        {
            var weapon = CreateConfiguredWeapon();

            weapon.StartFiring();
            weapon.UpdateFiring(0f);
            yield return null;

            Assert.IsTrue(weapon.isFiring, "Weapon should enter firing state when click-shoot starts.");
            Assert.AreEqual(1, GetBullets(weapon).Count, "A bullet/tracer instance should be spawned after firing.");
            Assert.AreEqual(29, weapon.ammoConfig.currentClipAmmo, "Firing once should consume exactly one bullet.");
        }

        [UnityTest]
        public IEnumerator Reload_ShouldRefillAmmo()
        {
            var weapon = CreateConfiguredWeapon(clipAmmo: 5, clipSize: 30, reserveAmmo: 50);

            weapon.StartReload();
            weapon.RefillAmmo();
            yield return null;

            Assert.IsFalse(weapon.isReloading, "Reload state should end after ammo refill completes.");
            Assert.AreEqual(30, weapon.ammoConfig.currentClipAmmo, "Clip ammo should be refilled to full.");
            Assert.AreEqual(25, weapon.ammoConfig.currentAmmo, "Reserve ammo should be reduced by the bullets inserted into the clip.");
        }

        [UnityTest]
        public IEnumerator StartReload_ShouldStopFiringAndEnterReloadState()
        {
            var weapon = CreateConfiguredWeapon(clipAmmo: 30, clipSize: 30, reserveAmmo: 90);

            weapon.StartFiring();
            weapon.UpdateFiring(0f);
            weapon.StartReload();
            yield return null;

            Assert.IsTrue(weapon.isReloading, "Weapon should enter reload state after StartReload is called.");
            Assert.IsFalse(weapon.isFiring, "Weapon should stop firing immediately when reload starts.");
        }

        [UnityTest]
        public IEnumerator AmmoZero_ShouldNotShoot()
        {
            var weapon = CreateConfiguredWeapon(clipAmmo: 0, clipSize: 30, reserveAmmo: 90);

            weapon.StartFiring();
            yield return null;

            Assert.IsFalse(weapon.isFiring, "Weapon must not start firing when clip ammo is empty.");
            Assert.AreEqual(0, GetBullets(weapon).Count, "No bullet should be spawned when there is no ammo.");
        }

        [UnityTest]
        public IEnumerator Reloading_ShouldNotShoot()
        {
            var weapon = CreateConfiguredWeapon(clipAmmo: 30, clipSize: 30, reserveAmmo: 90);
            weapon.isReloading = true;

            weapon.StartFiring();
            yield return null;

            Assert.IsFalse(weapon.isFiring, "Weapon must not start firing while reloading.");
            Assert.AreEqual(0, GetBullets(weapon).Count, "No bullet should be spawned while reload is in progress.");
        }
    }
}
