# WeaponBehaviourSuiteTests Explanation

File được giải thích: `Assets/Tests/EditModeTests/WeaponBehaviourSuiteTests.cs`

Mục tiêu của file test này là kiểm tra 5 hành vi cốt lõi của hệ thống súng:

1. Click bắn thì có đạn được tạo ra.
2. Reload thì băng đạn được nạp lại.
3. Holster thì trạng thái cất súng đổi đúng.
4. Ammo bằng 0 thì không được phép bắn.
5. Đang reload thì không được phép bắn.

File test này là `EditMode Test`, nghĩa là nó kiểm tra logic C# trực tiếp trong editor, không cần chạy cả scene gameplay hoàn chỉnh. Vì vậy trong file có nhiều hàm helper để tự dựng object test tối thiểu.

---

## 1. Namespace và class

### `namespace ROPE.Tests`

- Ý nghĩa:
  Gom các class test vào cùng namespace với các test khác của dự án.
- Đầu vào:
  Không có.
- Đầu ra:
  Không có.
- Tại sao viết như vậy:
  Giúp tổ chức code rõ ràng, tránh xung đột tên class.

### `public class WeaponBehaviourSuiteTests`

- Ý nghĩa:
  Đây là class chứa toàn bộ test suite.
- Đầu vào:
  Không có tham số.
- Đầu ra:
  Không có giá trị trả về.
- Tại sao viết như vậy:
  NUnit yêu cầu các test được đặt trong class, và mỗi hàm `[Test]` trong class sẽ là một test case độc lập.

---

## 2. Biến dùng chung

### `readonly List<Object> m_TestObjects = new List<Object>();`

- Ý nghĩa:
  Danh sách này lưu tất cả `GameObject`, `ScriptableObject` được tạo ra trong lúc test.
- Đầu vào:
  Không có.
- Đầu ra:
  Một danh sách rỗng lúc khởi tạo.
- Tại sao viết như vậy:
  Trong test Unity, nếu tạo object mà không dọn thì test sau có thể bị ảnh hưởng. Danh sách này giúp gom mọi object test lại để `TearDown()` xóa sạch sau mỗi test.

---

## 3. Hàm dọn dẹp sau test

### `[TearDown] public void TearDown()`

- Ý nghĩa:
  Hàm này chạy sau mỗi test case.
- Đối số:
  Không có.
- Đầu vào:
  Sử dụng `m_TestObjects` đã được thêm object trong quá trình test.
- Đầu ra:
  Không trả về gì.
- Việc nó làm:
  1. Duyệt qua toàn bộ object test.
  2. Nếu object là `RaycastWeapon` thì lấy danh sách `bullets` private bằng reflection.
  3. Với mỗi bullet, lấy `tracer` và hủy `tracer.gameObject`.
  4. Sau đó hủy toàn bộ object còn lại trong `m_TestObjects`.
  5. Xóa rỗng danh sách.
- Tại sao viết như vậy:
  `RaycastWeapon` khi bắn sẽ `Instantiate` ra tracer clone. Clone này không tự nằm trong `m_TestObjects`, nên nếu không dọn riêng, nó có thể sót lại trong editor giữa các test.
- Vì sao dùng `DestroyImmediate`:
  Đây là EditMode Test, không chạy theo frame runtime đầy đủ. `DestroyImmediate` đảm bảo object bị xóa ngay lập tức.

---

## 4. Hàm helper tạo object

### `GameObject CreateTestObject(string name)`

- Ý nghĩa:
  Tạo nhanh một `GameObject` phục vụ test.
- Đối số:
  - `name`: tên object muốn tạo.
- Đầu vào:
  Một chuỗi tên object.
- Đầu ra:
  Trả về `GameObject` vừa tạo.
- Việc nó làm:
  1. `new GameObject(name)`
  2. Thêm object đó vào `m_TestObjects`.
  3. Trả object về cho nơi gọi.
- Tại sao viết như vậy:
  Tạo object test lặp lại rất nhiều, nên tách ra một helper để code gọn hơn và chắc chắn object nào tạo ra cũng được đăng ký để dọn dẹp.

### `T CreateTestAsset<T>() where T : ScriptableObject`

- Ý nghĩa:
  Tạo nhanh một `ScriptableObject` cho test.
- Đối số:
  Không có đối số thường.
  `T` là kiểu generic, ví dụ `AmmoConfigSO`.
- Đầu vào:
  Kiểu `T` phải kế thừa `ScriptableObject`.
- Đầu ra:
  Trả về instance của `T`.
- Việc nó làm:
  1. `ScriptableObject.CreateInstance<T>()`
  2. Thêm asset vừa tạo vào `m_TestObjects`.
  3. Trả asset về.
- Tại sao viết như vậy:
  Dự án có `AmmoConfigSO`, nên test cần tạo asset tạm trong bộ nhớ. Viết generic giúp tái dùng cho nhiều loại `ScriptableObject`.

---

## 5. Hàm helper dùng Reflection

### `static void InvokeNonPublic(object instance, string methodName)`

- Ý nghĩa:
  Gọi một hàm `private` hoặc `protected` bằng reflection.
- Đối số:
  - `instance`: object chứa method cần gọi.
  - `methodName`: tên method cần gọi.
- Đầu vào:
  Một object thật và tên method dạng chuỗi.
- Đầu ra:
  Không trả về giá trị.
- Việc nó làm:
  1. Tìm method bằng `BindingFlags.Instance | BindingFlags.NonPublic`.
  2. Assert method phải tồn tại.
  3. Gọi method đó với `Invoke(instance, null)`.
- Tại sao viết như vậy:
  Trong Unity, nhiều method như `Start()` hoặc `ShootControl()` là `private`, test không gọi trực tiếp được. Reflection cho phép test chạm đúng logic gốc mà không phải sửa mã nguồn production.

### `static void SetPrivateField(object instance, string fieldName, object value)`

- Ý nghĩa:
  Gán giá trị cho field private.
- Đối số:
  - `instance`: object chứa field.
  - `fieldName`: tên field private.
  - `value`: giá trị muốn gán.
- Đầu vào:
  Object, tên field, giá trị mới.
- Đầu ra:
  Không trả về gì.
- Việc nó làm:
  1. Tìm field bằng reflection.
  2. Assert field phải tồn tại.
  3. Gán `value` vào field đó.
- Tại sao viết như vậy:
  `RaycastWeapon` có các field serialize/private như `muzzleFlash`, `raycastOrigin`, `tracerEffect`. Test cần cấu hình chúng nhưng không có setter public, nên dùng reflection.

### `static IList GetBullets(RaycastWeapon weapon)`

- Ý nghĩa:
  Lấy danh sách `bullets` private từ `RaycastWeapon`.
- Đối số:
  - `weapon`: đối tượng súng đang test.
- Đầu vào:
  Một `RaycastWeapon`.
- Đầu ra:
  Trả về `IList` chứa các bullet nội bộ.
- Tại sao viết như vậy:
  `bullets` là field private, nhưng test cần kiểm tra sau khi bắn có bullet nào được thêm vào hay chưa. `IList` được dùng vì test chỉ cần đếm số phần tử, không cần truy cập kiểu mạnh.

---

## 6. Hàm tạo ammo config cho test

### `AmmoConfigSO CreateAmmoConfig(int clipAmmo = 30, int clipSize = 30, int reserveAmmo = 90)`

- Ý nghĩa:
  Tạo một cấu hình đạn với số lượng mong muốn cho từng test.
- Đối số:
  - `clipAmmo`: số đạn hiện có trong băng.
  - `clipSize`: kích thước tối đa của băng.
  - `reserveAmmo`: số đạn dự trữ.
- Đầu vào:
  Ba số nguyên, đều có giá trị mặc định.
- Đầu ra:
  Trả về `AmmoConfigSO`.
- Việc nó làm:
  1. Tạo `AmmoConfigSO`.
  2. Gọi `OnEnable()` bằng reflection.
  3. Gán lại `clipSize`, `currentClipAmmo`, `currentAmmo` theo nhu cầu test.
- Tại sao phải gọi `OnEnable()`:
  `AmmoConfigSO` của dự án khởi tạo giá trị ban đầu trong `OnEnable()`. Nếu không gọi, object mới có thể chưa ở đúng trạng thái logic mà code production mong đợi.
- Tại sao lại gán lại sau `OnEnable()`:
  Vì từng test cần trạng thái riêng, ví dụ băng còn 5 viên hoặc bằng 0.

---

## 7. Hàm tạo súng cho test

### `RaycastWeapon CreateConfiguredWeapon(string name = "Pistol", int clipAmmo = 30, int clipSize = 30, int reserveAmmo = 90, bool keepInactive = true)`

- Ý nghĩa:
  Tạo một `RaycastWeapon` đã được gắn đủ cấu hình tối thiểu để test bắn/reload.
- Đối số:
  - `name`: tên súng, đồng thời dùng làm `weaponName`.
  - `clipAmmo`: số đạn hiện tại trong băng.
  - `clipSize`: sức chứa tối đa của băng.
  - `reserveAmmo`: đạn dự trữ.
  - `keepInactive`: có giữ object ở trạng thái inactive hay không.
- Đầu vào:
  5 tham số cấu hình.
- Đầu ra:
  Trả về `RaycastWeapon` đã sẵn sàng cho test.
- Việc nó làm:
  1. Tạo `GameObject` cho súng.
  2. Có thể để object inactive.
  3. Add component `RaycastWeapon`.
  4. Gán `weaponName`, `ammoConfig`, `fireRate`.
  5. Tạo `origin` và `destination` cho ray bắn.
  6. Tạo `TrailRenderer` prefab để giả lập tracer.
  7. Dùng reflection gán các field private cần thiết.
  8. Trả `weapon`.

#### Ý nghĩa từng đối số

- `name`
  Dùng để đặt tên object và gán vào `weapon.weaponName`.
  Điều này quan trọng vì một số logic animation/recoil trong project dùng tên súng.

- `clipAmmo`
  Điều khiển trạng thái ban đầu của băng đạn.
  Dùng để test các tình huống như còn 30 viên, còn 5 viên, hoặc 0 viên.

- `clipSize`
  Dùng để xác định reload có nạp đầy đến bao nhiêu.

- `reserveAmmo`
  Dùng cho case reload, để kiểm tra số đạn dự trữ bị trừ đúng.

- `keepInactive`
  Nếu `true`, object súng bị tắt để tránh chạy các vòng đời Unity không cần thiết.
  Nếu `false`, object active để `ActiveWeapon.Start()` có thể tìm thấy súng bằng `GetComponentInChildren<RaycastWeapon>()`.

#### Tại sao chỗ này lại viết như vậy

- `weaponObject.SetActive(!keepInactive ? true : false);`
  Mục đích thật sự là:
  - `keepInactive = true` thì object inactive.
  - `keepInactive = false` thì object active.
  Viết này hơi dài, nhưng nó thể hiện rõ ý đồ phụ thuộc vào cờ `keepInactive`.

- `SetPrivateField(weapon, "muzzleFlash", new ParticleSystem[0]);`
  Vì `FireBullet()` lặp qua `muzzleFlash`. Nếu field này null thì dễ phát sinh lỗi. Mảng rỗng là cấu hình an toàn tối thiểu.

- `SetPrivateField(weapon, "raycastOrigin", origin);`
  `FireBullet()` cần vị trí đầu bắn.

- `SetPrivateField(weapon, "tracerEffect", tracer);`
  `CreateBullet()` gọi `Instantiate(tracerEffect, ...)`, nên bắt buộc phải có tracer prefab.

- `weapon.raycastDestination = destination;`
  Dùng điểm đích để tính vận tốc đầu viên đạn.

---

## 8. Hàm tạo ActiveWeapon cho test holster

### `ActiveWeapon CreateConfiguredActiveWeapon()`

- Ý nghĩa:
  Tạo một player tối thiểu có thể test được logic holster.
- Đối số:
  Không có.
- Đầu vào:
  Không nhận tham số từ bên ngoài, nhưng tự dựng đầy đủ component cần thiết.
- Đầu ra:
  Trả về `ActiveWeapon`.
- Việc nó làm:
  1. Tạo player object.
  2. Add `ActiveWeapon`, `StarterAssetsInputs`, `Animator`.
  3. Load `RigController.controller` từ project.
  4. Tạo các transform cần thiết: `crossHairTarget`, `weaponParent`, `backSocket`, `hipSocket`.
  5. Gán các transform đó vào `ActiveWeapon`.
  6. Tạo một `RaycastWeapon` active.
  7. Gắn weapon vào player.
  8. Gọi `Start()` của `ActiveWeapon` bằng reflection.
  9. Assert trạng thái sau khởi tạo phải hợp lệ.

#### Tại sao phải load `RigController.controller`

`ActiveWeapon.ShootControl()` dùng:

- `rigController.GetBool("holster_weapon")`
- `rigController.SetBool("holster_weapon", ...)`

Nếu animator không có parameter thật tên `holster_weapon`, test có thể không phản ánh đúng logic runtime. Dùng luôn controller thật của dự án giúp test đáng tin cậy hơn.

#### Tại sao lại có các assert ngay trong helper

Ví dụ:

- `Assert.IsFalse(activeWeapon.IsHolstered, ...)`
- `Assert.AreSame(weapon, activeWeapon.CurrentWeapon)`

Các assert này giúp bắt lỗi sớm nếu khâu setup sai. Nếu setup đã hỏng mà vẫn chạy test chính, kết quả fail sẽ khó hiểu hơn.

---

## 9. Phần test case

### `[Test] public void ClickShoot_ShouldSpawnBullet()`

- Ý nghĩa:
  Kiểm tra khi bắt đầu bắn thì có bullet được tạo.
- Đối số:
  Không có.
- Đầu vào:
  Súng được tạo bởi `CreateConfiguredWeapon()`, mặc định có 30 viên.
- Đầu ra:
  Không trả về gì, chỉ assert.
- Trình tự:
  1. Tạo súng.
  2. Gọi `weapon.StartFiring()`.
  3. Gọi `weapon.UpdateFiring(0f)`.
  4. Kiểm tra `isFiring == true`.
  5. Kiểm tra số bullet trong list là 1.
  6. Kiểm tra đạn trong băng giảm từ 30 còn 29.

#### Tại sao phải gọi cả `StartFiring()` và `UpdateFiring(0f)`

- `StartFiring()` chỉ bật trạng thái bắn.
- Việc sinh bullet thật sự nằm trong `UpdateFiring()` gọi tiếp `FireBullet()`.

#### Tại sao truyền `0f`

Trong code hiện tại của `UpdateFiring()`:

- nó cộng `accumulatedTime += deltaTime`
- sau đó chạy `while (accumulatedTime >= 0f)`

Nghĩa là kể cả `deltaTime = 0`, vòng lặp vẫn chạy một lần và bắn ra 1 viên. Test này cố ý dựa trên đúng implementation hiện tại.

### `[Test] public void Reload_ShouldRefillAmmo()`

- Ý nghĩa:
  Kiểm tra reload nạp lại băng đạn và trừ đạn dự trữ đúng.
- Đối số:
  Không có.
- Đầu vào:
  Súng với:
  - `clipAmmo = 5`
  - `clipSize = 30`
  - `reserveAmmo = 50`
- Đầu ra:
  Không có giá trị trả về.
- Trình tự:
  1. Tạo súng với băng gần cạn.
  2. Gọi `StartReload()`.
  3. Gọi `RefillAmmo()`.
  4. Assert `isReloading == false`.
  5. Assert `currentClipAmmo == 30`.
  6. Assert `currentAmmo == 25`.

#### Tại sao test gọi cả `StartReload()` rồi mới `RefillAmmo()`

Trong gameplay thật:

1. Người chơi bắt đầu reload.
2. Súng chuyển sang state reload.
3. Khi animation/reload event kết thúc thì mới refill ammo.

Test mô phỏng đúng chuỗi đó thay vì gọi `RefillAmmo()` trực tiếp.

### `[Test] public void Holster_ShouldUpdateStateCorrectly()`

- Ý nghĩa:
  Kiểm tra khi có input holster thì state cất súng đổi đúng.
- Đối số:
  Không có.
- Đầu vào:
  Một `ActiveWeapon` đã được setup đầy đủ cùng `StarterAssetsInputs`.
- Đầu ra:
  Không trả về gì.
- Trình tự:
  1. Tạo player có `ActiveWeapon`.
  2. Lấy `StarterAssetsInputs`.
  3. Gán `input.holster = true`.
  4. Gọi `ShootControl()` lần 1.
  5. Gọi `ShootControl()` lần 2.
  6. Assert `IsHolstered == true`.
  7. Assert animator bool `holster_weapon == true`.

#### Tại sao phải gọi `ShootControl()` hai lần

Trong `ActiveWeapon.ShootControl()`:

1. Đầu hàm, `IsHolstered = rigController.GetBool("holster_weapon");`
2. Cuối hàm, nếu `input.holster` là true thì nó mới `SetBool("holster_weapon", !currentHolsterState);`

Nghĩa là:

- Lần gọi thứ nhất:
  Đọc state cũ trước, rồi mới đổi bool trong animator.
- Lần gọi thứ hai:
  `IsHolstered` mới đọc lại giá trị bool mới từ animator.

Nếu chỉ gọi 1 lần thì animator bool có thể đổi rồi, nhưng `IsHolstered` của `ActiveWeapon` chưa kịp cập nhật.

### `[Test] public void AmmoZero_ShouldNotShoot()`

- Ý nghĩa:
  Kiểm tra băng đạn rỗng thì không được bắn.
- Đối số:
  Không có.
- Đầu vào:
  Súng với `clipAmmo = 0`.
- Đầu ra:
  Không trả về gì.
- Trình tự:
  1. Tạo súng với 0 viên trong băng.
  2. Gọi `StartFiring()`.
  3. Assert `isFiring == false`.
  4. Assert số bullet vẫn là 0.

#### Tại sao test chỉ gọi `StartFiring()` mà không gọi `UpdateFiring()`

Vì logic chặn bắn khi hết đạn nằm ngay trong `StartFiring()`:

- nếu `currentClipAmmo <= 0` thì `isFiring = false` và return.

Nên chỉ cần test tại entry point này là đủ.

### `[Test] public void Reloading_ShouldNotShoot()`

- Ý nghĩa:
  Kiểm tra đang reload thì không được bắn.
- Đối số:
  Không có.
- Đầu vào:
  Súng còn đạn, nhưng `isReloading = true`.
- Đầu ra:
  Không trả về gì.
- Trình tự:
  1. Tạo súng còn 30 viên.
  2. Tự đặt `weapon.isReloading = true`.
  3. Gọi `StartFiring()`.
  4. Assert `isFiring == false`.
  5. Assert không có bullet nào được sinh.

#### Tại sao không cần mô phỏng full quy trình reload

Mục tiêu của case này là kiểm tra điều kiện chặn bắn:

- chỉ cần `isReloading = true` là đủ để xác nhận `StartFiring()` bị từ chối.

Không cần lặp lại animation hay event reload đầy đủ vì sẽ làm test dài hơn nhưng không tăng giá trị kiểm thử cho điều kiện này.

---

## 10. Đầu vào và đầu ra của cả file

### Đầu vào tổng thể

File test này không nhận input từ người dùng như chương trình thông thường. Đầu vào của nó là:

- Các class có sẵn trong dự án:
  - `RaycastWeapon`
  - `ActiveWeapon`
  - `AmmoConfigSO`
  - `StarterAssetsInputs`
  - `Animator`
- Runtime controller:
  - `Assets/_Game/Animations/Player/RigController.controller`
- Các tham số setup trong helper:
  - số đạn trong băng
  - sức chứa băng
  - đạn dự trữ
  - trạng thái active/inactive

### Đầu ra tổng thể

Đầu ra của file test là kết quả pass/fail trong Unity Test Runner.

Mỗi test không trả về dữ liệu để nơi khác dùng, mà dùng `Assert` để xác minh:

- đúng trạng thái thì test pass
- sai trạng thái thì test fail

---

## 11. Tại sao file này dùng nhiều helper thay vì viết thẳng trong từng test

Lý do chính:

- Tránh lặp code.
- Mỗi test chỉ tập trung vào ý nghĩa nghiệp vụ.
- Setup trở nên nhất quán.
- Dễ mở rộng thêm test mới sau này.

Ví dụ:

- Nếu sau này `RaycastWeapon` cần thêm một field private bắt buộc, chỉ cần sửa `CreateConfiguredWeapon()` một lần là mọi test dùng chung đều được cập nhật.

---

## 12. Một số điểm kỹ thuật quan trọng

### Vì sao đây là EditMode test chứ không phải PlayMode test

Vì các case này chủ yếu kiểm tra logic:

- có được bắn hay không
- reload có nạp đạn hay không
- holster có đổi state hay không

Chúng không cần camera thật, input system thật, scene thật hoặc frame gameplay đầy đủ.

### Vì sao dùng Reflection

Vì code production có nhiều phần không public:

- `Start()`
- `ShootControl()`
- field private như `bullets`, `muzzleFlash`, `raycastOrigin`, `tracerEffect`

Thay vì sửa code production chỉ để dễ test, test dùng reflection để truy cập có kiểm soát.

### Vì sao case holster không test transform sang back socket

Vì yêu cầu của bài là:

- state chuyển đúng

Nên test hiện tại tập trung vào:

- `IsHolstered`
- animator bool `holster_weapon`

Nếu muốn mở rộng, có thể thêm test cho `OnWeaponHolster()` để kiểm tra weapon parent đổi sang `backSocket` hoặc `hipSocket`.

---

## 13. Tóm tắt ngắn từng hàm

### Hàm setup

- `TearDown()`
  Dọn tất cả object sau mỗi test.

- `CreateTestObject(name)`
  Tạo `GameObject` test và đăng ký để dọn.

- `CreateTestAsset<T>()`
  Tạo `ScriptableObject` test và đăng ký để dọn.

- `InvokeNonPublic(instance, methodName)`
  Gọi method private bằng reflection.

- `SetPrivateField(instance, fieldName, value)`
  Gán field private bằng reflection.

- `GetBullets(weapon)`
  Đọc list bullet private từ `RaycastWeapon`.

- `CreateAmmoConfig(...)`
  Tạo cấu hình đạn theo số lượng mong muốn.

- `CreateConfiguredWeapon(...)`
  Tạo súng đã cấu hình đủ để test.

- `CreateConfiguredActiveWeapon()`
  Tạo player tối thiểu để test holster.

### Hàm test

- `ClickShoot_ShouldSpawnBullet()`
  Test bắn tạo bullet.

- `Reload_ShouldRefillAmmo()`
  Test reload nạp lại đạn.

- `Holster_ShouldUpdateStateCorrectly()`
  Test cất súng đổi state đúng.

- `AmmoZero_ShouldNotShoot()`
  Test hết đạn thì không bắn.

- `Reloading_ShouldNotShoot()`
  Test đang reload thì không bắn.

---

## 14. Nếu bạn muốn giải thích sâu hơn nữa

Có thể tách tiếp thành các tài liệu riêng:

1. Giải thích riêng từng dòng code trong file test.
2. Vẽ luồng chạy giữa `ActiveWeapon` và `RaycastWeapon`.
3. So sánh EditMode test này với cách viết PlayMode test tương đương.
4. Phân tích vì sao `UpdateFiring(0f)` vẫn bắn ra 1 viên trong implementation hiện tại.
