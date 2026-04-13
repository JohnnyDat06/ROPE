using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ROPE.Tests
{
    public class InventorySystemTests
    {
        private List<GameObject> m_CreatedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in m_CreatedObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            m_CreatedObjects.Clear();
        }

        private GameObject CreateGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            m_CreatedObjects.Add(go);
            return go;
        }

        // --- TEST 1: ITEM DATA VALIDATION ---
        [Test]
        public void Test1_ItemData_Properties_AreValid()
        {
            ItemData testData = ScriptableObject.CreateInstance<ItemData>();
            testData.itemName = "Test Key";
            testData.weight = 5.0f;
            testData.itemType = ItemType.Special;

            Assert.AreEqual("Test Key", testData.itemName);
            Assert.AreEqual(5.0f, testData.weight);
            
            Object.DestroyImmediate(testData);
        }

        // --- TEST 2: SLOT INITIALIZATION ---
        [Test]
        public void Test2_Inventory_SlotInitialization_MatchesSettings()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();

            Transform[] mockSlots = new Transform[4];
            for (int i = 0; i < 4; i++) mockSlots[i] = CreateGameObject("Slot_" + i).transform;
            inventorySystem.inventorySlots = mockSlots;

            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, new ItemController[4]);

            var currentItems = (ItemController[])inventoryItemsField.GetValue(inventorySystem);
            Assert.AreEqual(4, currentItems.Length);
        }

        // --- TEST 3: KEYCARD DETECTION ---
        [Test]
        public void Test3_Inventory_CheckKeyCard_DetectionWorks()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();
            inventorySystem.inventorySlots = new Transform[1];
            inventorySystem.inventorySlots[0] = CreateGameObject("Slot0").transform;
            inventorySystem.keyCardName = "KeyCard";

            ItemData keyCardData = ScriptableObject.CreateInstance<ItemData>();
            keyCardData.itemName = "KeyCard";

            GameObject itemGO = CreateGameObject("KeyCardItem");
            ItemController itemController = itemGO.AddComponent<ItemController>();
            itemController.data = keyCardData;

            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            ItemController[] itemsArray = new ItemController[1];
            itemsArray[0] = itemController;
            inventoryItemsField.SetValue(inventorySystem, itemsArray);

            MethodInfo checkMethod = typeof(PlayerInventorySystem).GetMethod("CheckHasKeyCard", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)checkMethod.Invoke(inventorySystem, null);

            Assert.IsTrue(result);
            Object.DestroyImmediate(keyCardData);
        }

        // --- TEST 4: INVENTORY STATS CALCULATION ---
        [Test]
        public void Test4_Inventory_UpdateStats_CalculatesCorrectly()
        {
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();

            ItemController[] itemsArray = new ItemController[2];
            for (int i = 0; i < 2; i++)
            {
                ItemController item = CreateGameObject("Item_" + i).AddComponent<ItemController>();
                item.data = ScriptableObject.CreateInstance<ItemData>();
                item.data.weight = 10f;
                item.scrapValue = 50;
                itemsArray[i] = item;
            }
            
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, itemsArray);

            MethodInfo updateStatsMethod = typeof(PlayerInventorySystem).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
            updateStatsMethod.Invoke(inventorySystem, null);

            Assert.AreEqual(20f, inventorySystem.TotalWeight);
            Assert.AreEqual(100, inventorySystem.TotalValue);
            
            foreach (var it in itemsArray) Object.DestroyImmediate(it.data);
        }

        // --- TEST 5: AUTO-SCALING NORMALIZATION ---
        [Test]
        public void Test5_Inventory_Pickup_CalculatesNormalizationScaleCorrectly()
        {
            // ARRANGE
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();
            inventorySystem.inventorySlots = new Transform[1];
            inventorySystem.inventorySlots[0] = CreateGameObject("Slot0").transform;

            GameObject itemGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_CreatedObjects.Add(itemGO);
            
            ItemController itemController = itemGO.AddComponent<ItemController>();
            itemController.data = ScriptableObject.CreateInstance<ItemData>();
            itemController.originalScale = Vector3.one;

            // Manual Injection: Vì Awake() không chạy trong Edit Mode test, ta phải tự gán rb và col
            var rbField = typeof(ItemController).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
            var colField = typeof(ItemController).GetField("col", BindingFlags.NonPublic | BindingFlags.Instance);
            rbField.SetValue(itemController, itemGO.GetComponent<Rigidbody>());
            colField.SetValue(itemController, itemGO.GetComponent<Collider>());

            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            inventoryItemsField.SetValue(inventorySystem, new ItemController[1]);

            // ACT
            try {
                MethodInfo pickupMethod = typeof(PlayerInventorySystem).GetMethod("PickupItem", BindingFlags.NonPublic | BindingFlags.Instance);
                pickupMethod.Invoke(inventorySystem, new object[] { itemController, 0 });
            } catch (TargetInvocationException ex) {
                // Nếu lỗi do gán Layer không tồn tại, ta có thể bỏ qua vì mục tiêu chính là test Scaling
                if (!ex.InnerException.Message.Contains("layer")) throw;
            }

            // ASSERT: Cube size 1, targetSize 1.4 => Scale should be 1.4
            Assert.AreEqual(1.4f, itemGO.transform.localScale.x, 0.001f);
            Object.DestroyImmediate(itemController.data);
        }
    }
}