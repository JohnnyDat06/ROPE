using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public RaycastWeapon weaponPrefab;

    private void OnTriggerEnter(Collider other)
    {
       ActiveWeapon activeWeapon = other.gameObject.GetComponent<ActiveWeapon>();
        if (activeWeapon)
         {
              RaycastWeapon newWeapon = Instantiate(weaponPrefab);
              activeWeapon.Equip(newWeapon);
        }
    }
}
