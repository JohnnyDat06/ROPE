using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Testing")]
    public bool overrideQuotaForTesting = false;
    public int testQuotaValue = 130;

    [Header("Settings")]
    public int minItemsToSpawn = 10;
    public int maxItemsToSpawn = 15;
    [Range(0f, 1f)] public float specialItemChance = 0.3f; // 30% ra đồ đặc biệt
    public int bufferItemsCount = 5;

    [Header("--- VỊ TRÍ ĐẶC BIỆT (FIRE ROOM) ---")]
    [Tooltip("Kéo vị trí bàn đạp trong phòng lửa vào đây")]
    public Transform fireRoomSpawnPoint;

    [Header("References")]
    public List<Transform> allSpawnPoints; // Các điểm spawn còn lại
    public List<ItemData> commonItems;
    public List<ItemData> specialItems;

    public SellingZone sellingZone;

    public int totalMapValue;
    public int currentQuota;

    void Start()
    {
        SpawnLevelItems();
    }

    void SpawnLevelItems()
    {
        // Copy danh sách điểm spawn để xử lý (tránh lỗi list gốc)
        List<Transform> availableSpawns = new List<Transform>(allSpawnPoints);

        // Loại bỏ điểm spawn phòng lửa ra khỏi danh sách chung (để không bị spawn chồng 2 món)
        if (availableSpawns.Contains(fireRoomSpawnPoint))
        {
            availableSpawns.Remove(fireRoomSpawnPoint);
        }

        int spawnCount = Random.Range(minItemsToSpawn, maxItemsToSpawn + 1);
        totalMapValue = 0;
        List<int> spawnedValues = new List<int>();

        // ================================================================
        // BƯỚC 1: XỬ LÝ PHÒNG LỬA (BẮT BUỘC PHẢI CÓ ITEM)
        // ================================================================
        if (fireRoomSpawnPoint != null)
        {
            ItemData fireRoomItem = null;

            // Roll tỷ lệ ra đồ đặc biệt
            bool isSpecial = Random.value <= specialItemChance;

            if (isSpecial && specialItems.Count > 0)
            {
                // May mắn: Lấy đồ đặc biệt
                fireRoomItem = specialItems[Random.Range(0, specialItems.Count)];
                Debug.Log("<color=cyan>FIRE ROOM: Spawned SPECIAL Item!</color>");
            }
            else
            {
                // Xui: Lấy đồ thường thế mạng (Fallback)
                if (commonItems.Count > 0)
                {
                    fireRoomItem = commonItems[Random.Range(0, commonItems.Count)];
                    Debug.Log("<color=orange>FIRE ROOM: Spawned COMMON Item (Fallback)</color>");
                }
            }

            // Tiến hành Spawn ngay tại bàn đạp
            if (fireRoomItem != null)
            {
                int val = SpawnItem(fireRoomItem, fireRoomSpawnPoint);
                spawnedValues.Add(val);
                spawnCount--; // Trừ đi 1 suất spawn vì đã dùng cho phòng lửa
            }
        }

        // ================================================================
        // BƯỚC 2: SPAWN CÁC MÓN CÒN LẠI RA MAP
        // ================================================================

        // Shuffle (Tráo bài) vị trí spawn
        for (int i = 0; i < availableSpawns.Count; i++)
        {
            Transform temp = availableSpawns[i];
            int randomIndex = Random.Range(i, availableSpawns.Count);
            availableSpawns[i] = availableSpawns[randomIndex];
            availableSpawns[randomIndex] = temp;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            if (i >= availableSpawns.Count) break;

            // Lấy ngẫu nhiên đồ thường
            ItemData randomItem = commonItems[Random.Range(0, commonItems.Count)];
            int val = SpawnItem(randomItem, availableSpawns[i]);
            spawnedValues.Add(val);
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
            float averageValue = 0;
            if (spawnedValues.Count > 0) averageValue = (float)totalMapValue / spawnedValues.Count;
            int bufferValue = Mathf.RoundToInt(averageValue * bufferItemsCount);
            currentQuota = Mathf.Max(totalMapValue - bufferValue, Mathf.RoundToInt(totalMapValue * 0.3f));

            Debug.Log($"FINAL QUOTA: {currentQuota} (Total: {totalMapValue})");
        }

        if (sellingZone != null) sellingZone.quotaMoney = currentQuota;
    }

    int SpawnItem(ItemData data, Transform location)
    {
        GameObject obj = Instantiate(data.modelPrefab, location.position, location.rotation);
        ItemController ctrl = obj.GetComponent<ItemController>();
        if (ctrl == null) ctrl = obj.AddComponent<ItemController>();

        ctrl.data = data;
        ctrl.InitializeValue();

        totalMapValue += ctrl.scrapValue;
        return ctrl.scrapValue;
    }
}