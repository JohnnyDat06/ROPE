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
    public float scanningDuration = 1.0f; 

    [Header("Game Logic Data")]
    public CheckMode currentMode;
    public ConditionType currentCondition;
    public float targetValue;

    [Header("--- DEBUG SETTINGS ---")]
    public bool showDebugRays = true; 
    public bool showDebugLogs = true; 

    private PlayerInventorySystem currentPlayerInside = null;
    private float realMapAverageValue = 0;
    private bool isScanning = false;    
    private float scanTimer = 0f;       
    private bool hasPassedInitialScan = false; 

    private void Start()
    {
        Invoke(nameof(InitializeSecurity), 0.5f);
    }

    void InitializeSecurity()
    {
        ScanMapItems();
        RandomizeRoomRules();
    }

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
        UpdateNeonBoard_Idle(); 
    }

    private void Update()
    {
        if (currentPlayerInside != null)
        {
            if (isScanning)
            {
                HandleScanningPhase();
                // [DEBUG] Vẽ tia màu vàng (Cảnh báo)
                if (showDebugRays) DrawDebugRays(Color.yellow);
            }
            else
            {
                CheckPlayerRealtime(currentPlayerInside);
            }
        }
    }

    void HandleScanningPhase()
    {
        scanTimer -= Time.deltaTime;
        if (neonBoardText)
        {
            neonBoardText.color = Color.yellow;
            int dotCount = (int)(Time.time * 3) % 4;
            string dots = new string('.', dotCount);
            neonBoardText.text = $"SCANNING{dots}";
        }
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
                isScanning = true;
                scanTimer = scanningDuration; 
                hasPassedInitialScan = false;
                foreach (var turret in turrets) turret.DeactivateTrap();
                if (showDebugLogs) Debug.Log("<color=yellow>SECURITY: Player entered. Scanning started...</color>");
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
            foreach (var turret in turrets) turret.DeactivateTrap();
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
            // --- VI PHẠM ---
            foreach (var turret in turrets) 
            {
                turret.ActivateTrap(); // SỬA: Không cần truyền player nữa
                
                // [DEBUG] Vẽ tia ĐỎ thẳng tắp từ nòng súng
                if (showDebugRays && turret.firePoint != null)
                    Debug.DrawRay(turret.firePoint.position, turret.firePoint.forward * 10f, Color.red);
            }

            if (neonBoardText)
            {
                neonBoardText.color = Color.red;
                neonBoardText.text = "ACCESS DENIED";
            }
        }
        else
        {
            // --- AN TOÀN ---
            foreach (var turret in turrets) 
            {
                turret.DeactivateTrap();
                
                // [DEBUG] Vẽ tia XANH LÁ thẳng tắp (An toàn)
                if (showDebugRays && turret.firePoint != null)
                    Debug.DrawRay(turret.firePoint.position, turret.firePoint.forward * 10f, Color.green);
            }

            if (neonBoardText)
            {
                neonBoardText.color = Color.green;
                neonBoardText.text = "PASSED";
            }
        }
    }

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

    // Hàm phụ trợ vẽ tia thẳng
    void DrawDebugRays(Color color)
    {
        foreach (var turret in turrets)
        {
            if (turret != null && turret.firePoint != null)
            {
                // Thay vì nối vào người chơi, ta vẽ tia Forward dài 10m
                Debug.DrawRay(turret.firePoint.position, turret.firePoint.forward * 10f, color);
            }
        }
    }
}