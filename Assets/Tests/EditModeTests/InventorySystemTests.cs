using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ROPE.Tests
{
    public class InventorySystemTests
    {
        private List<GameObject> m_CreatedObjects = new List<GameObject>();
        private const string assetPath = "Assets/_Game/Scripts/_Core/InvetoryCore/";

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in m_CreatedObjects)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            m_CreatedObjects.Clear();
        }

        private GameObject CreateGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            m_CreatedObjects.Add(go);
            return go;
        }

        // --- TEST 1: ITEM DATA VALIDATION (Dữ liệu thật) ---
        [Test]
        public void Test1_ItemData_Properties_AreValid()
        {
            // Sử dụng dữ liệu thật từ Asset thay vì CreateInstance giả
            ItemData testData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath + "Data_Bolt.asset");
            
            Assert.IsNotNull(testData, "Không tìm thấy file Data_Bolt.asset");
            Assert.AreEqual("Old Engine", testData.itemName);
            Assert.AreEqual(ItemType.IronLarge, testData.itemType);
        }

        // --- TEST 2: SLOT INITIALIZATION ---
        [Test]
        public void Test2_Inventory_SlotInitialization_MatchesSettings()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();

            Transform[] slots = new Transform[2];
            slots[0] = CreateGameObject("Slot_0").transform;
            slots[1] = CreateGameObject("Slot_1").transform;
            inventorySystem.inventorySlots = slots;

            // Gọi Start qua Reflection để khởi tạo mảng inventoryItems thật
            inventorySystem.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(inventorySystem, null);

            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var currentItems = (ItemController[])inventoryItemsField.GetValue(inventorySystem);
            
            Assert.AreEqual(2, currentItems.Length);
        }

        // --- TEST 3: KEYCARD DETECTION (Sử dụng God.asset thật) ---
        [Test]
        public void Test3_Inventory_CheckKeyCard_DetectionWorks()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();
            inventorySystem.inventorySlots = new[] { CreateGameObject("Slot0").transform };
            inventorySystem.keyCardName = "KeyCard";

            // Load KeyCard thật (God.asset)
            ItemData keyCardData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath + "God.asset");

            GameObject itemGO = CreateGameObject("KeyCardItem");
            ItemController itemController = itemGO.AddComponent<ItemController>();
            itemController.data = keyCardData;

            // Inject vào hệ thống
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, new[] { itemController });

            MethodInfo checkMethod = typeof(PlayerInventorySystem).GetMethod("CheckHasKeyCard", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)checkMethod.Invoke(inventorySystem, null);

            Assert.IsTrue(result, "Hệ thống không nhận diện được KeyCard từ God.asset");
        }

        // --- TEST 4: INVENTORY STATS CALCULATION (Dữ liệu thật) ---
        [Test]
        public void Test4_Inventory_UpdateStats_CalculatesCorrectly()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();

            ItemData realData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath + "Data_Bolt.asset");
            
            ItemController item = CreateGameObject("Item").AddComponent<ItemController>();
            item.data = realData;
            item.scrapValue = 50; // Giá trị trong dải của IronLarge (50-71)
            
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, new[] { item });

            MethodInfo updateStatsMethod = typeof(PlayerInventorySystem).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
            updateStatsMethod.Invoke(inventorySystem, null);

            Assert.AreEqual(realData.weight, inventorySystem.TotalWeight);
            Assert.AreEqual(50, inventorySystem.TotalValue);
        }

        // --- TEST 5: PICKUP LOGIC (Sử dụng hàm thật) ---
        [Test]
        public void Test5_Inventory_Pickup_Functionality()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();
            inventorySystem.inventorySlots = new[] { CreateGameObject("Slot0").transform };

            ItemData realData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath + "Data_Bolt.asset");
            GameObject itemGO = CreateGameObject("RealItem");
            itemGO.AddComponent<Rigidbody>();
            itemGO.AddComponent<BoxCollider>();
            
            ItemController itemController = itemGO.AddComponent<ItemController>();
            itemController.data = realData;

            // Khởi tạo mảng inventoryItems (vì Start() không tự chạy trong EditMode)
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, new ItemController[1]);

            // Khởi tạo item (gọi Awake thật)
            itemController.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(itemController, null);

            // Thực hiện Pickup thật
            MethodInfo pickupMethod = typeof(PlayerInventorySystem).GetMethod("PickupItem", BindingFlags.NonPublic | BindingFlags.Instance);
            pickupMethod.Invoke(inventorySystem, new object[] { itemController, 0 });

            Assert.AreEqual(inventorySystem.inventorySlots[0], itemGO.transform.parent);
            Assert.IsTrue(itemGO.GetComponent<Rigidbody>().isKinematic);
        }
    }
}
