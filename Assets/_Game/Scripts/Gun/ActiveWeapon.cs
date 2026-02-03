using StarterAssets;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditor.Animations;

public class ActiveWeapon : MonoBehaviour
{
    public Transform crossHairTarget;
    private bool isAiming = false;

    [Header("Weapon")]
    [SerializeField] private Transform weaponLeftGrip;
    [SerializeField] private Transform weaponRightGrip;
    public Transform weaponParent;
    public Animator rigController;

    private RaycastWeapon weapon;
    private StarterAssetsInputs _input;

    private void Start()
    {
        // Use AnimatorUpdateMode.Fixed instead of the obsolete AnimatePhysics, make sure weights are set to 1 on rig constraints
        rigController.updateMode = AnimatorUpdateMode.Fixed;
        rigController.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        rigController.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        rigController.updateMode = AnimatorUpdateMode.Normal;
        rigController.animatePhysics = true;

        _input = GetComponent<StarterAssetsInputs>();
        RaycastWeapon existingWeapon = GetComponentInChildren<RaycastWeapon>();
        if (existingWeapon)
            Equip(existingWeapon);
    }

    private void Update()
    {
        ShootControl();
    }

    public bool IsAiming() => isAiming;
    private void ShootControl()
    {
        if (weapon)
        {
            isAiming = true;
            if (_input.shoot)
            {
                if (!weapon.isFiring)
                    weapon.StartFiring();

                weapon.UpdateFiring(Time.deltaTime);
            }
            else
                if (weapon.isFiring)
                weapon.StopFiring();

            if (_input.holster)
            {
                bool isHolstering = rigController.GetBool("holster_weapon");
                rigController.SetBool("holster_weapon", !isHolstering);
                _input.holster = false;
            }

            weapon.UpdateBullets(Time.deltaTime);
        }
    }

    public void Equip(RaycastWeapon newWeapon)
    {
        if (weapon)
            Destroy(weapon.gameObject);

        weapon = newWeapon;
        weapon.raycastDestination = crossHairTarget;
        weapon.transform.parent = weaponParent;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        rigController.Play("equip_" + weapon.weaponName);
    }
}
