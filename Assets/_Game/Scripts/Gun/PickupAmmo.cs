using UnityEngine;

public class PickupAmmo : MonoBehaviour
{
    [Header("Cấu hình")]
    // if want to limit to specific ammo type, uncomment below line
    //[SerializeField] private AmmoConfigSO validAmmoConfig; 

    [SerializeField] private int ammoAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        ActiveWeapon activeWeapon = other.gameObject.GetComponent<ActiveWeapon>();
        
        if (activeWeapon != null)
        {
            RaycastWeapon currentWeapon = activeWeapon.GetCurrentWeapon();

            if (currentWeapon != null /*&& currentWeapon.ammoConfig == validAmmoConfig*/)
            {
                currentWeapon.ammoConfig.currentAmmo += ammoAmount;

                Debug.Log($"Đã thêm {ammoAmount} viên đạn vào hộp đạn {currentWeapon.weaponName}");

                Destroy(gameObject);
            }
        }
    }
}