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

    // ... (Phần OnTriggerEnter và OnTriggerExit giữ nguyên như bài trước) ...
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerInv = other.GetComponent<PlayerInventory>();
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

    // --- CẬP NHẬT LOGIC RANDOM TẠI ĐÂY ---
    public void RandomizeRoomRules()
    {
        // 1. Random Chế độ
        currentMode = (CheckMode)Random.Range(0, 2);

        // 2. Random Điều kiện (Thấp, Bằng, Cao)
        currentCondition = (ConditionType)Random.Range(0, 3);

        // 3. Random Giá trị mục tiêu (Đã sửa theo yêu cầu)
        if (currentMode == CheckMode.Quantity)
        {
            // Yêu cầu: 1 đến 4 món
            // Random.Range(int min, int max) -> max là exclusive (không lấy)
            // Nên phải điền là (1, 5) để lấy được các số: 1, 2, 3, 4
            targetValue = Random.Range(1, 5);
        }
        else // CheckMode.Weight
        {
            // Yêu cầu: 20kg đến 36kg
            // Random.Range(float min, float max) -> max là inclusive (có lấy)
            targetValue = Random.Range(20f, 36f);

            // Làm tròn 2 chữ số thập phân cho đẹp bảng điện tử (VD: 25.43 Kg)
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
            case ConditionType.Lower: condStr = "<"; break;  // Thấp hơn
            case ConditionType.Equal: condStr = "="; break;  // Bằng
            case ConditionType.Higher: condStr = ">"; break; // Cao hơn (Ngược lại với thấp)
        }

        neonBoardText.text = $"REQ: {condStr} {targetValue} {modeStr}";
    }

    // ... (Hàm CheckPlayer giữ nguyên logic kiểm tra) ...
    void CheckPlayer(PlayerInventory player, Transform playerTransform)
    {
        float playerValue = (currentMode == CheckMode.Weight) ? player.totalWeight : player.totalItems;
        bool isSafe = false;

        switch (currentCondition)
        {
            case ConditionType.Lower: // Thấp hơn
                isSafe = playerValue < targetValue;
                break;

            case ConditionType.Equal: // Bằng
                isSafe = Mathf.Abs(playerValue - targetValue) <= 0.0001f;
                break;

            case ConditionType.Higher: // Cao hơn
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