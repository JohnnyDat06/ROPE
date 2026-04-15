using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Behavior;
using System.Reflection;

namespace Tests.PlayModeTests
{
    /// <summary>
    /// Các bài kiểm tra Play Mode chuyên nghiệp cho component VisionSensor.
    /// Đã được tinh chỉnh để đảm bảo độ chính xác vật lý và AI.
    /// </summary>
    public class VisionSensorTest
    {
        private readonly List<GameObject> m_TestObjects = new List<GameObject>();
        
        private VisionSensor m_Sensor;
        private BehaviorGraphAgent m_BehaviorAgent;
        private GameObject m_SensorGo;
        private GameObject m_TargetGo;

        private GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        private bool GetDetectedState()
        {
            if (m_Sensor == null) return false;
            // Đọc trực tiếp biến private _canSeePlayer để bỏ qua phụ thuộc vào Behavior Graph/Blackboard trong Unit Test
            var field = typeof(VisionSensor).GetField("_canSeePlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)(field?.GetValue(m_Sensor) ?? false);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 1. Tạo Target (Người chơi) TRƯỚC để Sensor có thể tìm thấy
            m_TargetGo = CreateTestObject("Target_Player");
            m_TargetGo.tag = "Player";
            m_TargetGo.layer = LayerMask.NameToLayer("Player");
            
            // Thêm Collider và giả lập kích thước thực tế (1.8m chiều cao)
            var col = m_TargetGo.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.center = new Vector3(0, 1f, 0);

            // 2. Thiết lập Sensor (Kẻ địch)
            m_SensorGo = CreateTestObject("Sensor_Enemy");
            // Đặt Layer của Kẻ địch khác với Layer vật cản để tránh tự va chạm
            m_SensorGo.layer = LayerMask.NameToLayer("Ignore Raycast");
            
            // Chú ý: Ta vẫn thêm BehaviorGraphAgent nhưng test sẽ đọc trực tiếp từ Sensor
            m_BehaviorAgent = m_SensorGo.AddComponent<BehaviorGraphAgent>();
            m_Sensor = m_SensorGo.AddComponent<VisionSensor>();
            
            // Cấu hình thông số tầm nhìn
            m_Sensor.viewRadius = 15f;
            m_Sensor.viewAngle = 110f;
            m_Sensor.detectionHoldTime = 0f;
            m_Sensor.targetMask = LayerMask.GetMask("Player");
            m_Sensor.obstacleMask = LayerMask.GetMask("Default");

            yield return null; // Chờ Start() chạy

            // Đảm bảo Sensor đã nhận diện được Collider của Player qua Reflection
            var fieldTarget = typeof(VisionSensor).GetField("_playerTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldCollider = typeof(VisionSensor).GetField("_playerCollider", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldMask = typeof(VisionSensor).GetField("_combinedMask", BindingFlags.NonPublic | BindingFlags.Instance);

            fieldTarget?.SetValue(m_Sensor, m_TargetGo.transform);
            fieldCollider?.SetValue(m_Sensor, col);
            fieldMask?.SetValue(m_Sensor, (int)(m_Sensor.obstacleMask | m_Sensor.targetMask));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var obj in m_TestObjects)
            {
                if (obj != null) Object.Destroy(obj);
            }
            m_TestObjects.Clear();
            yield return null;
        }

        [UnityTest]
        [Description("TC1: Phải phát hiện mục tiêu khi đứng ngay trước mặt và trong tầm nhìn.")]
        public IEnumerator Test_TargetVisible_WhenInFrontAndInRange()
        {
            // Đặt Sensor và Player ở độ cao 1m để tránh tia Raycast chạm sàn (y=0)
            m_SensorGo.transform.SetPositionAndRotation(new Vector3(0, 1f, 0), Quaternion.identity);
            m_TargetGo.transform.position = new Vector3(0, 1f, 5f);

            // Chờ vài frame để hệ thống vật lý và AI cập nhật
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f); 

            bool isDetected = GetDetectedState();
            Assert.IsTrue(isDetected, "Kẻ địch phải nhìn thấy Người chơi khi ở ngay trước mặt.");
        }

        [UnityTest]
        [Description("TC2: Không được phát hiện khi mục tiêu vượt quá khoảng cách viewRadius.")]
        public IEnumerator Test_TargetNotVisible_WhenOutOfRange()
        {
            m_SensorGo.transform.position = new Vector3(0, 1f, 0);
            m_TargetGo.transform.position = new Vector3(0, 1f, 20f); // Ngoài tầm 15m

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);

            bool isDetected = GetDetectedState();
            Assert.IsFalse(isDetected, "Kẻ địch không được nhìn thấy Người chơi khi vượt quá tầm nhìn.");
        }

        [UnityTest]
        [Description("TC3: Không được phát hiện khi mục tiêu đứng sau lưng (Ngoài FOV).")]
        public IEnumerator Test_TargetNotVisible_WhenBehindSensor()
        {
            m_SensorGo.transform.SetPositionAndRotation(new Vector3(0, 1f, 0), Quaternion.identity);
            m_TargetGo.transform.position = new Vector3(0, 1f, -5f); // Sau lưng

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);

            bool isDetected = GetDetectedState();
            Assert.IsFalse(isDetected, "Kẻ địch không được nhìn thấy Người chơi khi họ đứng sau lưng.");
        }

        [UnityTest]
        [Description("TC4: Không được phát hiện khi có vật cản che khuất tầm nhìn (Occlusion).")]
        public IEnumerator Test_TargetNotVisible_WhenObstructedByWall()
        {
            m_SensorGo.transform.position = new Vector3(0, 1f, 0);
            m_TargetGo.transform.position = new Vector3(0, 1f, 10f);

            // Tạo tường chắn ở giữa
            GameObject wall = CreateTestObject("Wall");
            wall.AddComponent<BoxCollider>();
            wall.transform.position = new Vector3(0, 1f, 5f);
            wall.transform.localScale = new Vector3(10, 10, 1);
            wall.layer = LayerMask.NameToLayer("Default");

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);

            bool isDetected = GetDetectedState();
            Assert.IsFalse(isDetected, "Kẻ địch không được nhìn thấy Người chơi xuyên qua tường.");
        }

        [UnityTest]
        [Description("TC5: Kiểm tra tính năng Detection Hold Time (Bộ nhớ).")]
        public IEnumerator Test_DetectionHoldTime_Persistence()
        {
            m_Sensor.detectionHoldTime = 1.0f;
            
            m_SensorGo.transform.position = new Vector3(0, 1f, 0);
            m_TargetGo.transform.position = new Vector3(0, 1f, 5f);
            
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(GetDetectedState(), "Phải nhìn thấy người chơi lúc đầu.");

            // Di chuyển ra xa (Ngoài tầm nhìn vật lý)
            m_TargetGo.transform.position = new Vector3(0, 1f, 100f);
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(GetDetectedState(), "Vẫn phải coi là 'Phát hiện' trong thời gian Hold Time.");
            
            yield return new WaitForSeconds(1.0f);
            Assert.IsFalse(GetDetectedState(), "Phải mất dấu sau khi hết thời gian chờ.");
        }

    }
}
