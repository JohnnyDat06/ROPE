using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    [Header("Liên kết")]
    [Tooltip("Kéo script PressurePlate từ object cha vào đây")]
    [SerializeField] private PressurePlate plateLogic;

    private void OnTriggerEnter(Collider other)
    {
        if (plateLogic) plateLogic.OnObjectEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (plateLogic) plateLogic.OnObjectExit(other);
    }
}