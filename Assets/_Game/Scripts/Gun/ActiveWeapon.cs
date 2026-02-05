using StarterAssets;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static RaycastWeapon;

public class ActiveWeapon : MonoBehaviour
{
    public Transform crossHairTarget;
    public bool IsHolstered { get; private set; } = true;

    [Header("Weapon")]
    [SerializeField] private Transform weaponLeftGrip;
    [SerializeField] private Transform weaponRightGrip;
    public Transform weaponParent; 
    public Animator rigController;

    [Header("Holster Sockets (Body)")]
    public Transform backSocket;
    public Transform hipSocket;

    [Header("Settings")]
    public float fireDelayTime = 0.5f;

    private RaycastWeapon weapon;
    private StarterAssetsInputs _input;
    private float _nextFireTime = 0f;
    private bool _wasHolstered = true;

    private void Start()
    {
        rigController.updateMode = AnimatorUpdateMode.Fixed;
        rigController.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        rigController.animatePhysics = true;
        rigController.updateMode = AnimatorUpdateMode.Normal;

        _input = GetComponent<StarterAssetsInputs>();
        RaycastWeapon existingWeapon = GetComponentInChildren<RaycastWeapon>();
        if (existingWeapon)
            Equip(existingWeapon);
    }

    private void Update()
    {
        ShootControl();
    }

    private void ShootControl()
    {
        if (weapon)
        {
            IsHolstered = rigController.GetBool("holster_weapon");

            if (_wasHolstered == true && IsHolstered == false)
            {
                _nextFireTime = Time.time + fireDelayTime;
            }
            _wasHolstered = IsHolstered;

            if (_input.shoot && !IsHolstered && Time.time >= _nextFireTime)
            {
                if (!weapon.isFiring) weapon.StartFiring();
                weapon.UpdateFiring(Time.deltaTime);
            }
            else
            {
                if (weapon.isFiring) weapon.StopFiring();
            }

            if (_input.holster)
            {
                bool currentHolsterState = rigController.GetBool("holster_weapon");
                rigController.SetBool("holster_weapon", !currentHolsterState);
                _input.holster = false;
            }
            weapon.UpdateBullets(Time.deltaTime);
        }
        else
        {
            IsHolstered = true;
        }
    }

    public void Equip(RaycastWeapon newWeapon)
    {
        if (weapon) Destroy(weapon.gameObject);

        weapon = newWeapon;
        weapon.raycastDestination = crossHairTarget;

        weapon.transform.SetParent(weaponParent);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        rigController.Play("equip_" + weapon.weaponName);
        rigController.SetBool("holster_weapon", false);
        IsHolstered = false;
        _wasHolstered = true;
    }

    public void OnWeaponEquip()
    {
        if (weapon != null)
        {
            weapon.transform.SetParent(weaponParent);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
    }

    public void OnWeaponHolster()
    {
        if (weapon == null) return;

        Transform targetSocket = null;

        // Kiểm tra xem khẩu súng hiện tại muốn về đâu
        switch (weapon.holsterLocation)
        {
            case HolsterLocation.Back:
                targetSocket = backSocket;
                break;
            case HolsterLocation.Hip:
                targetSocket = hipSocket;
                break;
        }

        // Thực hiện chuyển cha (Parent)
        if (targetSocket != null)
        {
            weapon.transform.SetParent(targetSocket);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("Chưa gán Socket cho vị trí này trong Inspector!");
        }
    }
}