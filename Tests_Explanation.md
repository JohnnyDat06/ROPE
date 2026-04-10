# Giải thích chi tiết các file Script Auto Test trong dự án

Tài liệu này giải thích chi tiết mục đích, luồng hoạt động, cấu trúc và từng thành phần tham gia vào quá trình test tự động (Automated Testing) dựa trên 3 file: `PlayerHealthTest.cs`, `PlayerMovementTest.cs`, và `WeaponTest.cs`.

---

## 1. Thành phần chung (Common Elements)

### Các chỉ thị `using` (Namespaces)
- `using System.Reflection;`: Cung cấp thư viện Reflection của C#. Kỹ thuật này được dùng để thâm nhập vào các biến (fields) và hàm (methods) được khai báo là `private` hoặc `protected` trong mã nguồn dự án mà không cần phải đổi chúng thành `public`. 
- `using NUnit.Framework;`: Thư viện lõi cho Unit Test trong Unity qua package NUnit. Cung cấp các attribute (`[Test]`, `[SetUp]`, `[TearDown]`) và lớp `Assert` để so sánh, đối chiếu kết quả kỳ vọng.
- `using UnityEngine;`: Cho phép script truy cập vào Unity Engine API để tạo ra các `GameObject`, gán các Component, ScriptableObject,...
- `using StarterAssets;`, `using DatScript;`: Thư viện nội bộ của dự án chứa các component cần kiểm thử như `ThirdPersonController`, `PlayerHealth`,...

### Unit Test Attributes
- `[SetUp]`: Đánh dấu hàm khởi tạo, sẽ được tự động gọi TRƯỚC mỗi khi một hàm `[Test]` được chạy. Dùng để cấu hình môi trường chuẩn bị.
- `[TearDown]`: Đánh dấu hàm dọn dẹp, sẽ được tự động gọi SAU mỗi khi một hàm `[Test]` vừa chạy xong. Dùng để dọn rác, hủy các `GameObject` rác sinh ra trong lúc test (thông qua `Object.DestroyImmediate(obj)`).
- `[Test]`: Đánh dấu một hàm là một Test Case để Unity Test Runner có thể tìm thấy và chạy kiểm thử độc lập.

---

## 2. Giải thích `PlayerHealthTest.cs`

**Mục tiêu file test:** Kiểm thử tính đúng đắn của script `PlayerHealth` làm nhiệm vụ quản lý máu, nhận rủi ro, hồi sinh và xử lý hồi máu của người chơi.

**Luồng Prepare Data (`SetupPlayerHealth`):**
1. Tạo một `GameObject` tên "Player" và `SetActive(false)` lập tức. Việc set false giúp ngăn không cho các hàm `Awake` và `Start` của MonoBehaviour chạy tự động (gây lỗi null reference nếu thiếu thành phần).
2. Lần lượt `AddComponent` các đoạn script vật lý và điều khiển bắt buộc (`StarterAssetsInputs`, `ThirdPersonController`, `ActiveWeapon`, `Animator`, và `PlayerHealth`).
3. Dùng Reflection gọi ép hàm `Start()` của `PlayerHealth` (vốn là private) để mô phỏng khởi tạo component.

**Các Test Cases:**
- `TestTakeDamageReducesHealth()`:
  - **Mô tả:** Kiểm tra hàm chịu sát thương.
  - **Luồng:** Gọi `health.TakeDamage(25f)` với tham số 25 damage.
  - **Kiểm chứng:** Dùng Reflection lấy biến private `currentHealth`, dùng `Assert.AreEqual` kiểm tra xem máu có trừ từ (có thể là) 100 xuống 75 không.
- `TestTakeDamageBelowZeroTriggersDeathStateAndClampsHealth()`:
  - **Mô tả:** Kiểm tra logic nếu chịu sát thương quá lớn dẫn đến nhân vật chết (về 0).
  - **Luồng:** Gọi `TakeDamage(999f)`.
  - **Kiểm chứng:** Máu hiện tại (`currentHealth`) phải dừng mức nhỏ nhất là 0 (Clamp). Phải disable (tắt) các component điều khiển, vũ khí (`controller.enabled == false`, v.v...).
- `TestHealIncreasesHealthButRestrictsToMax()`:
  - **Mô tả:** Kiểm tra hàm bơm máu `Heal()`.
  - **Luồng:** Ép máu hiện tại xuống 50. Bơm `Heal(20f)`. Kỳ vọng máu lên 70. Bơm lố `Heal(500f)`.
  - **Kiểm chứng:** Máu sẽ không thể vượt quá giá trị 100.
- `TestResetHealthRestoresComponentsAndHealth()`:
  - **Mô tả:** Kiểm tra hàm hồi sinh `ResetHealth()`.
  - **Luồng:** Đánh chết nhân vật `TakeDamage(100f)`, sau đó gọi `ResetHealth()`.
  - **Kiểm chứng:** Máu phải full lại (100). Các trình điều khiển bị tắt ở hàm chết phải được bật trở lại (`enabled = true`).

---

## 3. Giải thích `PlayerMovementTest.cs`

**Mục tiêu file test:** Kiểm thử tính ổn định của cụm logic camera/chuyển động `ThirdPersonController`.

**Luồng test:**
- `TestDefaultMovementValues()`:
  - Sinh GameObject ẩn, thêm component `ThirdPersonController`.
  - Dùng `Assert.AreEqual` đối chiếu các giá trị mặc định lúc khởi tạo component như tốc độ đi bộ (2.0), tốc độ chạy (5.335), sức bật (1.2)... có đúng như thiết kế hay không.
- `TestSetSensitivity()`:
  - Gọi hàm cấu hình độ nhạy `SetSensitivity(3.5f)`.
  - Kiểm tra xem Property `.Sensitivity` có được cập nhật thành số 3.5 đúng như mong muốn không.
- `TestPrivateTerminalVelocityUsingReflection()`:
  - Dùng hàm thuộc Reflection `GetField("Tên biến", BindingFlags.NonPublic | BindingFlags.Instance)` để chọc vào lấy giá trị `_terminalVelocity` nội bộ ra.
  - Xác nhận giá trị rơi tự do vô cực có được gán mặc định là 53.0f hay không.
- `TestAddRecoilUpdatesPitchAndYaw()`:
  - Phục vụ kiểm tra tính năng "Giật súng" (Recoil) trên Camera.
  - Dùng Reflection reset góc `_cinemachineTargetPitch` về 0.
  - Gọi `AddRecoil(2.0f, 0.0f)`. Camera bị đẩy lên 2.0f chiều dọc.
  - Kỳ vọng giá trị Pitch bị kéo xuống thành `-2.0f` (âm) mô phỏng góc nhìn giật cao lên trên.

---

## 4. Giải thích `WeaponTest.cs`

**Mục tiêu file test:** Kiểm thử trạng thái của súng đạn (`RaycastWeapon`) bao gồm bắn, thay đạn, dự trữ băng đạn.

**Luồng Prepare Data (`SetupWeaponAndAmmo`):**
- Hàm tiện ích này tạo Súng (`RaycastWeapon`) trên Game Object, đồng thời tạo một lớp `AmmoConfigSO` (kế thừa từ `ScriptableObject`) đại diện cho thông số đạn.
- Do hàm `OnEnable` của lưới `ScriptableObject` ko chạy, nên Script dùng Reflection gọi hàm `OnEnable` ép để `AmmoConfigSO` có thể reset giá trị mặc định đầu game.

**Các Test Cases:**
- `TestWeaponCanReloadCondition()`:
  - Cấu hình đạn trong băng = 10, đạn tổng = 90. Gọi hàm `weapon.CanReload()` trả về True (đủ điều kiện thay).
  - Đổi tổng đạn = 0. Gọi `weapon.CanReload()` trả về False (hết đạn, không thể nạp).
- `TestStartReloadUpdatesWeaponState()`:
  - Can thiệp đổi trạng thái trước khi nạp (`isFiring` = true, `isReloading` = false).
  - Gọi lệnh `weapon.StartReload()`.
  - Test buộc cờ chờ (isReloading) phải là **True** và phải rớt trạng thái bắn (isFiring = **False**).
- `TestRefillAmmoUpdatesAmmoMathAndRestoresState()`:
  - Mô phỏng khâu chốt tính toán vào băng: trong băng (5/30), đạn tổng 50. Đang ở chu trình (isReloading = true).
  - Gọi `weapon.RefillAmmo()`.
  - Kết quả: `isReloading` phải về false. Băng đạn đầy (30). Đạn tổng bị trừ sạch lượng 25 viên vào (còn 25).
- `TestStartFiringShouldBeBlockedWhenEmptyOrReloading()`:
  - Trạng thái 1: Đang nạp đạn (isReloading = true). Góp nút bắn bằng hàm `StartFiring()`. Kết quả: fail, không được bắn (`isFiring` giữ False).
  - Trạng thái 2: Không nạp đạn, nhưng rỗng băng (`currentClipAmmo = 0`). Gọi `StartFiring()`. Kết quả: fail, nòng súng cạch cạch tắt ngóm.

