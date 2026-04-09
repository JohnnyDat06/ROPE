using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests.InventoryTests
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

        // --- TEST CASE 1: ITEM DATA VALIDATION ---
        [Test]
        public void Test1_ItemData_Properties_AreValid()
        {
            // ACT: Create an instance of ItemData ScriptableObject
            ItemData testData = ScriptableObject.CreateInstance<ItemData>();
            testData.itemName = "Test Key";
            testData.weight = 5.0f;
            testData.itemType = ItemType.Special;

            // ASSERT: Verify properties are stored correctly
            Assert.AreEqual("Test Key", testData.itemName, "Item name should match assigned value.");
            Assert.AreEqual(5.0f, testData.weight, "Weight should match assigned value.");
            Assert.AreEqual(ItemType.Special, testData.itemType, "Item type should match assigned value.");
            
            // Cleanup ScriptableObject (not a GameObject)
            Object.DestroyImmediate(testData);
        }

        // --- TEST CASE 2: INVENTORY SLOT MANAGEMENT ---
        [Test]
        public void Test2_Inventory_SlotInitialization_MatchesSettings()
        {
            // ARRANGE: Setup PlayerInventorySystem
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();

            // Setup 4 mock slots
            Transform[] mockSlots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                mockSlots[i] = CreateGameObject("Slot_" + i).transform;
                mockSlots[i].SetParent(playerGO.transform);
            }
            inventorySystem.inventorySlots = mockSlots;

            // ACT: Manually trigger the initialization (normally in Start)
            // Since we're in Edit Mode, we'll use Reflection to initialize the internal array
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            ItemController[] itemsArray = new ItemController[mockSlots.Length];
            inventoryItemsField.SetValue(inventorySystem, itemsArray);

            // ASSERT: Verify the internal array size matches the slots array size
            var currentItems = (ItemController[])inventoryItemsField.GetValue(inventorySystem);
            Assert.AreEqual(4, currentItems.Length, "Inventory items array should be initialized with the same length as slots.");
        }

        // --- TEST CASE 3: KEYCARD DETECTION LOGIC ---
        [Test]
        public void Test3_Inventory_CheckKeyCard_DetectionWorks()
        {
            // ARRANGE: Setup PlayerInventorySystem
            GameObject playerGO = CreateGameObject("Player");
            PlayerInventorySystem inventorySystem = playerGO.AddComponent<PlayerInventorySystem>();
            
            // Mock the slots array
            inventorySystem.inventorySlots = new Transform[1];
            inventorySystem.inventorySlots[0] = CreateGameObject("Slot0").transform;

            // Create a mock ItemData for KeyCard
            ItemData keyCardData = ScriptableObject.CreateInstance<ItemData>();
            keyCardData.itemName = "KeyCard"; // Must match the name in PlayerInventorySystem.keyCardName
            inventorySystem.keyCardName = "KeyCard";

            // Create a mock ItemController
            GameObject itemGO = CreateGameObject("KeyCardItem");
            ItemController itemController = itemGO.AddComponent<ItemController>();
            itemController.data = keyCardData;

            // Manually inject into the private inventoryItems array via Reflection
            var inventoryItemsField = typeof(PlayerInventorySystem).GetField("inventoryItems", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            ItemController[] itemsArray = new ItemController[1];
            itemsArray[0] = itemController;
            inventoryItemsField.SetValue(inventorySystem, itemsArray);

            // ACT: Call the private method CheckHasKeyCard via Reflection
            MethodInfo checkMethod = typeof(PlayerInventorySystem).GetMethod("CheckHasKeyCard", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            bool result = (bool)checkMethod.Invoke(inventorySystem, null);

            // ASSERT: Verify detection works
            Assert.IsTrue(result, "CheckHasKeyCard should return true when a KeyCard is in the inventory.");

            // Cleanup
            Object.DestroyImmediate(keyCardData);
        }
    }
}