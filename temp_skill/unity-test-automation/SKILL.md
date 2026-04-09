---
name: unity-test-automation
description: >
  Write Unity Test Framework automation scripts (Edit Mode & Play Mode) for Unity game projects.
  Use this skill whenever the user wants to write unit tests, integration tests, or automation tests
  for Unity game code — including testing MonoBehaviours, game systems, components, managers, events,
  health/weapon/score logic, or any Unity C# scripts. Triggers on: "viết test", "write test", "tạo test",
  "test automation", "NUnit", "Unity Test Framework", "EditMode test", "PlayMode test", "asmdef",
  "kiểm tra component", or whenever the user supplies game scripts and asks to test them.
  Always use this skill when test scripts or .asmdef files are involved.
---

# Unity Test Automation Skill

## Overview

This skill generates Unity Test Framework scripts that follow the exact conventions demonstrated in the
reference code (`FPSMicrogameTests.cs`). Tests are Edit Mode by default unless the user requests Play Mode.

---

## Step 1 — Analyze the Source Scripts

Before writing any test, read every supplied script and extract:

| Item | What to look for |
|---|---|
| **Namespace** | Top-level `namespace X { }` block. If absent → invent one (see §Namespace Rules) |
| **Class name** | The `MonoBehaviour` or plain C# class to test |
| **Public API** | Public fields, properties, methods |
| **Private internals** | Private fields/methods that must be accessed via `System.Reflection` |
| **Events / delegates** | `Action`, `UnityEvent`, custom event buses (e.g. `EventManager`) |
| **Dependencies** | Other components the class `GetComponent`s or subscribes to |

### Namespace Rules

- If the source file has `namespace Foo.Bar { … }` → use **the same namespace** in the test file,
  OR use `using Foo.Bar;` at the top and put the test class in `namespace EditModeTests`.
- If the source file has **no namespace** → derive a PascalCase namespace from the game/project name
  visible in the `.asmdef` `"references"` list (e.g. `"fps.Game"` → `Unity.FPS.Game`).
- Always put test classes inside `namespace EditModeTests` (matching the asmdef name when possible).

---

## Step 2 — Read the .asmdef File

When the user supplies an `.asmdef` file, extract:

```json
{
  "name": "EditModeTest",          // → test assembly name
  "references": ["fps.Game", …],   // → using directives / confirmed namespaces
  "includePlatforms": ["Editor"]   // → Edit Mode test
}
```

- Each entry in `"references"` maps to a **using directive** in the test file.
  - `"fps.Game"` → `using Unity.FPS.Game;`
  - `"fps.Gameplay"` → `using Unity.FPS.Gameplay;`
  - Convert dots to PascalCase segments and prepend `Unity.` only when the project convention
    shows that pattern (check the reference sample code for confirmation).
- `"includePlatforms": ["Editor"]` → use `[TestFixture]` / `[Test]` (Edit Mode, NUnit).
- If `"includePlatforms"` is absent or contains `"Standalone"` → consider Play Mode
  (`[UnityTest]` + `IEnumerator`).

---

## Step 3 — File & Class Structure

Always follow this exact template (mirroring `FPSMicrogameTests.cs`):

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
// one using per asmdef reference:
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace EditModeTests          // always EditModeTests (or asmdef "name" value)
{
    public class <SystemName>Tests
    {
        // ── Shared state ──────────────────────────────────────────────
        readonly System.Collections.Generic.List<Object> m_TestObjects =
            new System.Collections.Generic.List<Object>();

        // ── Lifecycle ─────────────────────────────────────────────────
        [SetUp]
        public void SetUp()
        {
            EventManager.Clear();   // include only when EventManager exists in the project
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_TestObjects)
                if (obj != null) Object.DestroyImmediate(obj);
            m_TestObjects.Clear();
            EventManager.Clear();   // same as above
        }

        // ── Helper ────────────────────────────────────────────────────
        GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        // ── Tests ─────────────────────────────────────────────────────
        // Each test has a Vietnamese or English comment block explaining its intent.
        [Test]
        public void Test<Feature>()
        {
            // …
        }
    }
}
```

**Rules:**
- Include `EventManager.Clear()` in `SetUp`/`TearDown` **only** if the project has an `EventManager`.
- `CreateTestObject` is always present — it ensures objects are cleaned up.
- Use `m_` prefix for all instance fields (Unity convention).
- Test method names: `Test<Feature>` e.g. `TestScore`, `TestPlayerHealth`, `TestWeaponInventory`.

---

## Step 4 — Writing Individual Tests

### 4a. Component / MonoBehaviour Tests

```csharp
[Test]
public void TestComponentName()
{
    // 1. Create GameObject, disable before adding complex components
    var go = CreateTestObject("Name");
    go.SetActive(false);                      // prevent Awake/Start side-effects
    var comp = go.AddComponent<MyComponent>();

    // 2. Set public fields directly
    comp.SomePublicField = value;

    // 3. Assert initial state
    Assert.AreEqual(expected, comp.SomeProperty);

    // 4. Call public methods or use Reflection for private ones
    comp.PublicMethod(args);
    Assert.AreEqual(expected, comp.Result);
}
```

### 4b. Accessing Private Members via Reflection

Use `System.Reflection` when a private field or method must be verified:

```csharp
// Private field
var field = typeof(MyClass).GetField("m_FieldName",
    BindingFlags.NonPublic | BindingFlags.Instance);
Assert.IsNotNull(field, "m_FieldName should exist");
var value = (int)field.GetValue(instance);

// Private method
var method = typeof(MyClass).GetMethod("MethodName",
    BindingFlags.NonPublic | BindingFlags.Instance);
Assert.IsNotNull(method, "MethodName should exist");
method.Invoke(instance, new object[] { arg1, arg2 });
```

> Always assert that the reflected member is not null with a descriptive message.

### 4c. Event / Delegate Tests

```csharp
// Subscribe to an Action event
bool eventFired = false;
component.OnSomeEvent += () => eventFired = true;

// Trigger condition
component.TakeDamage(100f, null);

// Verify exactly once
Assert.IsTrue(eventFired);

// Verify NOT fired after dead-state
eventFired = false;
component.TakeDamage(10f, null);
Assert.IsFalse(eventFired);
```

### 4d. Struct / Value-Type Events (e.g. EnemyKillEvent)

```csharp
var evt = Events.EnemyKillEvent;   // get shared struct instance
evt.Enemy = null;
evt.RemainingEnemyCount = 2;
onEnemyKilledMethod.Invoke(objective, new object[] { evt });
```

---

## Step 5 — Comment Convention

Every `[Test]` method **must** have a Vietnamese or bilingual comment block immediately above it that:
1. States what component/system is being tested.
2. Lists the key steps (setup, actions, assertions).
3. Describes edge cases covered.

```csharp
// Kiểm tra <SystemName>: <brief description of what's tested>,
// <step 1>, <step 2>, xác nhận <assertion summary>.
[Test]
public void TestFeature() { … }
```

---

## Step 6 — .asmdef File (if not provided)

If the user does NOT supply an `.asmdef`, generate one alongside the test file:

```json
{
    "name": "EditModeTests",
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "references": [
        "<AssemblyRef1>",
        "<AssemblyRef2>"
    ],
    "includePlatforms": [
        "Editor"
    ]
}
```

Populate `"references"` from the `using` directives you identified in the source scripts.

---

## Step 7 — Play Mode Tests (when needed)

If the user requests Play Mode tests (runtime behaviour, coroutines, physics):

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace PlayModeTests
{
    public class MyPlayModeTests
    {
        [UnityTest]
        public IEnumerator TestCoroutineBehaviour()
        {
            // setup
            yield return null;  // wait one frame
            // assert
        }
    }
}
```

And the `.asmdef` should use:
```json
{ "includePlatforms": [] }   // empty = all platforms including runtime
```

---

## Quality Checklist

Before outputting the final test file, verify:

- [ ] `using System.Reflection;` present when Reflection is used
- [ ] `using NUnit.Framework;` present
- [ ] `using UnityEngine;` present
- [ ] All `asmdef "references"` have corresponding `using` directives
- [ ] Namespace matches `asmdef "name"` value (`EditModeTests`)
- [ ] Every `[Test]` has a Vietnamese comment block
- [ ] Every GameObject created via `CreateTestObject()` (not `new GameObject()`)
- [ ] Reflection asserts (`IsNotNull`) with descriptive failure messages
- [ ] `TearDown` calls `DestroyImmediate` on all test objects
- [ ] No hardcoded `Update()`/`Start()` calls — use `go.SetActive(false)` to suppress them

---

## Reference Example

See the uploaded `FPSMicrogameTests.cs` for a complete, correct example covering:
- Score / objective system (`ObjectiveKillEnemies`) with private method + field reflection
- Health component (`Health`) with event delegate
- Weapon inventory (`PlayerWeaponsManager` + `WeaponController`) with array slot manipulation

Use this file as the gold-standard pattern for structure, naming, and assertion style.
