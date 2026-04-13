using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StarterAssets;

namespace ROPE.Tests
{
    public class PlayerMovementTest
    {
        // ── Shared state ──────────────────────────────────────────────
        readonly System.Collections.Generic.List<Object> m_TestObjects =
            new System.Collections.Generic.List<Object>();

        // ── Lifecycle ─────────────────────────────────────────────────
        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in m_TestObjects)
                if (obj != null) Object.DestroyImmediate(obj);
            m_TestObjects.Clear();
        }

        // ── Helper ────────────────────────────────────────────────────
        GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            m_TestObjects.Add(obj);
            return obj;
        }

        // ── Tests ─────────────────────────────────────────────────────

        // Kiểm tra ThirdPersonController: Kiểm tra các giá trị mặc định khi khởi tạo component, 
        // tắt GameObject trước khi AddComponent, xác nhận MoveSpeed, SprintSpeed, JumpHeight và Gravity.
        [Test]
        public void TestDefaultMovementValues()
        {
            // 1. Create GameObject, disable before adding components
            var go = CreateTestObject("Player");
            go.SetActive(false); // prevent Awake/Start side-effects
            var comp = go.AddComponent<ThirdPersonController>();

            // 2. Assert initial state
            Assert.AreEqual(2.0f, comp.MoveSpeed, "MoveSpeed mặc định phải là 2.0f");
            Assert.AreEqual(5.335f, comp.SprintSpeed, "SprintSpeed mặc định phải là 5.335f");
            Assert.AreEqual(1.2f, comp.JumpHeight, "JumpHeight mặc định phải là 1.2f");
            Assert.AreEqual(-15.0f, comp.Gravity, "Gravity mặc định phải là -15.0f");
            Assert.IsTrue(comp.Grounded, "Grounded mặc định phải là true");
        }

        // Kiểm tra ThirdPersonController: Thiết lập độ trễ chuột con quay hồi chuyển,
        // gọi hàm SetSensitivity và xác nhận field Sensitivity được cập nhật đúng giá trị.
        [Test]
        public void TestSetSensitivity()
        {
            var go = CreateTestObject("Player");
            go.SetActive(false);
            var comp = go.AddComponent<ThirdPersonController>();

            comp.SetSensitivity(3.5f);

            Assert.AreEqual(3.5f, comp.Sensitivity, "Sensitivity phải được cập nhật thành 3.5f");
        }

        // Kiểm tra ThirdPersonController: Truy cập private field bằng Reflection, 
        // lấy giá trị của _terminalVelocity và xác nhận giá trị đúng kịch bản thiết kế.
        [Test]
        public void TestPrivateTerminalVelocityUsingReflection()
        {
            var go = CreateTestObject("Player");
            go.SetActive(false);
            var comp = go.AddComponent<ThirdPersonController>();

            var field = typeof(ThirdPersonController).GetField("_terminalVelocity", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(field, "Field _terminalVelocity phải tồn tại");
            var value = (float)field.GetValue(comp);
            
            Assert.AreEqual(53.0f, value, "_terminalVelocity mặc định phải là 53.0f");
        }

        // Kiểm tra ThirdPersonController: Xác nhận logic AddRecoil thay đổi giá trị Camera bằng Reflection,
        // gọi hàm giả định có dao động súng và truy xuất _cinemachineTargetPitch để xem thay đổi.
        [Test]
        public void TestAddRecoilUpdatesPitchAndYaw()
        {
            var go = CreateTestObject("Player");
            go.SetActive(false);
            var comp = go.AddComponent<ThirdPersonController>();

            var pitchField = typeof(ThirdPersonController).GetField("_cinemachineTargetPitch", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(pitchField, "Field _cinemachineTargetPitch phải tồn tại");

            // Setup giá trị khởi tạo
            pitchField.SetValue(comp, 0f);

            // Giả lập lực giật dọc 2.0f, lực giật ngang 0f (để triệt tiêu random)
            comp.AddRecoil(2.0f, 0.0f);

            var newPitch = (float)pitchField.GetValue(comp);
            
            // Xác nhận bị giật camera (pitch giảm do recoil)
            Assert.AreEqual(-2.0f, newPitch, "Pitch phải giảm 2.0f do tác động của Recoil");
        }
    }
}
