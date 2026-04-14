using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayModeTests
{
    public class InventorySystemSimpleTests
    {
        private GameObject m_Player;
        private GameObject m_MainCam;
        private PlayerInventorySystem m_Inventory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 1. Setup Camera (Required for PlayerInventorySystem to not fail in Start/Update)
            m_MainCam = new GameObject("MainCamera");
            m_MainCam.tag = "MainCamera";
            m_MainCam.AddComponent<Camera>();

            // 2. Setup Player and Inventory
            m_Player = new GameObject("Player");
            m_Inventory = m_Player.AddComponent<PlayerInventorySystem>();

            // 3. Setup 4 slots
            m_Inventory.inventorySlots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                m_Inventory.inventorySlots[i] = new GameObject("Slot_" + i).transform;
                m_Inventory.inventorySlots[i].SetParent(m_Player.transform);
            }

            yield return null; // Wait 1 frame for Start() to initialize internal arrays
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Player != null) Object.Destroy(m_Player);
            if (m_MainCam != null) Object.Destroy(m_MainCam);
            yield return null;
        }

        // --- TEST 1: KIỂM TRA CHỈ SỐ BAN ĐẦU ---
        [UnityTest]
        public IEnumerator Test1_InitialStats_AreZero()
        {
            Assert.AreEqual(0f, m_Inventory.TotalWeight, "Trọng lượng ban đầu phải là 0");
            Assert.AreEqual(0, m_Inventory.TotalValue, "Giá trị ban đầu phải là 0");
            Assert.AreEqual(0, m_Inventory.TotalItemCount, "Số lượng vật phẩm ban đầu phải là 0");
            yield return null;
        }

        // --- TEST 2: KIỂM TRA KHI TÚI ĐỒ ĐẦY ---
        [UnityTest]
        public IEnumerator Test2_GetEmptySlot_ReturnsMinusOne_WhenFull()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            ItemController[] items = (ItemController[])field.GetValue(m_Inventory);

            // Làm đầy tất cả các ô
            for (int i = 0; i < items.Length; i++)
            {
                GameObject go = new GameObject("Item_" + i);
                items[i] = go.AddComponent<ItemController>();
            }

            var method = typeof(PlayerInventorySystem).GetMethod("GetEmptySlot", BindingFlags.NonPublic | BindingFlags.Instance);
            int result = (int)method.Invoke(m_Inventory, null);

            Assert.AreEqual(-1, result, "Khi túi đầy, GetEmptySlot phải trả về -1");

            // Cleanup
            foreach (var item in items) Object.Destroy(item.gameObject);
            yield return null;
        }

        // --- TEST 3: KIỂM TRA ĐẾM SỐ LƯỢNG VẬT PHẨM ---
        [UnityTest]
        public IEnumerator Test3_UpdateStats_ItemCount_IsCorrect()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            ItemController[] items = (ItemController[])field.GetValue(m_Inventory);

            // Thêm 3 vật phẩm vào túi
            for (int i = 0; i < 3; i++)
            {
                GameObject go = new GameObject("Item_" + i);
                ItemController ic = go.AddComponent<ItemController>();
                ic.data = ScriptableObject.CreateInstance<ItemData>(); // Fix NullReference
                items[i] = ic;
            }

            var method = typeof(PlayerInventorySystem).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(m_Inventory, null);

            Assert.AreEqual(3, m_Inventory.TotalItemCount, "Tổng số lượng vật phẩm phải là 3");

            // Cleanup
            for (int i = 0; i < 3; i++) 
            {
                Object.Destroy(items[i].data);
                Object.Destroy(items[i].gameObject);
            }
            yield return null;
        }

        // --- TEST 4: KIỂM TRA TÍNH TOÁN CỘNG DỒN TRỌNG LƯỢNG VÀ GIÁ TRỊ ---
        [UnityTest]
        public IEnumerator Test4_UpdateStats_HandlesMultipleItems_WeightAndValue()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            ItemController[] items = (ItemController[])field.GetValue(m_Inventory);

            // Item 1: Weight 5.5, Value 100
            GameObject go1 = new GameObject("Item1");
            ItemController ic1 = go1.AddComponent<ItemController>();
            ic1.data = ScriptableObject.CreateInstance<ItemData>();
            ic1.data.weight = 5.5f;
            ic1.scrapValue = 100;
            items[0] = ic1;

            // Item 2: Weight 2.0, Value 50
            GameObject go2 = new GameObject("Item2");
            ItemController ic2 = go2.AddComponent<ItemController>();
            ic2.data = ScriptableObject.CreateInstance<ItemData>();
            ic2.data.weight = 2.0f;
            ic2.scrapValue = 50;
            items[1] = ic2;

            var method = typeof(PlayerInventorySystem).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(m_Inventory, null);

            Assert.AreEqual(7.5f, m_Inventory.TotalWeight, "Tổng trọng lượng phải là 5.5 + 2.0 = 7.5");
            Assert.AreEqual(150, m_Inventory.TotalValue, "Tổng giá trị phải là 100 + 50 = 150");

            Object.Destroy(ic1.data); Object.Destroy(go1);
            Object.Destroy(ic2.data); Object.Destroy(go2);
            yield return null;
        }

        // --- TEST 5: KIỂM TRA KHÔNG CÓ THẺ KEYCARD ---
        [UnityTest]
        public IEnumerator Test5_CheckHasKeyCard_ReturnsFalse_WhenWrongItemPresent()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            ItemController[] items = (ItemController[])field.GetValue(m_Inventory);

            // Thêm một vật phẩm không phải KeyCard
            GameObject go = new GameObject("NotAKeyCard");
            ItemController ic = go.AddComponent<ItemController>();
            ic.data = ScriptableObject.CreateInstance<ItemData>();
            ic.data.itemName = "ScrapMetal";
            items[0] = ic;

            var method = typeof(PlayerInventorySystem).GetMethod("CheckHasKeyCard", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)method.Invoke(m_Inventory, null);

            Assert.IsFalse(result, "CheckHasKeyCard phải trả về false khi không có vật phẩm tên 'KeyCard'");

            Object.Destroy(ic.data);
            Object.Destroy(go);
            yield return null;
        }
    }
}
