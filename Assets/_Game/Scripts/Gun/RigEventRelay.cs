using UnityEngine;

public class RigEventRelay : MonoBehaviour
{
    private ActiveWeapon activeWeapon;

    private void Start()
    {
        activeWeapon = GetComponentInParent<ActiveWeapon>();

        if (activeWeapon == null)
        {
            Debug.LogError("Không tìm thấy script ActiveWeapon ở object cha!");
        }
    }

    public void TriggerEquip()
    {
        if (activeWeapon != null)
        {
            activeWeapon.OnWeaponEquip();
        }
    }

    public void TriggerHolster()
    {
        if (activeWeapon != null)
        {
            activeWeapon.OnWeaponHolster();
        }
    }
}