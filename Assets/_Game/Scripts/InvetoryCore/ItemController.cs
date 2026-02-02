using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemController : MonoBehaviour
{
    public ItemData data;
    [HideInInspector] public int scrapValue; // Giá trị tiền thực tế của món này

    private Rigidbody rb;
    private Collider col;
    private int originalLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        originalLayer = gameObject.layer;
    }

    public void InitializeValue()
    {
        // Random giá trị dựa trên Type theo yêu cầu
        switch (data.itemType)
        {
            case ItemType.Small: scrapValue = Random.Range(1, 11); break;
            case ItemType.Large: scrapValue = Random.Range(11, 37); break;
            case ItemType.IronSmall: scrapValue = Random.Range(16, 45); break;
            case ItemType.IronLarge: scrapValue = Random.Range(50, 101); break;
            case ItemType.Special: scrapValue = Random.Range(500, 1000); break;
        }
    }

    // Chuyển đổi trạng thái khi vào/ra Inventory
    public void SetState(bool inInventory)
    {
        rb.isKinematic = inInventory;
        col.enabled = !inInventory;

        if (inInventory)
        {
            // Tắt vật lý, set layer sang "InventoryRender" để Camera riêng nhìn thấy
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
}