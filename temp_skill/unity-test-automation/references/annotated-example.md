# Reference: FPSMicrogameTests.cs — Annotated

This file annotates every structural decision in the reference test file so the skill can replicate
the pattern precisely for any Unity project.

---

## File Header

```csharp
using System.Reflection;   // required for private member access
using NUnit.Framework;     // NUnit — always needed
using UnityEngine;         // for GameObject, Object.DestroyImmediate, etc.
using Unity.FPS.Game;      // from asmdef "references": ["fps.Game"]
using Unity.FPS.Gameplay;  // from asmdef "references": ["fps.Gameplay"]
```

**Namespace derivation from asmdef:**
| asmdef "references" entry | using directive          |
|---------------------------|--------------------------|
| `"fps.Game"`              | `using Unity.FPS.Game;`  |
| `"fps.Gameplay"`          | `using Unity.FPS.Gameplay;` |

Pattern: replace `.` with `.` and prepend `Unity.` (project-specific — confirm from source files).

---

## Test Class Skeleton

```csharp
namespace EditModeTests       // always matches asmdef "name"
{
    public class FPSMicrogameTests
    {
        // Tracks every GameObject created during a test run
        readonly System.Collections.Generic.List<Object> m_TestObjects =
            new System.Collections.Generic.List<Object>();
```

---

## SetUp / TearDown

```csharp
[SetUp]
public void SetUp()
{
    EventManager.Clear();   // reset global event bus before each test
}

[TearDown]
public void TearDown()
{
    foreach (Object obj in m_TestObjects)
    {
        if (obj != null)
            Object.DestroyImmediate(obj);  // synchronous destroy in Edit Mode
    }
    m_TestObjects.Clear();
    EventManager.Clear();
}
```

**Why `DestroyImmediate`?** In Edit Mode there is no frame loop — `Destroy` is deferred and never runs.
`DestroyImmediate` is safe and required here.

---

## CreateTestObject Helper

```csharp
GameObject CreateTestObject(string name)
{
    var obj = new GameObject(name);
    m_TestObjects.Add(obj);   // auto-cleanup in TearDown
    return obj;
}
```

**Rule:** Never call `new GameObject()` directly in a test — always go through this helper.

---

## Pattern A: Private Field + Private Method via Reflection (TestScore)

```csharp
[Test]
public void TestScore()
{
    // 1. Create and configure component
    var go = CreateTestObject("Objective");
    var objective = go.AddComponent<ObjectiveKillEnemies>();
    objective.MustKillAllEnemies = false;
    objective.KillsToCompleteObjective = 3;

    // 2. Assert initial state via public property
    Assert.IsFalse((bool)objective.IsCompleted);

    // 3. Retrieve private METHOD via reflection
    var onEnemyKilledMethod = typeof(ObjectiveKillEnemies).GetMethod("OnEnemyKilled",
        BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(onEnemyKilledMethod, "OnEnemyKilled method should exist on ObjectiveKillEnemies");

    // 4. Retrieve private FIELD via reflection
    var killTotalField = typeof(ObjectiveKillEnemies).GetField("m_KillTotal",
        BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(killTotalField, "m_KillTotal field should exist on ObjectiveKillEnemies");

    // 5. Assert initial field value
    Assert.AreEqual(0, (int)killTotalField.GetValue(objective));

    // 6. Simulate events incrementally
    var evt = Events.EnemyKillEvent;   // reuse the shared struct
    evt.Enemy = null;
    evt.RemainingEnemyCount = 2;
    onEnemyKilledMethod.Invoke(objective, new object[] { evt });
    Assert.AreEqual(1, (int)killTotalField.GetValue(objective));
    Assert.IsFalse((bool)objective.IsCompleted);

    // … repeat for 2nd and 3rd kill …

    // 7. Final assertion: objective should now be complete
    Assert.IsTrue((bool)objective.IsCompleted);
}
```

---

## Pattern B: Component with Event Delegate (TestPlayerHealth)

```csharp
[Test]
public void TestPlayerHealth()
{
    var go = CreateTestObject("Player");
    var health = go.AddComponent<Health>();
    health.MaxHealth = 100f;
    health.CurrentHealth = 100f;

    // Assert helper methods
    Assert.AreEqual(1f, health.GetRatio());
    Assert.IsFalse((bool)health.IsCritical());
    Assert.IsFalse((bool)health.CanPickup());

    // Subscribe to delegate BEFORE triggering
    bool died = false;
    health.OnDie += () => died = true;

    // Partial damage — should NOT die
    health.TakeDamage(30f, null);
    Assert.AreEqual(70f, health.CurrentHealth);
    Assert.IsFalse(died);

    // Lethal damage — should die exactly once
    health.TakeDamage(80f, null);
    Assert.AreEqual(0f, health.CurrentHealth);
    Assert.IsTrue(died);

    // Post-death damage — event must NOT fire again
    died = false;
    health.TakeDamage(10f, null);
    Assert.AreEqual(0f, health.CurrentHealth);
    Assert.IsFalse(died);
}
```

**Key patterns:**
- Set flag to `false` again after death to verify "fire exactly once" semantics.
- HP never goes below 0 — assert `AreEqual(0f, ...)` not negative.

---

## Pattern C: Array Slot Manipulation with Reflection (TestWeaponInventory)

```csharp
[Test]
public void TestWeaponInventory()
{
    // Disable the GameObject before adding manager to suppress Awake()
    var playerGO = CreateTestObject("Player");
    playerGO.SetActive(false);
    playerGO.AddComponent<PlayerWeaponsManager>();
    var weaponsManager = playerGO.GetComponent<PlayerWeaponsManager>();

    // Get private array via reflection
    var slotsField = typeof(PlayerWeaponsManager).GetField("m_WeaponSlots",
        BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(slotsField, "m_WeaponSlots field should exist on PlayerWeaponsManager");
    var slots = (WeaponController[])slotsField.GetValue(weaponsManager);

    // Empty slot check
    Assert.IsNull(weaponsManager.GetWeaponAtSlotIndex(0));

    // Insert weapon into slot directly
    var blasterGO = CreateTestObject("Blaster");
    blasterGO.SetActive(false);
    var blaster = blasterGO.AddComponent<WeaponController>();
    blaster.WeaponName = "Blaster";
    blaster.WeaponMuzzle = blasterGO.transform;
    blaster.WeaponRoot = blasterGO;
    blaster.SourcePrefab = blasterGO;

    slots[0] = blaster;
    Assert.IsNotNull(weaponsManager.GetWeaponAtSlotIndex(0));
    Assert.AreEqual("Blaster", weaponsManager.GetWeaponAtSlotIndex(0).WeaponName);
    Assert.IsNotNull(weaponsManager.HasWeapon(blaster));

    // Remove slot — verify nulls
    slots[0] = null;
    Assert.IsNull(weaponsManager.GetWeaponAtSlotIndex(0));
    Assert.IsNull(weaponsManager.HasWeapon(blaster));
}
```

**Key patterns:**
- `SetActive(false)` on both the manager and weapon GOs prevents `Awake`/`Start` side effects.
- Assign minimum required fields (`WeaponMuzzle`, `WeaponRoot`, `SourcePrefab`) to avoid NPEs.
- Mutate the reflected array directly: `slots[i] = weapon`.

---

## Assert Cheat-Sheet

| Scenario | Assert |
|---|---|
| Value equals expected | `Assert.AreEqual(expected, actual)` |
| Object is not null | `Assert.IsNotNull(obj)` |
| Object is null | `Assert.IsNull(obj)` |
| Boolean is true | `Assert.IsTrue(condition)` |
| Boolean is false | `Assert.IsFalse(condition)` |
| Reflected member exists | `Assert.IsNotNull(member, "descriptive message")` |
| Float comparison | `Assert.AreEqual(1f, ratio)` (exact for simple ratios) |
