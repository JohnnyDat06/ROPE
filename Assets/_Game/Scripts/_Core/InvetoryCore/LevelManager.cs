using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("--- Testing ---")]
    public bool overrideQuotaForTesting = false;
    public int testQuotaValue = 130;

    [Header("--- Spawn Settings ---")]
    [Tooltip("Số lượng đồ cơ bản TỐI THIỂU muốn xuất hiện (VD: 11)")]
    public int minItemsToSpawn = 10;

    [Tooltip("Số lượng đồ cơ bản TỐI ĐA muốn xuất hiện (VD: 15)")]
    public int maxItemsToSpawn = 12;

    [Tooltip("Số lượng đồ cộng thêm (Buffer dư ra để người chơi dễ thở) (VD: 10)")]
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

        // Tính tổng số đồ sẽ xuất hiện trên map
        int baseSpawnCount = Random.Range(minItemsToSpawn, maxItemsToSpawn + 1);
        int totalSpawnCount = baseSpawnCount + bufferItemsCount;

        totalMapValue = 0;
        List<int> spawnedValues = new List<int>();

        if (totalSpawnCount > availableSpawns.Count)
        {
            Debug.LogWarning($"[LevelManager] Bạn muốn spawn {totalSpawnCount} món nhưng map chỉ có {availableSpawns.Count} vị trí. Sẽ chỉ spawn tối đa {availableSpawns.Count} món!");
            totalSpawnCount = availableSpawns.Count;
        }

        // ================================================================
        // BƯỚC 1: XỬ LÝ PHÒNG LỬA
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
            }

            if (templateToSpawn != null)
            {
                int val = SpawnItemFromTemplate(templateToSpawn, fireRoomSpawnPoint);
                spawnedValues.Add(val);
                totalSpawnCount--;
            }
        }

        // ================================================================
        // BƯỚC 2: SPAWN CÁC MÓN CÒN LẠI (THUẬT TOÁN TÚI ĐỒ)
        // ================================================================
        ShuffleList(availableSpawns);
        List<ItemController> grabBag = new List<ItemController>(commonItemTemplates);
        ShuffleList(grabBag);

        for (int i = 0; i < totalSpawnCount; i++)
        {
            if (i >= availableSpawns.Count) break;

            if (commonItemTemplates.Count > 0)
            {
                if (grabBag.Count == 0)
                {
                    grabBag.AddRange(commonItemTemplates);
                    ShuffleList(grabBag);
                }

                ItemController selectedTemplate = grabBag[0];
                grabBag.RemoveAt(0);

                int val = SpawnItemFromTemplate(selectedTemplate, availableSpawns[i]);
                spawnedValues.Add(val);
            }
        }

        // ================================================================
        // BƯỚC 3: TÍNH QUOTA (Theo chuẩn công thức: Num = N - Buffer)
        // ================================================================
        if (overrideQuotaForTesting)
        {
            currentQuota = testQuotaValue;
        }
        else
        {
            int totalItemsSpawned = spawnedValues.Count;

            // Tính giá trị trung bình của 1 món đồ
            float averageItemValue = 0;
            if (totalItemsSpawned > 0)
            {
                averageItemValue = (float)totalMapValue / totalItemsSpawned;
            }

            // Áp dụng công thức của bạn: Num = Tổng đồ (N) - Đồ dư (Buffer)
            int numForQuota = totalItemsSpawned - bufferItemsCount;

            // Đề phòng trường hợp bạn nhập số Đồ dư lớn hơn cả Tổng đồ, ta giữ Num tối thiểu là 1 để tránh lỗi Quota = 0$
            if (numForQuota < 1)
            {
                numForQuota = 1;
            }

            // Tính Quota cuối cùng = Num * Giá trị trung bình
            currentQuota = Mathf.RoundToInt(numForQuota * averageItemValue);

            // In ra Console để bạn dễ dàng kiểm tra phép tính
            Debug.Log($"<color=yellow>[GAME BALANCE] Tính Quota theo công thức N - Buffer:</color>");
            Debug.Log($"Tổng đồ: {totalItemsSpawned} | Đồ dư: {bufferItemsCount} => <color=green>Num (Số đồ làm gốc): {numForQuota}</color>");
            Debug.Log($"Giá trị trung bình 1 món: {averageItemValue:F1}$");
            Debug.Log($"<color=orange>FINAL QUOTA = {numForQuota} * {averageItemValue:F1} = {currentQuota}$</color> (Tổng giá trị map: {totalMapValue}$)");
        }

        if (sellingZone != null) sellingZone.quotaMoney = currentQuota;
    }

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