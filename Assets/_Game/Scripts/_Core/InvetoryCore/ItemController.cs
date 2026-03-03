using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ItemController : MonoBehaviour
{
    public ItemData data;
    [HideInInspector] public int scrapValue;

    // --- 2 BIẾN LƯU THÔNG SỐ GỐC ---
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public Quaternion originalRotation; // Biến xoay góc chuẩn

    // --- BIẾN CHO QUICK OUTLINE ---
    private Outline itemOutline;
    private Coroutine highlightCoroutine;

    private Rigidbody rb;
    private Collider col;
    private int originalLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        originalLayer = gameObject.layer;

        // Lưu lại thông số chuẩn trên map trước khi bị nhặt vào túi
        originalScale = transform.localScale;
        originalRotation = transform.rotation;

        // =========================================================
        // TỰ ĐỘNG THÊM VÀ SETUP QUICK OUTLINE CHO ITEM
        // =========================================================
        itemOutline = GetComponent<Outline>();
        if (itemOutline == null)
        {
            itemOutline = gameObject.AddComponent<Outline>();
        }

        // Tùy chỉnh chế độ Xuyên Tường (Giống Genshin/Wuwa)
        itemOutline.OutlineMode = Outline.Mode.OutlineAll;

        // Chọn màu sắc viền (Ví dụ: Xanh ngọc Cyan rực rỡ)
        itemOutline.OutlineColor = new Color(0f, 1f, 1f, 1f);

        // Độ dày của viền (Chỉnh to lên nếu muốn dễ nhìn hơn)
        itemOutline.OutlineWidth = 5f;

        // Mặc định tắt đi, chỉ bật khi máy quét chạm tới
        itemOutline.enabled = false;
    }

    public void InitializeValue()
    {
        if (data == null) return;

        switch (data.itemType)
        {
            case ItemType.Small: scrapValue = Random.Range(10, 30); break;
            case ItemType.Large: scrapValue = Random.Range(20, 46); break;
            case ItemType.IronSmall: scrapValue = Random.Range(16, 45); break;
            case ItemType.IronLarge: scrapValue = Random.Range(50, 71); break;
            case ItemType.Special: scrapValue = Random.Range(72, 100); break;
        }
    }

    public void SetState(bool inInventory)
    {
        rb.isKinematic = inInventory;
        col.enabled = !inInventory;

        if (inInventory)
        {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("InventoryRender"));
        }
        else
        {
            SetLayerRecursively(gameObject, originalLayer);
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    // ==========================================
    // --- HÀM BẬT HIGHLIGHT ĐƯỢC GỌI TỪ MÁY QUẾT ---
    // ==========================================
    public void TriggerHighlight()
    {
        // Bỏ qua nếu đồ đang nằm trong túi đồ (tắt Collider) hoặc không có Outline
        if (!col.enabled || itemOutline == null) return;

        // Reset lại thời gian sáng nếu bị sóng quét trúng liên tục
        if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
        highlightCoroutine = StartCoroutine(HighlightRoutine());
    }

    private IEnumerator HighlightRoutine()
    {
        
        itemOutline.enabled = true;

        yield return new WaitForSeconds(2.0f);

        itemOutline.enabled = false;
    }
}