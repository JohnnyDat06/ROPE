using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SecurityRoomManager : MonoBehaviour
{
    [Header("Dependencies")]
    public TurretTrap[] turrets;
    public TextMeshPro neonBoardText;

    [Header("Calibration")]
    public float quotaMultiplierMin = 1.3f;
    public float quotaMultiplierMax = 2.5f;
    [Tooltip("Thời gian chờ quét an ninh (Giây). Giúp game tự nhiên hơn, tránh giật lag.")]
    public float scanningDuration = 1.0f; // Chờ 1 giây rồi mới phán xét

    [Header("Game Logic Data")]
    public CheckMode currentMode;
    public ConditionType currentCondition;
    public float targetValue;

    // --- BIẾN TRẠNG THÁI ---
    private PlayerInventorySystem currentPlayerInside = null;
    private float realMapAverageValue = 0;

    private bool isScanning = false;    // Đang trong quá trình quét?
    private float scanTimer = 0f;       // Bộ đếm thời gian
    private bool hasPassedInitialScan = false; // Đã quét xong lần đầu chưa?

    private void Start()
    {
        Invoke(nameof(InitializeSecurity), 0.5f);
    }

    void InitializeSecurity()
    {
        ScanMapItems();
        RandomizeRoomRules();
    }

    // (Giữ nguyên hàm ScanMapItems như cũ)
    void ScanMapItems()
    {
        ItemController[] allItems = FindObjectsByType<ItemController>(FindObjectsSortMode.None);
        if (allItems.Length > 0)
        {
            float totalScrap = 0;
            foreach (var item in allItems) totalScrap += item.scrapValue;
            realMapAverageValue = totalScrap / allItems.Length;
        }
        else realMapAverageValue = 50f;
    }

    // (Giữ nguyên RandomizeRoomRules như cũ)
    public void RandomizeRoomRules()
    {
        currentMode = (CheckMode)Random.Range(0, 2);
        if (currentMode == CheckMode.Quantity)
        {
            currentCondition = (ConditionType)Random.Range(0, 3);
            targetValue = Random.Range(1, 5);
        }
        else
        {
            currentCondition = (Random.value > 0.5f) ? ConditionType.Higher : ConditionType.Lower;
            float calculatedTarget = realMapAverageValue * Random.Range(quotaMultiplierMin, quotaMultiplierMax);
            targetValue = Mathf.RoundToInt(calculatedTarget / 10) * 10;
            if (targetValue < 10) targetValue = 10;
        }
        UpdateNeonBoard_Idle(); // Hiển thị trạng thái chờ ban đầu
    }

    // --- LOGIC MỚI TRONG UPDATE ---
    private void Update()
    {
        if (currentPlayerInside != null)
        {
            if (isScanning)
            {
                // 1. GIAI ĐOẠN QUÉT (SCANNING)
                HandleScanningPhase();
            }
            else
            {
                // 2. GIAI ĐOẠN GIÁM SÁT (REAL-TIME MONITORING)
                // Chỉ chạy khi đã quét xong
                CheckPlayerRealtime(currentPlayerInside);
            }
        }
    }

    // Xử lý đếm ngược khi mới bước vào
    void HandleScanningPhase()
    {
        scanTimer -= Time.deltaTime;

        // Hiệu ứng chữ nhấp nháy hoặc chấm chấm cho ngầu
        if (neonBoardText)
        {
            neonBoardText.color = Color.yellow;
            // Tạo hiệu ứng dấu chấm chạy chạy: SCANNING. -> SCANNING.. -> SCANNING...
            int dotCount = (int)(Time.time * 3) % 4;
            string dots = new string('.', dotCount);
            neonBoardText.text = $"SCANNING{dots}";
        }

        // Hết giờ -> Kết thúc quét -> Chuyển sang phán xét
        if (scanTimer <= 0)
        {
            isScanning = false;
            hasPassedInitialScan = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerInv = other.GetComponent<PlayerInventorySystem>();
            if (playerInv != null)
            {
                currentPlayerInside = playerInv;

                // --- BẮT ĐẦU QUY TRÌNH QUÉT MỚI ---
                isScanning = true;
                scanTimer = scanningDuration; // Đặt lại thời gian chờ (1s)
                hasPassedInitialScan = false;

                // Đảm bảo súng KHÔNG BẮN trong lúc đang quét
                foreach (var turret in turrets) turret.DeactivateTrap();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            currentPlayerInside = null;
            isScanning = false;
            hasPassedInitialScan = false;

            // Tắt súng
            foreach (var turret in turrets) turret.DeactivateTrap();

            // Trả lại bảng hiển thị gốc
            UpdateNeonBoard_Idle();
        }
    }

    void CheckPlayerRealtime(PlayerInventorySystem player)
    {
        float playerValue = (currentMode == CheckMode.TotalValue) ? player.TotalValue : player.TotalItemCount;
        bool isSafe = false;

        switch (currentCondition)
        {
            case ConditionType.Lower: isSafe = playerValue < targetValue; break;
            case ConditionType.Equal: isSafe = Mathf.Abs(playerValue - targetValue) <= 0.1f; break;
            case ConditionType.Higher: isSafe = playerValue > targetValue; break;
        }

        if (!isSafe)
        {
            // VI PHẠM: Kích hoạt súng
            foreach (var turret in turrets) turret.ActivateTrap(player.transform);

            if (neonBoardText)
            {
                neonBoardText.color = Color.red;
                neonBoardText.text = "ACCESS DENIED";
            }
        }
        else
        {
            // AN TOÀN
            foreach (var turret in turrets) turret.DeactivateTrap();

            if (neonBoardText)
            {
                neonBoardText.color = Color.green;
                neonBoardText.text = "PASSED";
            }
        }
    }

    // Hàm hiển thị khi không có ai (trạng thái chờ)
    void UpdateNeonBoard_Idle()
    {
        if (neonBoardText == null) return;
        string modeStr = currentMode == CheckMode.TotalValue ? "$" : "ITEMS";
        string condStr = "";
        switch (currentCondition)
        {
            case ConditionType.Lower: condStr = "<"; break;
            case ConditionType.Equal: condStr = "="; break;
            case ConditionType.Higher: condStr = ">"; break;
        }
        neonBoardText.text = $"REQ: {condStr} {targetValue} {modeStr}";
        neonBoardText.color = Color.yellow;
    }
}