using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("--- Testing ---")]
    public bool overrideQuotaForTesting = false;
    public int testQuotaValue = 130;

    [Header("--- Spawn Settings ---")]
    [Tooltip("Số lượng đồ cơ bản TỐI THIỂU muốn xuất hiện (VD: 10)")]
    public int minItemsToSpawn = 10;

    [Tooltip("Số lượng đồ cơ bản TỐI ĐA muốn xuất hiện (VD: 12)")]
    public int maxItemsToSpawn = 12;

    [Tooltip("Số lượng đồ cộng thêm (Buffer dư ra) (VD: 4)")]
    public int bufferItemsCount = 4;

    [Range(0f, 1f)] public float specialItemChance = 0.3f;

    [Header("--- VỊ TRÍ ĐẶC BIỆT (FIRE ROOM) ---")]
    public Transform fireRoomSpawnPoint;

    [Header("--- TEMPLATES (Kéo đồ từ TEMPLATE ROOM vào đây) ---")]
    public List<ItemController> commonItemTemplates;
    public List<ItemController> specialItemTemplates;

    [Header("--- References ---")]
    public List<Transform> allSpawnPoints;
    public SellingZone sellingZone;

    public int totalMapValue;
    public int currentQuota;

    void Start()
    {
        SpawnLevelItems();
    }

    void SpawnLevelItems()
    {
        List<Transform> availableSpawns = new List<Transform>(allSpawnPoints);
        if (availableSpawns.Contains(fireRoomSpawnPoint))
        {
            availableSpawns.Remove(fireRoomSpawnPoint);
        }

        // --- CẬP NHẬT LOGIC TÍNH TỔNG SỐ LƯỢNG SPAWN ---
        // Tính tổng số đồ = Random(10 đến 12) + 4 đồ dư ra
        int baseSpawnCount = Random.Range(minItemsToSpawn, maxItemsToSpawn + 1);
        int totalSpawnCount = baseSpawnCount + bufferItemsCount;

        totalMapValue = 0;
        List<int> spawnedValues = new List<int>();

        // Cảnh báo nếu map không đủ vị trí đặt đồ
        if (totalSpawnCount > availableSpawns.Count)
        {
            Debug.LogWarning($"[LevelManager] Bạn muốn spawn {totalSpawnCount} món nhưng map chỉ có {availableSpawns.Count} vị trí (Spawn Points). Hệ thống sẽ chỉ spawn tối đa {availableSpawns.Count} món!");
            totalSpawnCount = availableSpawns.Count;
        }

        // ================================================================
        // BƯỚC 1: XỬ LÝ PHÒNG LỬA (Giữ nguyên)
        // ================================================================
        if (fireRoomSpawnPoint != null)
        {
            ItemController templateToSpawn = null;
            bool isSpecial = Random.value <= specialItemChance;

            if (isSpecial && specialItemTemplates.Count > 0)
            {
                templateToSpawn = specialItemTemplates[Random.Range(0, specialItemTemplates.Count)];
                Debug.Log("<color=cyan>FIRE ROOM: Spawned SPECIAL Item!</color>");
            }
            else if (commonItemTemplates.Count > 0)
            {
                templateToSpawn = commonItemTemplates[Random.Range(0, commonItemTemplates.Count)];
                Debug.Log("<color=orange>FIRE ROOM: Spawned COMMON Item (Fallback)</color>");
            }

            if (templateToSpawn != null)
            {
                int val = SpawnItemFromTemplate(templateToSpawn, fireRoomSpawnPoint);
                spawnedValues.Add(val);
                totalSpawnCount--; // Trừ đi 1 slot vì đã dành cho phòng lửa
            }
        }

        // ================================================================
        // BƯỚC 2: SPAWN CÁC MÓN CÒN LẠI BẰNG THUẬT TOÁN "TÚI ĐỒ" (CHỐNG TRÙNG LẶP)
        // ================================================================

        // Xáo trộn các vị trí trên map để đồ rớt rải rác
        ShuffleList(availableSpawns);

        // Tạo một cái "Túi" chứa các Item Templates
        List<ItemController> grabBag = new List<ItemController>(commonItemTemplates);
        ShuffleList(grabBag);

        for (int i = 0; i < totalSpawnCount; i++)
        {
            if (i >= availableSpawns.Count) break;

            if (commonItemTemplates.Count > 0)
            {
                // Nếu rút hết đồ trong túi, đổ đầy lại và xáo trộn để bốc tiếp
                if (grabBag.Count == 0)
                {
                    grabBag.AddRange(commonItemTemplates);
                    ShuffleList(grabBag);
                    Debug.Log("Đã bốc hết 1 vòng đồ, đang xáo trộn lại túi Item!");
                }

                // Rút món đồ đầu tiên ra khỏi túi
                ItemController selectedTemplate = grabBag[0];
                grabBag.RemoveAt(0); // Xóa khỏi túi để không bốc trúng lại ở lượt sau

                // Tiến hành Spawn
                int val = SpawnItemFromTemplate(selectedTemplate, availableSpawns[i]);
                spawnedValues.Add(val);
            }
        }

        // ================================================================
        // BƯỚC 3: TÍNH QUOTA
        // ================================================================
        if (overrideQuotaForTesting)
        {
            currentQuota = testQuotaValue;
        }
        else
        {
            // Tính toán Quota linh hoạt dựa trên giá trị tổng
            float averageValue = 0;
            if (spawnedValues.Count > 0) averageValue = (float)totalMapValue / spawnedValues.Count;

            // Giữ lại phần buffer để trừ đi (tạo độ khó hợp lý)
            int bufferValue = Mathf.RoundToInt(averageValue * bufferItemsCount);
            currentQuota = Mathf.Max(totalMapValue - bufferValue, Mathf.RoundToInt(totalMapValue * 0.3f));

            Debug.Log($"FINAL QUOTA: {currentQuota} (Total Map Value: {totalMapValue})");
        }

        if (sellingZone != null) sellingZone.quotaMoney = currentQuota;
    }

    // --- HÀM MỚI: SPAWN TỪ TEMPLATE ---
    int SpawnItemFromTemplate(ItemController template, Transform location)
    {
        GameObject obj = Instantiate(template.gameObject, location.position, location.rotation);
        obj.SetActive(true);

        ItemController ctrl = obj.GetComponent<ItemController>();
        ctrl.transform.localScale = template.transform.localScale;
        ctrl.data = template.data;
        ctrl.InitializeValue();

        totalMapValue += ctrl.scrapValue;
        return ctrl.scrapValue;
    }

    // --- HÀM TIỆN ÍCH: XÁO TRỘN DANH SÁCH (Thuật toán Fisher-Yates) ---
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}