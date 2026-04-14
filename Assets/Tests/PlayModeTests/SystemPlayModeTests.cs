using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DatScript;

namespace Tests.PlayModeTests
{
    public class SystemPlayModeTests
    {
        private GameObject m_GameManagerGO;
        private GameManager m_GameManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Khởi tạo GameManager tối giản
            m_GameManagerGO = new GameObject("GameManager");
            m_GameManager = m_GameManagerGO.AddComponent<GameManager>();
            GameManager.instance = m_GameManager;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_GameManagerGO != null) Object.Destroy(m_GameManagerGO);
            yield return null;
        }

        // --- TEST 1: KIỂM TRA GAMEMANAGER ĐÃ TỒN TẠI ---
        [UnityTest]
        public IEnumerator Test1_GameManager_InstanceExists()
        {
            Assert.IsNotNull(GameManager.instance, "GameManager instance phải tồn tại.");
            yield return null;
        }

        // --- TEST 2: KIỂM TRA LƯU VỊ TRÍ CHECKPOINT ---
        [UnityTest]
        public IEnumerator Test2_GameManager_SetCheckpoint_StoresCorrectValue()
        {
            Vector3 testPos = new Vector3(123f, 456f, 789f);
            m_GameManager.SetCheckpoint(testPos);

            // Dùng Reflection để lấy giá trị private currentRespawnPosition
            var field = typeof(GameManager).GetField("currentRespawnPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 savedPos = (Vector3)field.GetValue(m_GameManager);

            Assert.AreEqual(testPos, savedPos, "Vị trí checkpoint lưu lại không đúng.");
            yield return null;
        }

        // --- TEST 3: KIỂM TRA DEFAULT SPAWN POINT ---
        [UnityTest]
        public IEnumerator Test3_GameManager_DefaultSpawnPoint_IsAssigned()
        {
            GameObject spawnGO = new GameObject("SpawnPoint");
            spawnGO.transform.position = new Vector3(10, 20, 30);
            m_GameManager.defaultSpawnPoint = spawnGO.transform;

            // Gọi lại Start thông qua Reflection để cập nhật logic ban đầu
            MethodInfo startMethod = typeof(GameManager).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod.Invoke(m_GameManager, null);

            var field = typeof(GameManager).GetField("currentRespawnPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 currentPos = (Vector3)field.GetValue(m_GameManager);

            Assert.AreEqual(spawnGO.transform.position, currentPos, "Vị trí hồi sinh ban đầu phải là defaultSpawnPoint.");
            
            Object.Destroy(spawnGO);
            yield return null;
        }

        // --- TEST 4: KIỂM TRA KÍCH HOẠT CHECKPOINT (TRIGGER) ---
        [UnityTest]
        public IEnumerator Test4_Checkpoint_Trigger_UpdatesGameManager()
        {
            // Tạo một checkpoint mới
            GameObject cpGO = new GameObject("TestCP");
            cpGO.transform.position = new Vector3(50, 50, 50);
            Checkpoint cp = cpGO.AddComponent<Checkpoint>();
            BoxCollider col = cpGO.AddComponent<BoxCollider>();
            col.isTrigger = true;

            // Tạo một Player giả để kích hoạt trigger
            GameObject playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.AddComponent<BoxCollider>();
            playerGO.AddComponent<Rigidbody>().isKinematic = true;

            // Giả lập va chạm bằng cách gọi trực tiếp OnTriggerEnter
            MethodInfo triggerMethod = typeof(Checkpoint).GetMethod("OnTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            triggerMethod.Invoke(cp, new object[] { playerGO.GetComponent<Collider>() });

            // Kiểm tra GameManager đã nhận vị trí mới chưa
            var field = typeof(GameManager).GetField("currentRespawnPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 savedPos = (Vector3)field.GetValue(m_GameManager);

            Assert.AreEqual(cpGO.transform.position, savedPos, "Checkpoint trigger không cập nhật GameManager.");

            Object.Destroy(cpGO);
            Object.Destroy(playerGO);
            yield return null;
        }

        // --- TEST 5: KIỂM TRA PANEL GAME OVER ---
        [UnityTest]
        public IEnumerator Test5_GameManager_GameOverPanel_Assignment()
        {
            GameObject panel = new GameObject("GameOverUI");
            m_GameManager.gameOverPanel = panel;
            
            Assert.AreEqual(panel, m_GameManager.gameOverPanel, "GameOverPanel phải được gán thành công.");
            
            Object.Destroy(panel);
            yield return null;
        }
    }
}