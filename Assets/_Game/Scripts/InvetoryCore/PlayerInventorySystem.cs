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
    public LayerMask itemLayer; // Chỉ chọn layer "Interactable"
    public Transform dropPoint; // Vị trí đồ rơi ra (trước mặt Camera)

    [Header("--- 2. Charge Throw (Ném Gồng Lực) ---")]
    public float minThrowForce = 2.0f;   // Nhấp nhẹ: Rơi ngay dưới chân

    // --- CẬP NHẬT: Tăng lực ném tối đa lên 25 (Ném cực mạnh) ---
    public float maxThrowForce = 25.0f;

    [Tooltip("Thời gian gồng tối đa. Nếu giữ quá thời gian này sẽ tự bắn.")]
    public float maxChargeTime = 3.0f; // Giảm xuống 3s cho gồng lẹ hơn

    [Header("--- 3. Drop Physics (Rơi Đầm) ---")]
    public float dropLinearDamping = 1.0f;
    public float dropAngularDamping = 1.0f;
    public float objectSpin = 5.0f; // Tăng độ xoay khi ném mạnh cho ngầu

    [Header("--- 4. Inventory 3D Slots ---")]
    [Tooltip("Kéo 4 Empty Object dưới lòng đất vào đây")]
    public Transform[] inventorySlots;

    [Header("--- 5. UI Display ---")]
    public RectTransform[] slotUIFrames;
    public float selectedScale = 1.2f;
    public float normalScale = 1.0f;
    public float uiScaleSpeed = 10f;

    [Header("--- 6. UI References ---")]
    public Image progressCircle;
    public TextMeshProUGUI promptText;
    [Tooltip("Kéo Text hiển thị tổng tiền vào đây")]
    public TextMeshProUGUI totalValueText;

    [Header("--- 7. External Systems ---")]
    public WeatherManager weatherManager;

    // ========================================================================
    // 2. PRIVATE VARIABLES & PROPERTIES
    // ========================================================================
    private Camera playerCam;
    private ItemController[] inventoryItems;
    private ItemController targetItem;

    private int currentSlotIndex = 0;

    // Biến xử lý timer
    private float pickupTimer = 0f;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;

    // Properties
    public float TotalWeight { get; private set; }
    public int TotalValue { get; private set; }
    public int TotalItemCount { get; private set; }

    // ========================================================================
    // 3. CORE FUNCTIONS
    // ========================================================================
    void Start()
    {
        playerCam = Camera.main;
        inventoryItems = new ItemController[inventorySlots.Length];

        // Ẩn UI lúc đầu
        if (progressCircle) progressCircle.fillAmount = 0;
        if (promptText) promptText.gameObject.SetActive(false);
        if (totalValueText) totalValueText.text = "TOTAL: $0";
    }

    void Update()
    {
        HandleInteraction();    // Xử lý nhìn đồ
        HandleInput();          // Xử lý phím bấm (Nhặt & Ném)
        HandleSlotSelectionUI();// Hiệu ứng phóng to ô túi
        RotateInventoryItems(); // Xoay đồ trong túi 3D
        UpdateStats();          // Cập nhật thông số
    }

    // ========================================================================
    // 4. INPUT & INTERACTION LOGIC
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
        // --- A. CHỌN SLOT ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlotIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlotIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlotIndex = 3;
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, inventorySlots.Length - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) currentSlotIndex = (currentSlotIndex + 1) % inventorySlots.Length;
        if (scroll < 0) currentSlotIndex = (currentSlotIndex - 1 + inventorySlots.Length) % inventorySlots.Length;

        // --- B. NHẶT ĐỒ ---
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
            else if (promptText) promptText.text = "<color=red>FULL!</color>";
        }
        else if (!isChargingThrow)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }

        // --- C. NÉM ĐỒ (Q) ---
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

            // Auto Throw sau khi gồng Max
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
            // Tính toán lực ném dựa trên thời gian giữ
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

    // --- HÀM DROP THÔNG MINH (XẾP CHỒNG + NÉM MẠNH) ---
    void DropItem(int slotIndex, float forceToApply)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;
        ItemController item = inventoryItems[slotIndex];
        if (item == null) return;

        // 1. Tách khỏi người chơi
        item.transform.SetParent(null);

        // 2. Reset Scale & Rotation
        // Nếu ném nhẹ (Nhấp Q) -> Dựng đứng item để dễ xếp chồng
        bool isGentleDrop = (forceToApply <= minThrowForce * 1.5f);

        item.transform.localScale = Vector3.one;
        if (isGentleDrop)
        {
            item.transform.rotation = Quaternion.identity;
        }

        // 3. Tính toán kích thước thật
        float itemHalfHeight = 0.25f;
        Collider col = item.GetComponent<Collider>();
        if (col != null) itemHalfHeight = col.bounds.extents.y;

        // 4. SphereCast để tìm chỗ đặt an toàn (Chống xuyên vật thể)
        Vector3 finalPos = dropPoint.position;
        float checkRadius = 0.2f;
        float checkDistance = 1.5f;

        if (Physics.SphereCast(dropPoint.position, checkRadius, Vector3.down, out RaycastHit hit, checkDistance))
        {
            float targetHeight = hit.point.y + itemHalfHeight + 0.05f;
            if (finalPos.y < targetHeight || (finalPos.y - hit.point.y) < itemHalfHeight * 2)
            {
                finalPos.y = targetHeight;
                finalPos.x = dropPoint.position.x;
                finalPos.z = dropPoint.position.z;
            }
        }

        item.transform.position = finalPos;

        // 5. Bật lại vật lý
        item.SetState(false);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // --- QUAN TRỌNG: BẬT LẠI TRỌNG LỰC ---
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = dropLinearDamping;
            rb.angularDamping = dropAngularDamping;

            // --- LOGIC PHÂN LOẠI LỰC NÉM ---
            if (!isGentleDrop)
            {
                // NÉM MẠNH: Thêm lực đẩy tới + Xoay mạnh
                Vector3 throwDir = (playerCam.transform.forward + Vector3.up * 0.2f).normalized;
                rb.AddForce(throwDir * forceToApply, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * objectSpin, ForceMode.Impulse);
            }
            // NÉM NHẸ: Không AddForce, chỉ rơi tự do tại chỗ (đã tính toán ở trên)
        }

        inventoryItems[slotIndex] = null;
    }

    // ========================================================================
    // 6. HELPER FUNCTIONS & UI
    // ========================================================================
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

                if (item.data.itemType == ItemType.IronSmall) lightningChance += 0.10f;
                else if (item.data.itemType == ItemType.IronLarge) lightningChance += 0.15f;
            }
        }

        TotalWeight = w;
        TotalValue = v;
        TotalItemCount = c;

        if (totalValueText != null) totalValueText.text = $"TOTAL: <color=yellow>${TotalValue}</color>";
        if (weatherManager) weatherManager.currentStrikeChance = lightningChance;
    }
}