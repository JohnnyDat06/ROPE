using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayModeTests
{
    public class SimplePlayModeTests
    {
        private GameObject m_Player;
        private GameObject m_MainCam;
        private PlayerInventorySystem m_Inventory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 1. Tạo Camera với tag MainCamera để tránh lỗi Null ở dòng 110 (playerCam.ViewportPointToRay)
            m_MainCam = new GameObject("MainCamera");
            m_MainCam.tag = "MainCamera";
            m_MainCam.AddComponent<Camera>();

            m_Player = new GameObject("Player");
            m_Inventory = m_Player.AddComponent<PlayerInventorySystem>();
            
            // 2. Thiết lập 4 slots trước khi yield return (để hàm Start của Inventory nhận đủ số lượng ô)
            m_Inventory.inventorySlots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                m_Inventory.inventorySlots[i] = new GameObject("Slot_" + i).transform;
                m_Inventory.inventorySlots[i].SetParent(m_Player.transform);
            }

            yield return null; // Chờ 1 frame để hàm Start() của Inventory chạy
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Player != null) Object.Destroy(m_Player);
            if (m_MainCam != null) Object.Destroy(m_MainCam);
            yield return null;
        }

        // --- TEST 1: KIỂM TRA KHỞI TẠO Ô ĐỒ ---
        [UnityTest]
        public IEnumerator Test1_Inventory_SlotsInitialized()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var items = (ItemController[])field.GetValue(m_Inventory);
            
            Assert.IsNotNull(items, "Mảng inventoryItems phải được khởi tạo.");
            Assert.AreEqual(4, items.Length, "Số lượng ô đồ phải khớp với số lượng inventorySlots.");
            yield return null;
        }

        // --- TEST 2: TÌM Ô ĐỒ TRỐNG ---
        [UnityTest]
        public IEnumerator Test2_Inventory_GetEmptySlot_ReturnsFirstAvailable()
        {
            MethodInfo getEmptySlotMethod = typeof(PlayerInventorySystem).GetMethod("GetEmptySlot", BindingFlags.NonPublic | BindingFlags.Instance);
            int index = (int)getEmptySlotMethod.Invoke(m_Inventory, null);
            
            Assert.AreEqual(0, index, "Ô trống đầu tiên phải là index 0.");

            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var items = (ItemController[])field.GetValue(m_Inventory);
            
            GameObject fakeGO = new GameObject("FakeItem");
            fakeGO.AddComponent<BoxCollider>(); // Đảm bảo có Collider
            items[0] = fakeGO.AddComponent<ItemController>();
            
            index = (int)getEmptySlotMethod.Invoke(m_Inventory, null);
            Assert.AreEqual(1, index, "Sau khi ô 0 đầy, ô trống tiếp theo phải là 1.");

            Object.Destroy(fakeGO);
            yield return null;
        }

        // --- TEST 3: TÍNH TOÁN GIÁ TRỊ VÀ TRỌNG LƯỢNG ---
        [UnityTest]
        public IEnumerator Test3_Inventory_UpdateStats_CalculatesCorrectly()
        {
            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var items = (ItemController[])field.GetValue(m_Inventory);

            GameObject itemGO1 = new GameObject("Item1");
            itemGO1.AddComponent<BoxCollider>();
            ItemController item1 = itemGO1.AddComponent<ItemController>();
            item1.data = ScriptableObject.CreateInstance<ItemData>();
            item1.data.weight = 10f;
            item1.scrapValue = 100;

            items[0] = item1;

            MethodInfo updateStatsMethod = typeof(PlayerInventorySystem).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
            updateStatsMethod.Invoke(m_Inventory, null);

            Assert.AreEqual(10f, m_Inventory.TotalWeight, "Tổng trọng lượng phải là 10.");
            Assert.AreEqual(100, m_Inventory.TotalValue, "Tổng giá trị phải là 100.");

            Object.Destroy(itemGO1);
            yield return null;
        }

        // --- TEST 4: NHẶT VẬT PHẨM VÀO TÚI ---
        [UnityTest]
        public IEnumerator Test4_Inventory_PickupItem_SetsParentAndState()
        {
            GameObject itemGO = new GameObject("PickupItem");
            itemGO.AddComponent<BoxCollider>(); // KHẮC PHỤC LỖI MISSING COMPONENTx
            ItemController item = itemGO.AddComponent<ItemController>();
            item.data = ScriptableObject.CreateInstance<ItemData>();

            MethodInfo pickupMethod = typeof(PlayerInventorySystem).GetMethod("PickupItem", BindingFlags.NonPublic | BindingFlags.Instance);
            pickupMethod.Invoke(m_Inventory, new object[] { item, 0 });

            Assert.AreEqual(m_Inventory.inventorySlots[0], item.transform.parent, "Item phải là con của Slot 0.");
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            Assert.IsTrue(rb.isKinematic, "Item trong túi phải có IsKinematic = true.");

            yield return null;
        }

        // --- TEST 5: KIỂM TRA KEYCARD ĐỂ MỞ CỬA ---
        [UnityTest]
        public IEnumerator Test5_Inventory_CheckHasKeyCard_Works()
        {
            m_Inventory.keyCardName = "GoldenKey";
            
            MethodInfo checkMethod = typeof(PlayerInventorySystem).GetMethod("CheckHasKeyCard", BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasKey = (bool)checkMethod.Invoke(m_Inventory, null);
            Assert.IsFalse(hasKey, "Lúc đầu không được có KeyCard.");

            GameObject keyGO = new GameObject("KeyCard");
            keyGO.AddComponent<BoxCollider>();
            ItemController keyItem = keyGO.AddComponent<ItemController>();
            keyItem.data = ScriptableObject.CreateInstance<ItemData>();
            keyItem.data.itemName = "GoldenKey";

            var field = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var items = (ItemController[])field.GetValue(m_Inventory);
            items[0] = keyItem;

            hasKey = (bool)checkMethod.Invoke(m_Inventory, null);
            Assert.IsTrue(hasKey, "Phải tìm thấy KeyCard khi nó nằm trong túi.");

            Object.Destroy(keyGO);
            yield return null;
        }
    }
}