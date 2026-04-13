# 📋 Hướng Dẫn Cấu Trúc Assembly và Test trong ROPE Project

## ✅ Cấu Trúc Mới (Đã Cập Nhật)

```
Assets/TestAutomationScripts/
├── Editor/
│   ├── EditTestMode.asmdef                    ← ✅ THÊMMỚI
│   ├── PlayerHealthTest.cs
│   ├── PlayerMovementTest.cs
│   └── WeaponTest.cs
└── PlayTestMode/
    ├── PlayTestMode.asmdef                     ← Đã tồn tại
    └── (play mode test files)
```

---

## 📌 Cài Đặt File `EditTestMode.asmdef`

### **Phần Quan Trọng - References**

```json
{
    "name": "EditTestMode",                      // Tên assembly
    "rootNamespace": "EditModeTests",           // Namespace gốc (tùy chọn)
    "references": [
        "UnityEngine.TestRunner",               // ✅ NUnit framework
        "UnityEditor.TestRunner",               // ✅ Unity editor test runner
        "Assembly-CSharp"                       // ✅ CODE GAME CỦA BẠN (RẤT QUAN TRỌNG!)
    ],
    "optionalUnityReferences": [
        "TestAssemblies"                        // ✅ Cho phép dùng NUnit
    ],
    "includePlatforms": [
        "Editor"                                 // Chỉ compile trong Editor
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": []
}
```

---

## 🔗 Mối Quan Hệ Giữa Các Assembly

```
┌─────────────────────────────────────────────────────┐
│  EditTestMode.asmdef (Editor Tests)                │
│  • Namespace: EditModeTests                         │
│  • Can access: StarterAssets, DatScript, etc.       │
└─────────────────┬───────────────────────────────────┘
                  │ references
                  ↓
        ┌─────────────────────────────────────────┐
        │  Assembly-CSharp                        │
        │  • Code game chính (Default Assembly)   │
        │  • Namespaces:                          │
        │    - StarterAssets                      │
        │    - DatScript                          │
        │    - Tất cả game logic                  │
        └─────────────────────────────────────────┘
```

---

## 🎯 Các Class Có Sẵn Mà Test Sử Dụng

### **Từ namespace `StarterAssets`:**
- `StarterAssetsInputs` (Assets/_Game/Scripts/_Utils/InputSystem/)
- `ThirdPersonController` (Assets/_Game/Scripts/_Characters/Player/)
- `PlayerAnimationController`

### **Từ namespace `DatScript`:**
- `PlayerHealth` (Assets/_Game/Scripts/_Characters/Player/)
- `GameManager` (Assets/_Game/Scripts/_Core/)

### **Không trong namespace:**
- `ActiveWeapon` (Assets/_Game/Scripts/Gun/)
- `RaycastWeapon`
- `AmmoConfigSO`

---

## ✨ Lợi Ích Của Cấu Trúc Này

| Lợi Ích | Giải Thích |
|---------|-----------|
| **Cách Ly Code** | Test Editor không ảnh hưởng Build game |
| **Compile Nhanh** | Chỉ recompile assembly bị thay đổi |
| **Dependency Rõ Ràng** | Biết chính xác test phụ thuộc vào code nào |
| **Tránh Circular Dependency** | Không thể reference ngược về test từ game |
| **Dễ Bảo Trì** | Cấu trúc tường minh cho team phát triển |

---

## 🛠️ Các Tests Hiện Tại & Namespaces Được Sử Dụng

### **PlayerHealthTest.cs**
```csharp
using StarterAssets;        // ✅ từ Assembly-CSharp
using DatScript;            // ✅ từ Assembly-CSharp
```
**Tests:** TakeDamage, Heal, ResetHealth

### **PlayerMovementTest.cs**
```csharp
using StarterAssets;        // ✅ từ Assembly-CSharp
```
**Tests:** DefaultMovementValues, SetSensitivity, AddRecoil, PrivateTerminalVelocity

### **WeaponTest.cs**
```csharp
// Chỉ dùng UnityEngine, không cần import từ Assembly-CSharp
// (RaycastWeapon, AmmoConfigSO được load via GameObject.AddComponent)
```
**Tests:** CanReload, StartReload, RefillAmmo, StartFiring

---

## 🚀 Cách Chạy Tests

### **Từ Unity Editor:**

1. **Window → General → Test Runner**
2. **Tab "EditMode"** → Thấy các test class
3. **Nhấn "Run All"** hoặc chọn test riêng

### **Expected Output (Nếu Cấu Hình Đúng):**
```
✓ PlayerHealthTest.TestTakeDamageReducesHealth
✓ PlayerHealthTest.TestTakeDamageBelowZeroTriggersDeathStateAndClampsHealth
✓ PlayerHealthTest.TestHealIncreasesHealthButRestrictsToMax
✓ PlayerHealthTest.TestResetHealthRestoresComponentsAndHealth
✓ PlayerMovementTest.TestDefaultMovementValues
✓ PlayerMovementTest.TestSetSensitivity
✓ PlayerMovementTest.TestAddRecoilUpdatesPitchAndYaw
✓ PlayerMovementTest.TestPrivateTerminalVelocityUsingReflection
✓ WeaponTest.TestWeaponCanReloadCondition
✓ WeaponTest.TestStartReloadUpdatesWeaponState
✓ WeaponTest.TestRefillAmmoUpdatesAmmoMathAndRestoresState
✓ WeaponTest.TestStartFiringShouldBeBlockedWhenEmptyOrReloading
```

---

## ⚠️ Nếu Vẫn Gặp Lỗi `NullReferenceException`

### **Checklist:**

- [ ] File `EditTestMode.asmdef` tồn tại trong `Assets/TestAutomationScripts/Editor/`
- [ ] Có dòng `"Assembly-CSharp"` trong `"references"`
- [ ] File `.meta` tự động được sinh ra
- [ ] Reload project (Close → Reopen)
- [ ] Xóa thư mục `Library` nếu vẫn lỗi → Unity sẽ rebuild

### **Nếu Compile Fail:**
```
error: Could not load type 'DatScript.PlayerHealth' from assembly
```
→ Giải pháp: Đảm bảo `"Assembly-CSharp"` có trong `references`

---

## 📚 Tài Liệu Tham Khảo

- [Assembly Definition Files - Unity](https://docs.unity3d.com/Manual/AssemblyDefinitions.html)
- [Unit Testing in Play Mode and Edit Mode - Unity](https://docs.unity3d.com/Manual/test-framework/workflow-run-tests.html)

---

**Status:** ✅ Cấu trúc đã được cập nhật thành công!

Ngày tạo: April 9, 2026

