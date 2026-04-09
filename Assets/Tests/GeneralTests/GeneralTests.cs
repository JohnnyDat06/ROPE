using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using DatScript; // Namespace for Checkpoint

namespace Tests.GeneralTests
{
    public class GeneralTests
    {
        // --- TEST CASE 1: ITEM DATA VALIDATION ---
        [Test]
        public void Test1_ItemData_DefaultDuration_IsValid()
        {
            // ARRANGE & ACT: Create an instance of ItemData ScriptableObject
            ItemData testData = ScriptableObject.CreateInstance<ItemData>();
            
            // ASSERT: Verify default pickup duration is at least 0
            // According to ItemData.cs, default is 1.0f
            Assert.GreaterOrEqual(testData.pickupDuration, 0f, "Pickup duration cannot be negative.");
            
            // Cleanup
            Object.DestroyImmediate(testData);
        }

        // --- TEST CASE 2: CHECKPOINT INITIAL STATE ---
        [Test]
        public void Test2_Checkpoint_InitialState_IsDeactivated()
        {
            // ARRANGE: Create a GameObject and add Checkpoint component
            GameObject go = new GameObject("TestCheckpoint");
            Checkpoint cp = go.AddComponent<Checkpoint>();

            // ACT: Use Reflection to get the private field 'isActivated'
            // In Checkpoint.cs, isActivated is a private bool
            FieldInfo field = typeof(Checkpoint).GetField("isActivated", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            bool initialState = (bool)field.GetValue(cp);

            // ASSERT: Verify the checkpoint starts as NOT activated
            Assert.IsFalse(initialState, "Checkpoint should not be activated by default.");

            // Cleanup
            Object.DestroyImmediate(go);
        }

        // --- TEST CASE 3: ITEM TYPE ENUM VALIDATION ---
        [Test]
        public void Test3_ItemType_Enum_HasRequiredValues()
        {
            // ACT & ASSERT: Verify specific enum values exist for the game's core items
            // This ensures common item types aren't accidentally removed from GameEnums.cs
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Small"), "Small type must exist.");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Large"), "Large type must exist.");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Special"), "Special type must exist.");
        }
    }
}