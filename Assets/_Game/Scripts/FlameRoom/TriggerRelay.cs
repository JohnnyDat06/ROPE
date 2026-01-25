using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    // Kéo thằng cha (PressurePlate_Group) vào đây
    [SerializeField] private PressurePlate plateLogic;

    private void OnTriggerEnter(Collider other)
    {
        // Gọi hàm của cha
        plateLogic.OnObjectEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // Gọi hàm của cha
        plateLogic.OnObjectExit(other);
    }
}