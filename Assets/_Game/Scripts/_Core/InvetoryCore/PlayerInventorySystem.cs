using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class PlayerInventorySystem : MonoBehaviour
{
    // ========================================================================
    // 1. SETTINGS & REFERENCES
    // ========================================================================
    [Header("--- 1. Interaction Settings ---")]
    public float interactDistance = 6.0f;
    public LayerMask itemLayer;
    public Transform dropPoint;

    [Header("--- 2. Charge Throw (Ném Gồng Lực) ---")]
    public float minThrowForce = 2.0f;
    public float maxThrowForce = 25.0f;
    public float maxChargeTime = 3.0f;

    [Header("--- 3. Drop Physics (Rơi Đầm) ---")]
    public float dropLinearDamping = 1.0f;
    public float dropAngularDamping = 1.0f;
    public float objectSpin = 5.0f;

    [Header("--- 4. Inventory 3D Slots ---")]
    public Transform[] inventorySlots;

    [Header("--- 5. UI Display ---")]
    public RectTransform[] slotUIFrames;
    public float selectedScale = 1.2f;
    public float normalScale = 1.0f;
    public float uiScaleSpeed = 10f;

    [Header("--- 6. UI References ---")]
    public Image progressCircle;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI totalValueText;

    [Header("--- 7. External Systems ---")]
    public WeatherManager weatherManager;

    // ========================================================================
    // 8. CUTSCENE & LEVEL TRANSITION SETTINGS
    // ========================================================================
    [Header("--- 8. Cutscene & Transition Settings ---")]
    [Tooltip("Layer của bức tường để check chuyển map (Ví dụ: Map2)")]
    public LayerMask map2Layer;
    [Tooltip("Tên chính xác của vật phẩm KeyCard trong ItemData")]
    public string keyCardName = "KeyCard";
    [Tooltip("Tên Scene bạn muốn chuyển đến sau khi hết Video")]
    public string nextSceneName = "Map2_Scene";

    [Header("--- Video References ---")]
    public VideoPlayer cutscenePlayer;
    public GameObject videoUIContainer;

    // ========================================================================
    // 2. PRIVATE VARIABLES & PROPERTIES
    // ========================================================================
    private Camera playerCam;
    private ItemController[] inventoryItems;
    private ItemController targetItem;

    private int currentSlotIndex = 0;
    private float pickupTimer = 0f;
    private float throwChargeTimer = 0f;
    private bool isChargingThrow = false;

    private bool isLookingAtMap2Wall = false;
    private bool isPlayingCutscene = false;

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

        if (progressCircle) progressCircle.fillAmount = 0;
        if (promptText) promptText.gameObject.SetActive(false);
        if (totalValueText) totalValueText.text = "TOTAL: $0";

        if (videoUIContainer) videoUIContainer.SetActive(false);
    }

    void Update()
    {
        if (isPlayingCutscene) return;

        HandleInteraction();
        HandleInput();
        HandleSlotSelectionUI();
        RotateInventoryItems();
        UpdateStats();
    }

    // ========================================================================
    // 4. INPUT & INTERACTION LOGIC
    // ========================================================================
    void HandleInteraction()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

        LayerMask combinedMask = itemLayer | map2Layer;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, combinedMask))
        {
            if (((1 << hit.collider.gameObject.layer) & itemLayer) != 0)
            {
                isLookingAtMap2Wall = false;

                if (hit.collider.TryGetComponent(out ItemController item))
                {
                    targetItem = item;
                    if (promptText)
                    {
                        // ĐÃ BỎ MÀU: Hiển thị text thô cơ bản
                        promptText.text = $"{item.data.itemName} (${item.scrapValue})\n[Press E]";
                        promptText.gameObject.SetActive(true);
                    }
                    return;
                }
            }
            else if (((1 << hit.collider.gameObject.layer) & map2Layer) != 0)
            {
                targetItem = null;
                isLookingAtMap2Wall = true;

                if (promptText)
                {
                    if (CheckHasKeyCard())
                    {
                        // ĐÃ BỎ MÀU
                        promptText.text = "Use KeyCard\n[Press E]";
                    }
                    else
                    {
                        // ĐÃ BỎ MÀU
                        promptText.text = "Locked\n[Requires KeyCard]";
                    }
                    promptText.gameObject.SetActive(true);
                }
                return;
            }
        }

        targetItem = null;
        isLookingAtMap2Wall = false;
        if (promptText) promptText.gameObject.SetActive(false);

        if (!isChargingThrow && pickupTimer > 0)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlotIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlotIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlotIndex = 3;
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, inventorySlots.Length - 1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) currentSlotIndex = (currentSlotIndex + 1) % inventorySlots.Length;
        if (scroll < 0) currentSlotIndex = (currentSlotIndex - 1 + inventorySlots.Length) % inventorySlots.Length;

        if (Input.GetKeyDown(KeyCode.E) && !isChargingThrow)
        {
            if (isLookingAtMap2Wall)
            {
                if (CheckHasKeyCard())
                {
                    StartCoroutine(PlayCutsceneAndTransition());
                }
                return;
            }

            if (targetItem != null)
            {
                int emptyIndex = GetEmptySlot();
                if (emptyIndex != -1)
                {
                    currentSlotIndex = emptyIndex;
                    PickupItem(targetItem, emptyIndex);

                    targetItem = null;
                    if (progressCircle) progressCircle.fillAmount = 0;
                    if (promptText) promptText.gameObject.SetActive(false);
                }
                else if (promptText)
                {
                    // ĐÃ BỎ MÀU
                    promptText.text = "FULL!";
                }
            }
        }
        else if (!isChargingThrow)
        {
            pickupTimer = 0;
            if (progressCircle) progressCircle.fillAmount = 0;
        }

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
    // 5. CUTSCENE & SCENE TRANSITION LOGIC 
    // ========================================================================
    bool CheckHasKeyCard()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] != null && inventoryItems[i].data != null)
            {
                if (inventoryItems[i].data.itemName == keyCardName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator PlayCutsceneAndTransition()
    {
        isPlayingCutscene = true;

        if (promptText) promptText.gameObject.SetActive(false);

        if (cutscenePlayer != null && videoUIContainer != null)
        {
            videoUIContainer.SetActive(true);
            cutscenePlayer.Prepare();

            while (!cutscenePlayer.isPrepared)
            {
                yield return null;
            }

            cutscenePlayer.Play();
            yield return new WaitForSeconds(6f);
        }
        else
        {
            Debug.LogWarning("Chưa gán VideoPlayer hoặc Video UI Container. Đợi 4s rồi chuyển map luôn.");
            yield return new WaitForSeconds(4f);
        }

        Debug.Log("Loading next scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    // ========================================================================
    // 6. ITEM HANDLING
    // ========================================================================
    void PickupItem(ItemController item, int slotIndex)
    {
        inventoryItems[slotIndex] = item;
        item.transform.SetParent(inventorySlots[slotIndex]);

        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float targetSize = 1.4f;

            if (maxDimension > 0.001f)
            {
                float autoScale = targetSize / maxDimension;
                item.transform.localScale = Vector3.one * autoScale;
            }

            Bounds newBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) newBounds.Encapsulate(renderers[i].bounds);

            Vector3 offsetToCenter = inventorySlots[slotIndex].position - newBounds.center;
            item.transform.position += offsetToCenter;
        }

        item.transform.localRotation *= Quaternion.Euler(item.data.inventoryRotationOffset);
        item.transform.localPosition += item.data.inventoryPositionOffset;

        item.SetState(true);
    }

    void DropItem(int slotIndex, float forceToApply)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;
        ItemController item = inventoryItems[slotIndex];
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.localScale = item.originalScale;

        bool isGentleDrop = (forceToApply <= minThrowForce * 1.5f);

        if (isGentleDrop)
        {
            Vector3 originalEuler = item.originalRotation.eulerAngles;
            Vector3 camEuler = playerCam.transform.eulerAngles;
            item.transform.rotation = Quaternion.Euler(originalEuler.x, camEuler.y, originalEuler.z);
        }
        else
        {
            item.transform.rotation = item.originalRotation;
        }

        float itemHalfHeight = 0.25f;
        Collider col = item.GetComponent<Collider>();
        if (col != null) itemHalfHeight = col.bounds.extents.y;

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
        item.SetState(false);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = dropLinearDamping;
            rb.angularDamping = dropAngularDamping;

            if (!isGentleDrop)
            {
                Vector3 throwDir = (playerCam.transform.forward + Vector3.up * 0.2f).normalized;
                rb.AddForce(throwDir * forceToApply, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * objectSpin, ForceMode.Impulse);
            }
        }

        inventoryItems[slotIndex] = null;
    }

    // ========================================================================
    // 7. HELPER FUNCTIONS & UI
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
            if (inventoryItems[i] != null) inventoryItems[i].transform.Rotate(Vector3.up * 30f * Time.deltaTime, Space.World);
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

                if (item.data.itemType == ItemType.IronSmall) lightningChance += 0.01f;
                else if (item.data.itemType == ItemType.IronLarge) lightningChance += 0.02f;
            }
        }

        TotalWeight = w;
        TotalValue = v;
        TotalItemCount = c;

        if (totalValueText != null) totalValueText.text = $"TOTAL: ${TotalValue}";
        if (weatherManager) weatherManager.currentStrikeChance = lightningChance;
    }
}