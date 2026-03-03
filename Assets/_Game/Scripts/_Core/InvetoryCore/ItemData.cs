using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public float weight;
    public float pickupDuration = 1.0f; 
    public GameObject modelPrefab; 

    [Header("Render Settings")]
    public Vector3 inventoryPositionOffset;
    public Vector3 inventoryRotationOffset;
    public float inventoryScale = 1f;
}