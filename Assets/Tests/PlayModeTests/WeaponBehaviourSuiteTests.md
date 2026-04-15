# WeaponBehaviourSuiteTests.cs

Tai lieu nay giai thich file [WeaponBehaviourSuiteTests.cs](D:/File/study/LAP_TRINH_GAME_3D_NANG_CAO/ROPE/Assets/Tests/PlayModeTests/WeaponBehaviourSuiteTests.cs): y nghia cua tung ham, dau vao, dau ra, logic xu ly, ly do can viet nhu vay, va cac doi so string duoc dung de tham chieu vao field/method private bang reflection.

## 1. Muc dich cua file

`WeaponBehaviourSuiteTests` la bo `PlayMode Test` dung de kiem tra cac hanh vi co ban va quan trong cua `RaycastWeapon`.

Hien tai suite nay tap trung vao 5 hanh vi:

- Ban mot phat co sinh bullet/tracer hay khong.
- Reload co nap day bang dan dung khong.
- Khi dang ban ma bat dau reload thi sung co dung ban ngay khong.
- Khi het dan thi co bi chan ban khong.
- Khi dang reload thi co bi chan ban khong.

File nay khong test UI, animation hay input system phuc tap. Muc tieu cua no la test logic runtime co ban cua vu khi trong PlayMode.

## 2. Tai sao file duoc viet theo cach nay

### 2.1. Vi day la PlayMode Test

PlayMode Test chay trong moi truong Unity runtime, nen viec test thuong can:

- tao `GameObject`
- gan `Component`
- tao `ScriptableObject`
- cho Unity chay them frame bang `yield return null`

Do do file phai tu dung len mot moi truong test nho gon bang code.

### 2.2. Vi `RaycastWeapon` co field private

`RaycastWeapon` co mot so field private ma test can thiet lap hoac doc ra, vi du:

- `"muzzleFlash"`
- `"raycastOrigin"`
- `"tracerEffect"`
- `"bullets"`

Vi chung la private, test dung reflection thong qua cac helper:

- `InvokeNonPublic`
- `SetPrivateField`
- `GetBullets`

### 2.3. Vi moi test can mot cau hinh sung rieng

Moi test co the can so dan khac nhau:

- con dan
- het dan
- dang reload
- bang dang thieu dan

Thay vi viet lai setup o tung test, file dung `CreateAmmoConfig(...)` va `CreateConfiguredWeapon(...)` de tao fixture co the tai su dung.

## 3. Cau truc tong quat cua class

Class gom 3 nhom chinh:

1. Quan ly object duoc tao trong test
2. Helper setup va helper reflection
3. Cac test case

## 4. Phan tich tung thanh phan

## 4.1. `m_TestObjects`

```csharp
readonly List<Object> m_TestObjects = new List<Object>();
```

### Y nghia

Luu toan bo `UnityEngine.Object` duoc tao trong qua trinh test.

### Dau vao

Khong co tham so truc tiep. Danh sach nay duoc bo sung thong qua:

- `CreateTestObject`
- `CreateTestAsset`

### Dau ra

Khong co gia tri tra ve. Day la noi luu tam de `TearDown()` biet can huy doi tuong nao.

### Tai sao can

Neu khong cleanup sau moi test:

- object cu co the con ton tai sang test sau
- state test truoc co the anh huong test sau
- ket qua test de bi sai

## 4.2. `TearDown()`

```csharp
[TearDown]
public void TearDown()
```

### Y nghia

Ham cleanup chay sau moi test.

### Dau vao

Khong co tham so.

### Dau ra

Khong tra ve gia tri.

### Logic

Ham chay theo 2 buoc:

1. Duyet `m_TestObjects`
   - neu object la `RaycastWeapon`
   - lay danh sach `bullets` private
   - tu tung bullet, lay field public `tracer`
   - neu co `TrailRenderer` thi huy game object cua tracer

2. Duyet lai `m_TestObjects`
   - huy tung object neu khac `null`

Sau do xoa danh sach `m_TestObjects`.

### Tai sao viet nhu vay

`RaycastWeapon` co the sinh ra tracer runtime ma khong nam truc tiep trong danh sach object ban dau. Neu chi huy object goc, tracer co the van con trong scene test.

### Tai sao can ham nay

Dam bao moi test doc lap va sach moi truong.

## 4.3. `CreateTestObject(string name)`

```csharp
GameObject CreateTestObject(string name)
```

### Y nghia

Tao nhanh mot `GameObject` test va tu dong dua vao danh sach cleanup.

### Dau vao

- `name`: ten object

### Dau ra

- `GameObject` vua duoc tao

### Logic

1. Tao `new GameObject(name)`
2. Them vao `m_TestObjects`
3. Tra ve object

### Tai sao can

Tranh lap lai code tao object va tranh quen dang ky object de cleanup.

## 4.4. `CreateTestAsset<T>()`

```csharp
T CreateTestAsset<T>() where T : ScriptableObject
```

### Y nghia

Tao nhanh mot `ScriptableObject` phuc vu test va tu dong dua vao danh sach cleanup.

### Dau vao

- `T`: kieu `ScriptableObject` can tao

### Dau ra

- instance kieu `T`

### Logic

1. Goi `ScriptableObject.CreateInstance<T>()`
2. Them vao `m_TestObjects`
3. Tra ve asset

### Tai sao can

Test `RaycastWeapon` can `AmmoConfigSO` nhung khong can tao asset that trong project.

## 4.5. `InvokeNonPublic(object instance, string methodName)`

```csharp
static void InvokeNonPublic(object instance, string methodName)
```

### Y nghia

Goi mot method private hoac non-public bang reflection.

### Dau vao

- `instance`: object can goi method
- `methodName`: ten method can goi

### Dau ra

Khong tra ve gia tri.

### Logic

1. Tim method bang `GetMethod(...)` voi:
   - `BindingFlags.Instance`
   - `BindingFlags.NonPublic`
2. Assert method phai ton tai
3. Goi `method.Invoke(instance, null)`

### Tai sao can

Trong suite hien tai, helper nay duoc dung de goi `AmmoConfigSO.OnEnable()` nham khoi tao dung state noi bo cua `ScriptableObject`.

### Doi so tham chieu la gi

- `instance`: doi tuong that dang chua method
- `methodName`: chuoi ten method chinh xac, vi du `"OnEnable"`

Neu ghi sai ten, test se fail ngay tai `Assert.IsNotNull`.

## 4.6. `SetPrivateField(object instance, string fieldName, object value)`

```csharp
static void SetPrivateField(object instance, string fieldName, object value)
```

### Y nghia

Gan gia tri cho mot field private bang reflection.

### Dau vao

- `instance`: object chua field private
- `fieldName`: ten field can gan
- `value`: gia tri can set

### Dau ra

Khong tra ve gia tri.

### Logic

1. Tim field bang `GetField(...)` voi:
   - `BindingFlags.Instance`
   - `BindingFlags.NonPublic`
2. Assert field phai ton tai
3. Goi `field.SetValue(instance, value)`

### Tai sao can

`RaycastWeapon` can mot so dependency private de chay on dinh trong test, nhu:

- `"muzzleFlash"`
- `"raycastOrigin"`
- `"tracerEffect"`

Neu khong gan cac field nay, viec fire bullet co the khong chay dung.

### Doi so tham chieu la gi

- `instance`: thuong la `RaycastWeapon`
- `fieldName`: ten chinh xac cua field private
- `value`: gia tri phu hop voi kieu field

Vi du:

- `SetPrivateField(weapon, "raycastOrigin", origin);`
- `SetPrivateField(weapon, "tracerEffect", tracer);`
- `SetPrivateField(weapon, "muzzleFlash", new ParticleSystem[0]);`

## 4.7. `GetBullets(RaycastWeapon weapon)`

```csharp
static IList GetBullets(RaycastWeapon weapon)
```

### Y nghia

Doc danh sach `bullets` private ben trong `RaycastWeapon`.

### Dau vao

- `weapon`: sung can kiem tra

### Dau ra

- `IList`: danh sach bullet/tracer dang duoc weapon quan ly

### Logic

1. Tim field private `"bullets"`
2. Assert field ton tai
3. Lay gia tri field
4. Cast sang `IList`

### Tai sao can

Test can xac nhan sau khi ban thi sung da tao bullet/tracer hay chua, va `TearDown()` cung can danh sach nay de cleanup tracer.

### Doi so tham chieu la gi

- dau vao la `weapon`
- field noi bo duoc tham chieu bang chuoi `"bullets"`

## 4.8. `CreateAmmoConfig(int clipAmmo = 30, int clipSize = 30, int reserveAmmo = 90)`

```csharp
AmmoConfigSO CreateAmmoConfig(int clipAmmo = 30, int clipSize = 30, int reserveAmmo = 90)
```

### Y nghia

Tao `AmmoConfigSO` phuc vu test voi so dan tuy chinh.

### Dau vao

- `clipAmmo`: so dan hien co trong bang
- `clipSize`: suc chua toi da cua bang
- `reserveAmmo`: so dan du tru

### Dau ra

- `AmmoConfigSO` da duoc cau hinh

### Logic

1. Tao `AmmoConfigSO`
2. Goi `OnEnable()` bang reflection
3. Gan:
   - `clipSize`
   - `currentClipAmmo`
   - `currentAmmo`
4. Tra ve asset

### Tai sao viet nhu vay

Mot so `ScriptableObject` can qua trinh `OnEnable()` de khoi tao noi bo dung cach. Goi no trong test giup trang thai asset giong runtime that hon.

### Tai sao can

Hau het cac test trong file deu can du lieu dan, nen ham nay giup gom setup vao mot cho.

### Y nghia tung doi so

- `clipAmmo`: dung de test con dan hay het dan
- `clipSize`: dung de test logic reload
- `reserveAmmo`: dung de test luong dan du tru sau khi nap

## 4.9. `CreateConfiguredWeapon(...)`

```csharp
RaycastWeapon CreateConfiguredWeapon(
    string name = "Pistol",
    int clipAmmo = 30,
    int clipSize = 30,
    int reserveAmmo = 90,
    bool keepInactive = true)
```

### Y nghia

Tao mot `RaycastWeapon` da duoc cau hinh toi thieu de co the test duoc.

### Dau vao

- `name`: ten object vu khi
- `clipAmmo`: so dan trong bang
- `clipSize`: kich thuoc bang dan
- `reserveAmmo`: so dan du tru
- `keepInactive`: co giu object o trang thai inactive hay khong

### Dau ra

- `RaycastWeapon` da san sang cho test

### Logic chi tiet

1. Tao `GameObject` cho weapon
2. Bat/tat active theo `keepInactive`
3. Add `RaycastWeapon`
4. Gan:
   - `weaponName`
   - `ammoConfig`
   - `fireRate`
5. Tao `origin` va `destination`
6. Dat:
   - `origin.position = Vector3.zero`
   - `destination.position = Vector3.forward * 10f`
7. Tao tracer prefab gia
8. Gan cac field private:
   - `"muzzleFlash"`
   - `"raycastOrigin"`
   - `"tracerEffect"`
9. Gan `weapon.raycastDestination`
10. Tra ve `weapon`

### Tai sao viet nhu vay

`RaycastWeapon` muon chay trong test khong chi can moi component. No can:

- du lieu dan
- diem bat dau raycast
- diem dich de tinh huong ban
- tracer de tao bullet visual

Ham nay tao ra mot cau hinh toi thieu nhung hop le.

### Tai sao can

Day la ham setup trung tam cua ca suite. Nho no, tung test case ngan gon hon va chi tap trung vao hanh vi can kiem tra.

### Y nghia tung doi so

- `name`: de phan biet object
- `clipAmmo`: dieu kien co ban duoc hay khong
- `clipSize`: co so de tinh nap dan
- `reserveAmmo`: so dan con lai sau reload
- `keepInactive`: tranh mot so vong doi Unity chay som truoc khi setup xong

## 5. Phan tich cac test case

## 5.1. `ClickShoot_ShouldSpawnBullet()`

### Chuc nang duoc test

Kiem tra hanh vi ban co ban cua sung.

### Dau vao

- weapon mac dinh tu `CreateConfiguredWeapon()`

### Hanh dong

1. Goi `weapon.StartFiring()`
2. Goi `weapon.UpdateFiring(0f)`
3. `yield return null`

### Dau ra mong doi

- `weapon.isFiring == true`
- `GetBullets(weapon).Count == 1`
- `weapon.ammoConfig.currentClipAmmo == 29`

### Tai sao can test nay

Day la test quan trong nhat cua hanh vi ban: vao trang thai firing, sinh bullet, va tru dung 1 vien.

## 5.2. `Reload_ShouldRefillAmmo()`

### Chuc nang duoc test

Kiem tra chuc nang nap dan.

### Dau vao

- `clipAmmo = 5`
- `clipSize = 30`
- `reserveAmmo = 50`

### Hanh dong

1. Goi `weapon.StartReload()`
2. Goi `weapon.RefillAmmo()`
3. `yield return null`

### Dau ra mong doi

- `weapon.isReloading == false`
- `currentClipAmmo == 30`
- `currentAmmo == 25`

### Logic tinh toan

Bang dang co 5 vien, can them 25 vien de day 30.
Kho du tru co 50 vien, nap 25 vien xong thi con 25 vien.

## 5.3. `StartReload_ShouldStopFiringAndEnterReloadState()`

### Chuc nang duoc test

Kiem tra chuyen trang thai tu `firing` sang `reloading`.

### Dau vao

- weapon co du dan:
  - `clipAmmo = 30`
  - `clipSize = 30`
  - `reserveAmmo = 90`

### Hanh dong

1. Goi `weapon.StartFiring()`
2. Goi `weapon.UpdateFiring(0f)` de dam bao sung dang trong logic ban
3. Goi `weapon.StartReload()`
4. `yield return null`

### Dau ra mong doi

- `weapon.isReloading == true`
- `weapon.isFiring == false`

### Tai sao can test nay

Test nay xac nhan khi nguoi choi bat dau nap dan thi sung phai:

- dung ban ngay
- vao dung state reload

Day la mot test on dinh hon so voi test holster cu vi no khong phu thuoc vao:

- `Animator`
- `AnimationEvent`
- `ActiveWeapon`

## 5.4. `AmmoZero_ShouldNotShoot()`

### Chuc nang duoc test

Kiem tra sung khong duoc ban khi bang dan bang 0.

### Dau vao

- `clipAmmo = 0`
- `clipSize = 30`
- `reserveAmmo = 90`

### Hanh dong

1. Goi `weapon.StartFiring()`
2. `yield return null`

### Dau ra mong doi

- `weapon.isFiring == false`
- `GetBullets(weapon).Count == 0`

### Tai sao can test nay

Dam bao game khong cho phep vao trang thai firing khi het dan.

## 5.5. `Reloading_ShouldNotShoot()`

### Chuc nang duoc test

Kiem tra sung khong duoc ban neu dang trong qua trinh reload.

### Dau vao

- weapon binh thuong
- `weapon.isReloading = true`

### Hanh dong

1. Goi `weapon.StartFiring()`
2. `yield return null`

### Dau ra mong doi

- `weapon.isFiring == false`
- `GetBullets(weapon).Count == 0`

### Tai sao can test nay

Dam bao state machine cua sung khong cho vua reload vua ban.

## 6. Cac chuoi string duoc dung de tham chieu

Trong file nay co nhung chuoi sau:

- `"OnEnable"`
- `"muzzleFlash"`
- `"raycastOrigin"`
- `"tracerEffect"`
- `"bullets"`

### Vai tro cua chung

Day la ten chinh xac cua method hoac field private ben trong class can truy cap bang reflection.

### Tai sao phai dung string

Reflection cua C# tim member theo ten o runtime. Vi cac member nay khong public nen test khong goi truc tiep duoc.

### Rui ro

Neu code production doi ten field/method ma test khong cap nhat theo, test se fail.

## 7. Dau vao va dau ra tong the cua file

## Dau vao tong the

Cac test trong file nhan dau vao tu:

- thong so dan:
  - `clipAmmo`
  - `clipSize`
  - `reserveAmmo`
- cau hinh object weapon
- cac dependency runtime can thiet cho `RaycastWeapon`

## Dau ra tong the

Dau ra sau cung la cac assertion de xac nhan:

- state cua sung
- so bullet/tracer duoc tao
- so dan con lai
- kha nang ban hoac bi chan ban trong tung tinh huong

Noi cach khac, dau ra thuc te cua file la ket qua pass/fail cua tung test case.

## 8. Tai sao khong con test holster

Truoc day suite co mot test holster lien quan den `ActiveWeapon`, `Animator` va `AnimationEvent`.

Test do da bi loai bo vi:

- phu thuoc vao animation controller that trong project
- co the fail do `AnimationEvent` thay vi fail do logic sung
- fixture phuc tap hon muc can thiet cua suite nay

Sau khi bo test nay, suite tro nen:

- gon hon
- de doc hon
- de bao tri hon
- on dinh hon trong PlayMode Test

## 9. Ket luan

`WeaponBehaviourSuiteTests.cs` la bo PlayMode Test tap trung vao logic cot loi cua `RaycastWeapon`.

Cac ham helper ton tai de:

- dung object test nhanh hon
- tranh lap code setup
- cau hinh field private can thiet
- doc du lieu private de assert va cleanup

Cac test case hien tai uu tien:

- de hieu
- de chay on dinh
- it phu thuoc vao he thong ben ngoai nhu animation hay input phuc tap

Neu can, buoc tiep theo hop ly la toi co the cap nhat them tai lieu nay theo kieu bang tong hop:

- ten test
- ham duoc test
- input
- output
- y nghia nghiep vu
