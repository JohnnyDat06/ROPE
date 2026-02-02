using UnityEngine;
using TMPro;

public class SecurityRoomManager : MonoBehaviour
{
    [Header("Dependencies")]
    public TurretTrap[] turrets;
    public TextMeshPro neonBoardText;

    [Header("Game Logic Data")]
    public CheckMode currentMode;
    public ConditionType currentCondition;
    public float targetValue;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // --- SỬA ĐỔI 1: Đổi tên class thành PlayerInventorySystem ---
            var playerInv = other.GetComponent<PlayerInventorySystem>();
            if (playerInv != null) CheckPlayer(playerInv, other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var turret in turrets) turret.DeactivateTrap();
        }
    }

    private void Start()
    {
        RandomizeRoomRules();
    }

    public void RandomizeRoomRules()
    {
        currentMode = (CheckMode)Random.Range(0, 2);
        currentCondition = (ConditionType)Random.Range(0, 3);

        if (currentMode == CheckMode.Quantity)
        {
            targetValue = Random.Range(1, 5);
        }
        else // CheckMode.Weight
        {
            targetValue = Random.Range(20f, 36f);
            targetValue = (float)System.Math.Round(targetValue, 2);
        }

        UpdateNeonBoard();
    }

    void UpdateNeonBoard()
    {
        string modeStr = currentMode == CheckMode.Weight ? "KG" : "ITEMS";
        string condStr = "";

        switch (currentCondition)
        {
            case ConditionType.Lower: condStr = "<"; break;
            case ConditionType.Equal: condStr = "="; break;
            case ConditionType.Higher: condStr = ">"; break;
        }

        neonBoardText.text = $"REQ: {condStr} {targetValue} {modeStr}";
    }

    // --- SỬA ĐỔI 2: Cập nhật tham số và tên biến ---
    void CheckPlayer(PlayerInventorySystem player, Transform playerTransform)
    {
        // Lưu ý: Dùng player.TotalWeight (Viết hoa) và player.TotalItemCount (Thay cho totalItems)
        float playerValue = (currentMode == CheckMode.Weight) ? player.TotalWeight : player.TotalItemCount;

        bool isSafe = false;

        switch (currentCondition)
        {
            case ConditionType.Lower:
                isSafe = playerValue < targetValue;
                break;

            case ConditionType.Equal:
                isSafe = Mathf.Abs(playerValue - targetValue) <= 0.0001f;
                break;

            case ConditionType.Higher:
                isSafe = playerValue > targetValue;
                break;
        }

        if (!isSafe)
        {
            foreach (var turret in turrets) turret.ActivateTrap(playerTransform);
        }
        else
        {
            neonBoardText.text = "PASSED";
            neonBoardText.color = Color.green;
        }
    }
}