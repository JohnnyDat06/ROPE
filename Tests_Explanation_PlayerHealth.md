# Hướng dẫn chi tiết File Unit Test (PlayerHealthTest)

Dưới đây là giải thích chi tiết về file Test `PlayerHealthTest.cs` trong môi trường Unity Edit Mode (sử dụng NUnit Framework).

## 1. Giải thích tham chiếu (using)
* **`using System.Reflection;`**: Cung cấp các API Reflection trong C# để có thể truy xuất, chỉnh sửa các biến, hàm `private` hoặc `protected` mà bình thường không thể truy cập từ bên ngoài class.
* **`using NUnit.Framework;`**: Thư viện lõi framework test của Unity. Cung cấp các Object Attributes như `[Test]`, `[TearDown]` và hệ thống lệnh `Assert` để đánh giá đúng/sai.
* **`using UnityEngine;`**: Thư viện của Unity engine cho phép thao tác tạo `GameObject`, sử dụng các hàm toán học (như `Mathf`).
* **`using StarterAssets;`**: Namespace chứa các Script điều khiển nhân vật mặc định (`ThirdPersonController`, `StarterAssetsInputs`...).
* **`using DatScript;`**: Namespace tự định nghĩa chứa kịch bản chính `PlayerHealth` đang được test.

## 2. Vì sao file này lại không cần dùng `[SetUp]`?
Theo chu trình chạy test bình thường, `[SetUp]` sẽ chạy trước MỖI hàm `[Test]` để chuẩn bị dữ liệu (ví dụ: tạo Object giả). 
Tuy nhiên trong thiết kế file này, tác giả đã không dùng `[SetUp]` mà tự tạo một hàm **Helper (`SetupPlayerHealth`)**. 
**Lý do:**
1. Tránh việc phải dùng các biến toàn cục (global test state) trong class Test. Hàm `SetupPlayerHealth()` sẽ khởi tạo nội bộ trong từng hàm Test và **trả về trực tiếp** PlayerHealth để sử dụng cục bộ.
2. Kiểm soát tốt hơn vòng đời lập trình, nếu cần có thể luân chuyển thiết lập các tham số đầu vào cho Object dễ dàng hơn cho tuỳ từng hàm Test. 
Thay vì `[SetUp]` thì NUnit vẫn nhận và gọi `[TearDown]` để phá hủy rác (danh sách `m_TestObjects`) cho mọi object đã chạy.

---

## 3. Giải thích Đoạn code bằng Reflection (Gọi hàm Start ẩn)
Bạn có thắc mắc đoạn code:
```csharp
var startMethod = typeof(PlayerHealth).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
if (startMethod != null) startMethod.Invoke(health, null);
```
* `typeof(PlayerHealth)`: Lấy ra đối tượng System.Type biểu diễn class `PlayerHealth`.
* `GetMethod(...)`: Yêu cầu tìm hàm có tên là **"Start"**. `BindingFlags.NonPublic` yêu cầu lôi hàm dù nó bị private, `BindingFlags.Instance` chỉ định đây là hàm của một đối chiếu thực (object) thay vì hàm static.
* `startMethod.Invoke(health, null)`: Thực thi hàm `Start()` đó trực tiếp lên object `health`. 
**Ý nghĩa:** Tại sao lại phải lôi hàm Start ra chạy trong khi Unity sẽ tự gọi? Vì ở trên chúng ta sử dụng `go.SetActive(false)`, ép Unity bỏ qua vòng đời `Awake/Start`. Nên khi muốn tái tạo lại y như lúc game chạy, ta phải chọc thẳng API Reflection để gọi thủ công chức năng bên trong `Start()`.

---

## 4. Tại sao hàm `SetupPlayerHealth` lại thiết lập Health bằng Reflection mà không xài Start?
Người thiết kế test muốn đi thẳng vấn đề và kiểm soát sát sao:
```csharp
var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
if (maxHealthField != null) maxHealthField.SetValue(health, 100f);

var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
if (currentHealthField != null) currentHealthField.SetValue(health, 100f);
```
**Ý nghĩa:**
1. Nếu gọi `Start()`, súng ống, animation và một số thứ UI khác có thể bị kéo theo rắc rối (gây throw lỗi `NullReferenceException` do thiếu Component). Người viết test chỉ muốn tập trung test module toán logic (Máu).
2. Code tận dụng Reflection để tự trỏ vào biến `maxHealth` và biến private ẩn `currentHealth`, sau đó dùng `.SetValue(health, 100f)` ép trực tiếp số lượng 100 máu lên object `health`. Điều này tạo ra một state game chuẩn xác giả lập 100% người chơi đang đầy cây máu mà không cần phụ thuộc vào Code trong `Start()`.

*(Để phần test chạy chính xác và không bị đụng chạm bug từ các lệnh `GetComponent` trong `Start`, tôi đã vá lại file bằng cách inject tương tự các `animator`, `activeWeapon`... cho file của bạn qua Reflection)*.

---

## 5. Luồng chạy Test (Flow của từng TestCase)

1. **`TestTakeDamageReducesHealth`**:
   * *Luồng:* Gọi giả lập Player đầy máu. Kích hoạt hàm trừ máu `TakeDamage(25f)`.
   * *Kiểm chứng:* Dùng Reflection soi lại biến private máu hiện tại (`currentHealth`), gọi `Assert.AreEqual` định đoạt máu từ 100 phải xuống đúng mốc $100 - 25 = 75$.

2. **`TestTakeDamageBelowZeroTriggersDeathStateAndClampsHealth`**:
   * *Luồng:* Cử Player ăn sát thương chí mạng 999 sát thương. 
   * *Kiểm chứng:* Máu chặn ở số không âm (kẹt lại ở không `0`). Đồng thời kiểm tra để chắc chắn code đã vô hiệu hoá các Component cho tương tác như: di chuyển (`ThirdPersonController.enabled = false`), nhập liệu bắn súng (`StarterAssetsInputs.enabled = false`).

3. **`TestHealIncreasesHealthButRestrictsToMax`**:
   * *Luồng:* Ép máu từ 100 xuống 50. Gọi hành vi hồi máu (`Heal(20f)`). Sau đó lại gọi hồi điên cuồng 500 máu (`Heal(500f)`).
   * *Kiểm chứng:* Lần hồi đầu máu sẽ lên 70 ($50+20$). Lần hồi 500 máu, thuật toán hàm `Heal` phải kìm máu tối đa ở ranh giới 100 (Không được cộng lố ra 570 máu).

4. **`TestResetHealthRestoresComponentsAndHealth`**:
   * *Luồng:* Cử Player dính 100 dame và gục mạng. Sau đó gọi hàm lật ngược tình thế sống dậy (`ResetHealth()`).
   * *Kiểm chứng:* Biến `currentHealth` phải khôi phục 100. Đảm bảo các hệ thống di chuyển được hoạt động trở lại bình thường (`enabled = true`). Đoạn test xác thực nhân vật cải tử hoàn sinh tốt đẹp.
