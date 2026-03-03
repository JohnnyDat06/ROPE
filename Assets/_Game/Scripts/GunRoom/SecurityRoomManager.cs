using UnityEngine;
using TMPro;
using System.Reflection; 
using DatScript;

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

    private PlayerInventorySystem currentPlayerInside = null;
    private float realMapAverageValue = 0;
    private bool isScanning = false;
    private float scanTimer = 0f;
    private FieldInfo _hpField; 

    private void Awake()
    {
        _hpField = typeof(PlayerHealth).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Start() => Invoke(nameof(InitializeSecurity), 0.5f);

    void InitializeSecurity()
    {
        ItemController[] allItems = FindObjectsByType<ItemController>(FindObjectsSortMode.None);
        if (allItems.Length > 0)
        {
            float totalScrap = 0;
            foreach (var item in allItems) totalScrap += item.scrapValue;
            realMapAverageValue = totalScrap / allItems.Length;
        }
        else realMapAverageValue = 50f;
        RandomizeRoomRules();
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
            targetValue = Mathf.Max(10, Mathf.RoundToInt((realMapAverageValue * Random.Range(quotaMultiplierMin, quotaMultiplierMax)) / 10) * 10);
        }
        UpdateNeonBoard_Idle();
    }

    private bool IsPlayerDead()
    {
        if (PlayerHealth.instance == null || _hpField == null) return false;
        return (float)_hpField.GetValue(PlayerHealth.instance) <= 0;
    }

    private void Update()
    {
        if (currentPlayerInside != null)
        {
            if (IsPlayerDead())
            {
                currentPlayerInside = null;
                isScanning = false;
                foreach (var turret in turrets) turret.DeactivateTrap();
                UpdateNeonBoard_Idle();
                return; 
            }

            if (isScanning) HandleScanningPhase();
            else CheckPlayerRealtime(currentPlayerInside);
        }
    }

    void HandleScanningPhase()
    {
        scanTimer -= Time.deltaTime;
        if (neonBoardText)
        {
            neonBoardText.color = Color.yellow;
            neonBoardText.text = $"SCANNING{new string('.', (int)(Time.time * 3) % 4)}";
        }
        if (scanTimer <= 0) isScanning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !IsPlayerDead())
        {
            var playerInv = other.GetComponent<PlayerInventorySystem>();
            if (playerInv != null)
            {
                currentPlayerInside = playerInv;
                isScanning = true;
                scanTimer = scanningDuration;
                foreach (var turret in turrets) turret.DeactivateTrap();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentPlayerInside != null)
            {
                currentPlayerInside = null;
                isScanning = false;
                foreach (var turret in turrets) turret.DeactivateTrap();
                UpdateNeonBoard_Idle();
            }
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
            foreach (var turret in turrets) turret.ActivateTrap();
            if (neonBoardText) { neonBoardText.color = Color.red; neonBoardText.text = "ACCESS DENIED"; }
        }
        else
        {
            foreach (var turret in turrets) turret.DeactivateTrap();
            if (neonBoardText) { neonBoardText.color = Color.green; neonBoardText.text = "PASSED"; }
        }
    }

    void UpdateNeonBoard_Idle()
    {
        if (neonBoardText == null) return;
        string modeStr = currentMode == CheckMode.TotalValue ? "$" : "ITEMS";
        string condStr = currentCondition == ConditionType.Lower ? "<" : (currentCondition == ConditionType.Equal ? "=" : ">");
        neonBoardText.text = $"REQ: {condStr} {targetValue} {modeStr}";
        neonBoardText.color = Color.yellow;
    }
}