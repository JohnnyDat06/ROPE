using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventorySystem : MonoBehaviour
{
    // ========================================================================
    // 1. SETTINGS & REFERENCES
    // ========================================================================
    [Header("--- 1. Interaction Settings ---")]
    [Tooltip("Khoảng cách có thể nhặt đồ")]
    public float interactDistance = 6.0f;
    public LayerMask itemLayer;
    public Transform dropPoint;

    [Header("--- 2. Charge Throw (Ném Gồng Lực) ---")]
    public float minThrowForce = 2.0f;
    public float maxThrowForce = 15.0f;
    public float maxChargeTime = 5.0f;

    [Header("--- 3. Drop Physics ---")]
    public float dropLinearDamping = 1.0f;
    public float dropAngularDamping = 1.0f;
    public float objectSpin = 2.0f;

    [Header("--- 4. Inventory 3D Slots ---")]
    public Transform[] inventorySlots;

    [Header("--- 5. UI Inventory Slots ---")]
    public RectTransform[] slotUIFrames;
    public float selectedScale = 1.2f;
    public float normalScale = 1.0f;
    public float uiScaleSpeed = 10f;

    [Header("--- 6. General UI References ---")]
    public Image progressCircle;
    public TextMeshProUGUI promptText;

    [Tooltip("Kéo TextMeshPro để hiển thị tổng tiền vào đây (VD: TOTAL: 50$)")]
    public TextMeshProUGUI totalValueText; // <--- MỚI: HIỂN THỊ TỔNG TIỀN

    [Header("--- 7. External Systems ---")]
    public WeatherManager weatherManager;

    // ========================================================================
    // 2. PRIVATE VARIABLES
    // ========================================================================
    private Camera playerCam;
    private ItemController[] inventoryItems;
    private ItemController targetItem;

    private int currentSlotIndex = 0;
    private float pickupTimer = 0f;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;

    // Properties
    public float TotalWeight { get; private set; } // Vẫn giữ để tính logic vật lý nếu cần
    public int TotalValue { get; private set; }
    public int TotalItemCount { get; private set; }

    // ========================================================================
    // 3. CORE FUNCTIONS
    // ========================================================================
    void Start()
    {
        playerCam = Camera.main;
        inventoryItems = new ItemController[inventorySlots.Length];

        if (progressCircle) progressCircle.fillAmount = 0;
        if (promptText) promptText.gameObject.SetActive(false);

        // Reset text tiền ban đầu
        if (totalValueText) totalValueText.text = "TOTAL: $0";
    }

    void Update()
    {
        HandleInteraction();
        HandleInput();
        HandleSlotSelectionUI();
        RotateInventoryItems();
        UpdateStats();
    }

    // ========================================================================
    // 4. INPUT & INTERACTION
    // ========================================================================
    void HandleInteraction()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, itemLayer))
        {
            if (hit.collider.TryGetComponent(out ItemController item))
            {
                targetItem = item;
                if (promptText)
                {
                    promptText.text = $"{item.data.itemName} <color=yellow>(${item.scrapValue})</color>\n<size=80%>[Hold E]</size>";
                    promptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        targetItem = null;
        if (promptText) promptText.gameObject.SetActive(false);

        if (!isChargingThrow && pickupTimer > 0)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }
    }

    void HandleInput()
    {
        // A. CHỌN SLOT
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlotIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlotIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlotIndex = 3;
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, inventorySlots.Length - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) currentSlotIndex = (currentSlotIndex + 1) % inventorySlots.Length;
        if (scroll < 0) currentSlotIndex = (currentSlotIndex - 1 + inventorySlots.Length) % inventorySlots.Length;

        // B. NHẶT ĐỒ
        if (Input.GetKey(KeyCode.E) && targetItem != null && !isChargingThrow)
        {
            int emptyIndex = GetEmptySlot();
            if (emptyIndex != -1)
            {
                pickupTimer += Time.deltaTime;
                float percent = pickupTimer / targetItem.data.pickupDuration;
                if (progressCircle) progressCircle.fillAmount = percent;

                if (pickupTimer >= targetItem.data.pickupDuration)
                {
                    currentSlotIndex = emptyIndex;
                    PickupItem(targetItem, emptyIndex);

                    pickupTimer = 0;
                    targetItem = null;
                    if (progressCircle) progressCircle.fillAmount = 0;
                    if (promptText) promptText.gameObject.SetActive(false);
                }
            }
            else if (promptText) promptText.text = "<color=red>INVENTORY FULL!</color>";
        }
        else if (!isChargingThrow)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }

        // C. NÉM GỒNG LỰC
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (inventoryItems[currentSlotIndex] != null)
            {
                isChargingThrow = true;
                throwChargeTimer = 0f;
            }
        }

        if (Input.GetKey(KeyCode.Q) && isChargingThrow)
        {
            throwChargeTimer += Time.deltaTime;
            float chargePercent = Mathf.Clamp01(throwChargeTimer / maxChargeTime);
            if (progressCircle) progressCircle.fillAmount = chargePercent;

            if (throwChargeTimer >= maxChargeTime)
            {
                DropItem(currentSlotIndex, maxThrowForce);
                isChargingThrow = false;
                throwChargeTimer = 0f;
                if (progressCircle) progressCircle.fillAmount = 0;
                return;
            }
        }

        if (Input.GetKeyUp(KeyCode.Q) && isChargingThrow)
        {
            float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, throwChargeTimer / maxChargeTime);
            DropItem(currentSlotIndex, finalForce);
            isChargingThrow = false;
            throwChargeTimer = 0f;
            if (progressCircle) progressCircle.fillAmount = 0;
        }
    }

    // ========================================================================
    // 5. ITEM HANDLING
    // ========================================================================
    void PickupItem(ItemController item, int slotIndex)
    {
        inventoryItems[slotIndex] = item;
        item.transform.SetParent(inventorySlots[slotIndex]);
        item.transform.localPosition = item.data.inventoryPositionOffset;
        item.transform.localRotation = Quaternion.Euler(item.data.inventoryRotationOffset);
        item.transform.localScale = Vector3.one * item.data.inventoryScale;
        item.SetState(true);
    }

    void DropItem(int slotIndex, float forceToApply)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;
        ItemController item = inventoryItems[slotIndex];
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.position = dropPoint.position;
        item.transform.localScale = Vector3.one;
        item.SetState(false);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = dropLinearDamping;
            rb.angularDamping = dropAngularDamping;

            Vector3 throwDir = (playerCam.transform.forward + Vector3.up * 0.2f).normalized;
            rb.AddForce(throwDir * forceToApply, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * objectSpin, ForceMode.Impulse);
        }

        inventoryItems[slotIndex] = null;
    }

    int GetEmptySlot()
    {
        for (int i = 0; i < inventoryItems.Length; i++) if (inventoryItems[i] == null) return i;
        return -1;
    }

    void HandleSlotSelectionUI()
    {
        if (slotUIFrames == null) return;
        for (int i = 0; i < slotUIFrames.Length; i++)
        {
            if (slotUIFrames[i] == null) continue;
            float target = (i == currentSlotIndex) ? selectedScale : normalScale;
            slotUIFrames[i].localScale = Vector3.Lerp(slotUIFrames[i].localScale, Vector3.one * target, Time.deltaTime * uiScaleSpeed);
        }
    }

    void RotateInventoryItems()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] != null) inventoryItems[i].transform.Rotate(Vector3.up * 30f * Time.deltaTime);
        }
    }

    // ========================================================================
    // 6. UPDATE STATS & UI (MỚI)
    // ========================================================================
    void UpdateStats()
    {
        float w = 0;
        int v = 0;
        int c = 0;
        float lightningChance = 0f;

        foreach (var item in inventoryItems)
        {
            if (item != null)
            {
                w += item.data.weight;
                v += item.scrapValue;
                c++;

                // Logic Sét
                if (item.data.itemType == ItemType.IronSmall) lightningChance += 0.10f;
                else if (item.data.itemType == ItemType.IronLarge) lightningChance += 0.15f;
            }
        }

        TotalWeight = w;
        TotalValue = v;
        TotalItemCount = c;

        // --- CẬP NHẬT UI TỔNG TIỀN (MỚI) ---
        if (totalValueText != null)
        {
            totalValueText.text = $"TOTAL: <color=yellow>${TotalValue}</color>";
        }

        // Cập nhật Weather
        if (weatherManager) weatherManager.currentStrikeChance = lightningChance;
    }
}