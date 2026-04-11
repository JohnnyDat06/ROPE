using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using DatScript; // Namespace cho Checkpoint và GameManager

namespace Tests.GeneralTests
{
    public class GeneralTests
    {
        // --- TEST CASE 1: KIỂM TRA DỮ LIỆU VẬT PHẨM ---
        [Test]
        public void Test1_ItemData_DefaultDuration_IsValid()
        {
            // CHUẨN BỊ & THỰC HIỆN: Tạo một bản thể của ItemData ScriptableObject
            ItemData testData = ScriptableObject.CreateInstance<ItemData>();
            
            // KIỂM TRA: Xác nhận thời gian nhặt mặc định không được âm
            // Theo ItemData.cs, giá trị mặc định là 1.0f
            Assert.GreaterOrEqual(testData.pickupDuration, 0f, "Thời gian nhặt vật phẩm không được là số âm.");
            
            // Dọn dẹp bộ nhớ
            Object.DestroyImmediate(testData);
        }

        // --- TEST CASE 2: TRẠNG THÁI KHỞI TẠO CỦA CHECKPOINT ---
        [Test]
        public void Test2_Checkpoint_InitialState_IsDeactivated()
        {
            // CHUẨN BỊ: Tạo một GameObject và thêm thành phần Checkpoint
            GameObject go = new GameObject("TestCheckpoint");
            Checkpoint cp = go.AddComponent<Checkpoint>();

            // THỰC HIỆN: Sử dụng Reflection để lấy trường dữ liệu private 'isActivated'
            // Trong file Checkpoint.cs, isActivated là một biến bool private
            FieldInfo field = typeof(Checkpoint).GetField("isActivated", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            bool initialState = (bool)field.GetValue(cp);

            // KIỂM TRA: Xác nhận Checkpoint mặc định ban đầu phải là CHƯA được kích hoạt
            Assert.IsFalse(initialState, "Checkpoint không được phép tự kích hoạt khi vừa tạo mới.");

            // Dọn dẹp
            Object.DestroyImmediate(go);
        }

        // --- TEST CASE 3: KIỂM TRA ENUM LOẠI VẬT PHẨM ---
        [Test]
        public void Test3_ItemType_Enum_HasRequiredValues()
        {
            // THỰC HIỆN & KIỂM TRA: Xác nhận các giá trị enum cụ thể tồn tại cho các vật phẩm cốt lõi của game
            // Việc này đảm bảo các loại vật phẩm quan trọng không vô tình bị xóa khỏi file GameEnums.cs
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Small"), "Loại Small phải tồn tại trong ItemType.");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Large"), "Loại Large phải tồn tại trong ItemType.");
            Assert.IsTrue(System.Enum.IsDefined(typeof(ItemType), "Special"), "Loại Special phải tồn tại trong ItemType.");
        }

        // --- TEST CASE 4: KHỞI TẠO GIÁ TRỊ VẬT PHẨM ---
        [Test]
        public void Test4_ItemController_InitializeValue_SetsCorrectValueRange()
        {
            // CHUẨN BỊ: Tạo vật phẩm (Item) và gán dữ liệu mẫu
            GameObject go = new GameObject("TestItem");
            ItemController item = go.AddComponent<ItemController>();
            ItemData data = ScriptableObject.CreateInstance<ItemData>();
            
            // Thiết lập loại vật phẩm là Small (Khoảng giá trị: 10 - 30 theo logic trong ItemController.cs)
            data.itemType = ItemType.Small;
            item.data = data;

            // THỰC HIỆN: Gọi hàm khởi tạo giá trị ngẫu nhiên
            item.InitializeValue();

            // KIỂM TRA: Xác nhận giá trị scrapValue có nằm trong khoảng từ 10 đến 30 hay không
            Assert.GreaterOrEqual(item.scrapValue, 10, "Giá trị vật phẩm loại Small không được nhỏ hơn 10.");
            Assert.LessOrEqual(item.scrapValue, 30, "Giá trị vật phẩm loại Small không được lớn hơn 30.");

            // Dọn dẹp
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(go);
        }

        // --- TEST CASE 5: CẬP NHẬT VỊ TRÍ CHECKPOINT TRONG GAMEMANAGER ---
        [Test]
        public void Test5_GameManager_SetCheckpoint_UpdatesPosition()
        {
            // CHUẨN BỊ: Tạo GameManager trong môi trường kiểm thử
            GameObject go = new GameObject("GameManager");
            GameManager manager = go.AddComponent<GameManager>();
            Vector3 testCheckpointPos = new Vector3(100f, 50f, 200f);

            // THỰC HIỆN: Thiết lập điểm hồi sinh (Checkpoint) mới
            manager.SetCheckpoint(testCheckpointPos);

            // Truy cập biến private currentRespawnPosition bằng kỹ thuật Reflection
            FieldInfo field = typeof(GameManager).GetField("currentRespawnPosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(field, "Không tìm thấy trường 'currentRespawnPosition' trong script GameManager.");
            Vector3 actualPos = (Vector3)field.GetValue(manager);

            // KIỂM TRA: Xác nhận vị trí hồi sinh thực tế đã được cập nhật đúng với vị trí truyền vào
            Assert.AreEqual(testCheckpointPos, actualPos, "Vị trí hồi sinh trong GameManager chưa được cập nhật đúng.");

            // Dọn dẹp
            Object.DestroyImmediate(go);
        }
    }
}