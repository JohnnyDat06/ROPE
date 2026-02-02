using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Testing")]
    public bool overrideQuotaForTesting = false; // TICK CÁI NÀY NẾU MUỐN TỰ CHỈNH QUOTA = 5
    public int testQuotaValue = 5;

    [Header("Settings")]
    public int minItemsToSpawn = 10;
    public int maxItemsToSpawn = 15;
    [Range(0f, 1f)] public float specialItemChance = 0.3f;
    [Tooltip("Số lượng món đồ muốn DƯ RA cho người chơi (để game không quá khó)")]
    public int bufferItemsCount = 5;

    [Header("References")]
    public List<Transform> allSpawnPoints;
    public List<Transform> specialSpawnPoints;
    public List<ItemData> commonItems;
    public List<ItemData> specialItems;

    // Kéo SellingZone vào đây để cập nhật số tiền Quota cho nó
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
        int spawnCount = Random.Range(minItemsToSpawn, maxItemsToSpawn + 1);
        totalMapValue = 0;

        // List tạm để tính giá trị trung bình
        List<int> spawnedValues = new List<int>();

        // 1. Spawn Đồ Đặc Biệt
        if (Random.value <= specialItemChance && specialSpawnPoints.Count > 0 && specialItems.Count > 0)
        {
            Transform spPoint = specialSpawnPoints[Random.Range(0, specialSpawnPoints.Count)];
            ItemData spItem = specialItems[Random.Range(0, specialItems.Count)];
            int val = SpawnItem(spItem, spPoint);
            spawnedValues.Add(val);
            spawnCount--;
        }

        // 2. Spawn Đồ Thường (Shuffle)
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
            ItemData randomItem = commonItems[Random.Range(0, commonItems.Count)];
            int val = SpawnItem(randomItem, availableSpawns[i]);
            spawnedValues.Add(val);
        }

        // --- FIX LOGIC TÍNH QUOTA ---
        if (overrideQuotaForTesting)
        {
            currentQuota = testQuotaValue;
            Debug.Log($"<color=cyan>TEST MODE: Quota set cứng là {currentQuota}</color>");
        }
        else
        {
            // Tính giá trị trung bình của các món đồ đã spawn
            float averageValue = 0;
            if (spawnedValues.Count > 0) averageValue = (float)totalMapValue / spawnedValues.Count;

            // Quota = Tổng tiền - (Giá trị trung bình * Số món muốn dư)
            // Ví dụ: Map có 1000$, trung bình mỗi món 100$, muốn dư 5 món (500$) -> Quota = 500$
            int bufferValue = Mathf.RoundToInt(averageValue * bufferItemsCount);

            // Đảm bảo Quota không bị âm hoặc quá thấp (tối thiểu 30% map)
            currentQuota = Mathf.Max(totalMapValue - bufferValue, Mathf.RoundToInt(totalMapValue * 0.3f));

            Debug.Log($"Map Total: {totalMapValue} | Avg Item: {averageValue} | Buffer: {bufferItemsCount} items | -> FINAL QUOTA: {currentQuota}");
        }

        // Gửi số Quota sang cho SellingZone
        if (sellingZone != null)
        {
            sellingZone.quotaMoney = currentQuota;
        }
    }

    int SpawnItem(ItemData data, Transform location)
    {
        GameObject obj = Instantiate(data.modelPrefab, location.position, Quaternion.identity);
        ItemController ctrl = obj.GetComponent<ItemController>();
        if (ctrl == null) ctrl = obj.AddComponent<ItemController>();

        ctrl.data = data;
        ctrl.InitializeValue(); // Random giá tiền

        totalMapValue += ctrl.scrapValue;
        return ctrl.scrapValue;
    }
}