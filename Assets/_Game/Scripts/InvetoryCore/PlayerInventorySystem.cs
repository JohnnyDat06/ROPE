using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventorySystem : MonoBehaviour
{
    // ========================================================================
    // 1. SETTINGS & REFERENCES
    // ========================================================================
    [Header("--- 1. Interaction Settings ---")]
    [Tooltip("Khoảng cách có thể nhặt đồ (Nên để 6m cho dễ với)")]
    public float interactDistance = 6.0f;
    public LayerMask itemLayer; // Chỉ chọn layer "Interactable"
    public Transform dropPoint; // Vị trí đồ rơi ra (trước mặt Camera)

    [Header("--- 2. Charge Throw (Ném Gồng Lực) ---")]
    public float minThrowForce = 2.0f;   // Nhấp nhẹ: Rơi ngay dưới chân
    public float maxThrowForce = 15.0f;  // Đè lâu: Ném xa
    [Tooltip("Thời gian gồng tối đa. Nếu giữ quá thời gian này sẽ tự bắn.")]
    public float maxChargeTime = 5.0f;

    [Header("--- 3. Drop Physics (Rơi Đầm) ---")]
    [Tooltip("Độ cản gió khi rơi (Càng cao càng ít trôi, rơi 'đầm' hơn)")]
    public float dropLinearDamping = 1.0f;
    public float dropAngularDamping = 1.0f;
    public float objectSpin = 2.0f; // Độ xoay ngẫu nhiên khi ném

    [Header("--- 4. Inventory 3D Slots (Logic) ---")]
    [Tooltip("Kéo 4 Empty Object dưới lòng đất vào đây")]
    public Transform[] inventorySlots;

    [Header("--- 5. UI Display (Visual) ---")]
    [Tooltip("Kéo 4 cái RawImage/Border đại diện cho 4 ô túi đồ vào đây")]
    public RectTransform[] slotUIFrames;
    public float selectedScale = 1.2f;   // Kích thước khi được chọn
    public float normalScale = 1.0f;     // Kích thước bình thường
    public float uiScaleSpeed = 10f;     // Tốc độ hiệu ứng UI

    [Header("--- 6. External References ---")]
    public Image progressCircle;       // Vòng tròn Loading (Nhặt & Ném)
    public TextMeshProUGUI promptText; // Dòng chữ hiện tên đồ
    public WeatherManager weatherManager; // Để cập nhật số lượng đồ sắt

    // ========================================================================
    // 2. PRIVATE VARIABLES & PROPERTIES
    // ========================================================================
    private Camera playerCam;
    private ItemController[] inventoryItems; // Danh sách đồ thực tế đang cầm
    private ItemController targetItem;       // Món đồ đang nhìn thấy

    private int currentSlotIndex = 0;

    // Biến xử lý timer
    private float pickupTimer = 0f;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;

    // Các biến Public Property để script khác đọc dữ liệu
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
    }

    void Update()
    {
        HandleInteraction();    // Xử lý nhìn đồ
        HandleInput();          // Xử lý phím bấm (Nhặt & Ném)
        HandleSlotSelectionUI();// Hiệu ứng phóng to ô túi
        RotateInventoryItems(); // Xoay đồ trong túi 3D
        UpdateStats();          // Cập nhật thông số cân nặng/tiền
    }

    // ========================================================================
    // 4. INPUT & INTERACTION LOGIC
    // ========================================================================
    void HandleInteraction()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Debug tia Ray đỏ trong Scene View
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

        // Nếu không nhìn thấy gì
        targetItem = null;
        if (promptText) promptText.gameObject.SetActive(false);

        // Reset timer nhặt nếu không nhìn vào đồ (nhưng không reset nếu đang gồng ném)
        if (!isChargingThrow && pickupTimer > 0)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }
    }

    void HandleInput()
    {
        // --- A. CHỌN SLOT (1-4) & CUỘN CHUỘT ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlotIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlotIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlotIndex = 3;
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, inventorySlots.Length - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) currentSlotIndex = (currentSlotIndex + 1) % inventorySlots.Length;
        if (scroll < 0) currentSlotIndex = (currentSlotIndex - 1 + inventorySlots.Length) % inventorySlots.Length;

        // --- B. NHẶT ĐỒ (HOLD E) ---
        // Chỉ cho phép nhặt khi KHÔNG đang gồng ném
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
                    currentSlotIndex = emptyIndex; // Tự chuyển sang slot mới
                    PickupItem(targetItem, emptyIndex);

                    // Reset
                    pickupTimer = 0;
                    targetItem = null;
                    if (progressCircle) progressCircle.fillAmount = 0;
                    if (promptText) promptText.gameObject.SetActive(false);
                }
            }
            else
            {
                if (promptText) promptText.text = "<color=red>INVENTORY FULL!</color>";
            }
        }
        else if (!isChargingThrow) // Nhả E thì reset vòng tròn
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }

        // --- C. NÉM GỒNG LỰC (CHARGE THROW Q) ---

        // 1. BẮT ĐẦU GỒNG
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (inventoryItems[currentSlotIndex] != null)
            {
                isChargingThrow = true;
                throwChargeTimer = 0f;
            }
        }

        // 2. ĐANG GIỮ Q
        if (Input.GetKey(KeyCode.Q) && isChargingThrow)
        {
            throwChargeTimer += Time.deltaTime;

            // Hiển thị lực ném lên vòng tròn
            float chargePercent = Mathf.Clamp01(throwChargeTimer / maxChargeTime);
            if (progressCircle) progressCircle.fillAmount = chargePercent;

            // [TỰ ĐỘNG BẮN] Nếu giữ quá 5s
            if (throwChargeTimer >= maxChargeTime)
            {
                DropItem(currentSlotIndex, maxThrowForce);

                // Reset ngay lập tức
                isChargingThrow = false;
                throwChargeTimer = 0f;
                if (progressCircle) progressCircle.fillAmount = 0;
                return; // Thoát hàm để không chạy phần KeyUp bên dưới
            }
        }

        // 3. THẢ Q (Ném theo lực đã gồng)
        if (Input.GetKeyUp(KeyCode.Q) && isChargingThrow)
        {
            float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, throwChargeTimer / maxChargeTime);
            DropItem(currentSlotIndex, finalForce);

            // Reset
            isChargingThrow = false;
            throwChargeTimer = 0f;
            if (progressCircle) progressCircle.fillAmount = 0;
        }
    }

    // ========================================================================
    // 5. ITEM HANDLING (PICKUP / DROP)
    // ========================================================================
    void PickupItem(ItemController item, int slotIndex)
    {
        inventoryItems[slotIndex] = item;

        item.transform.SetParent(inventorySlots[slotIndex]);
        item.transform.localPosition = item.data.inventoryPositionOffset;
        item.transform.localRotation = Quaternion.Euler(item.data.inventoryRotationOffset);
        item.transform.localScale = Vector3.one * item.data.inventoryScale;

        item.SetState(true); // Tắt vật lý, đổi layer InventoryRender
    }

    void DropItem(int slotIndex, float forceToApply)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;
        ItemController item = inventoryItems[slotIndex];
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.position = dropPoint.position;
        item.transform.localScale = Vector3.one;

        item.SetState(false); // Bật lại vật lý

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reset vận tốc cũ
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Tăng lực cản để rơi đầm hơn
            rb.linearDamping = dropLinearDamping;
            rb.angularDamping = dropAngularDamping;

            // Hướng ném: Theo hướng nhìn + hơi chếch lên 1 chút
            Vector3 throwDir = (playerCam.transform.forward + Vector3.up * 0.2f).normalized;

            rb.AddForce(throwDir * forceToApply, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * objectSpin, ForceMode.Impulse);
        }

        inventoryItems[slotIndex] = null;
    }

    // ========================================================================
    // 6. HELPER FUNCTIONS & UI
    // ========================================================================
    int GetEmptySlot()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null) return i;
        }
        return -1;
    }

    void HandleSlotSelectionUI()
    {
        if (slotUIFrames == null) return;

        for (int i = 0; i < slotUIFrames.Length; i++)
        {
            if (slotUIFrames[i] == null) continue;

            float target = (i == currentSlotIndex) ? selectedScale : normalScale;

            // Lerp mượt mà
            Vector3 newScale = Vector3.Lerp(slotUIFrames[i].localScale, Vector3.one * target, Time.deltaTime * uiScaleSpeed);
            slotUIFrames[i].localScale = newScale;
        }
    }

    void RotateInventoryItems()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] != null)
            {
                inventoryItems[i].transform.Rotate(Vector3.up * 30f * Time.deltaTime);
            }
        }
    }

    void UpdateStats()
    {
        float w = 0;
        int v = 0;
        int c = 0;
        int ironCount = 0;

        foreach (var item in inventoryItems)
        {
            if (item != null)
            {
                w += item.data.weight;
                v += item.scrapValue;
                c++;

                if (item.data.itemType == ItemType.IronSmall || item.data.itemType == ItemType.IronLarge)
                {
                    ironCount++;
                }
            }
        }

        TotalWeight = w;
        TotalValue = v;
        TotalItemCount = c;

        if (weatherManager) weatherManager.ironItemCount = ironCount;
    }
}