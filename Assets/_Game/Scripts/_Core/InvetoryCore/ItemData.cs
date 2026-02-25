using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public float weight;
    public float pickupDuration = 1.0f; // Thời gian đè E để nhặt
    public GameObject modelPrefab; // Prefab gốc

    [Header("Render Settings")]
    public Vector3 inventoryPositionOffset; // Tinh chỉnh vị trí khi hiển thị trong Inventory 3D
    public Vector3 inventoryRotationOffset;
    public float inventoryScale = 1f;
}