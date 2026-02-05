using StarterAssets;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ActiveWeapon : MonoBehaviour
{
    public Transform crossHairTarget;
    public bool IsHolstered { get; private set; } = true;

    [Header("Weapon")]
    public Transform weaponParent;
    public Animator rigController;

    [Header("Holster Sockets")]
    public Transform backSocket;
    public Transform hipSocket;

    [Header("Settings")]
    public float fireDelayTime = 0.5f;

    private RaycastWeapon weapon;
    public RaycastWeapon CurrentWeapon => weapon;
    private StarterAssetsInputs _input;
    private ThirdPersonController _controller;
    private float _nextFireTime = 0f;
    private bool _wasHolstered = true;

    private void Start()
    {
        rigController.updateMode = AnimatorUpdateMode.Fixed;
        rigController.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        rigController.animatePhysics = true;
        rigController.updateMode = AnimatorUpdateMode.Normal;

        _controller = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        RaycastWeapon existingWeapon = GetComponentInChildren<RaycastWeapon>();
        if (existingWeapon)
            Equip(existingWeapon);
    }

    private void Update()
    {
        if (weapon)
        {
            ReloadControl();
            ShootControl();
            weapon.UpdateBullets(Time.deltaTime);
        }
        else
        {
            IsHolstered = true;
        }
    }

    private void ReloadControl()
    {
        if (IsHolstered || weapon.isReloading) return;

        bool shouldReload = false;

        if (_input.reload)
        {
            shouldReload = true;
            _input.reload = false;
        }

        if (_input.shoot && weapon.ammoConfig.currentClipAmmo <= 0)
        {
            shouldReload = true;
        }

        if (shouldReload)
        {
            if (weapon.CanReload())
            {
                weapon.StartReload();
                rigController.SetTrigger("reload_weapon");
            }
        }
    }

    private void ShootControl()
    {
        IsHolstered = rigController.GetBool("holster_weapon");

        if (_wasHolstered == true && IsHolstered == false) _nextFireTime = Time.time + fireDelayTime;
        _wasHolstered = IsHolstered;

        if (_input.shoot && !IsHolstered && Time.time >= _nextFireTime && !weapon.isReloading)
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
            if (!weapon.isReloading)
            {
                bool currentHolsterState = rigController.GetBool("holster_weapon");
                rigController.SetBool("holster_weapon", !currentHolsterState);
            }
            _input.holster = false;
        }
    }

    public void Equip(RaycastWeapon newWeapon)
    {
        if (weapon) Destroy(weapon.gameObject);
        weapon = newWeapon;
        weapon.raycastDestination = crossHairTarget;

        if (weapon.recoil != null && _controller != null) weapon.recoil.playerController = _controller;
        if (weapon.recoil != null) weapon.recoil.rigCotroller = rigController;

        weapon.transform.SetParent(weaponParent);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        rigController.Play("equip_" + weapon.weaponName);
        rigController.SetBool("holster_weapon", false);
        IsHolstered = false;
        _wasHolstered = true;
    }

    public void OnWeaponHolster()
    {
        if (weapon != null)
        {
            Transform target = weapon.holsterLocation == RaycastWeapon.HolsterLocation.Back ? backSocket : hipSocket;
            if (target) { weapon.transform.SetParent(target); weapon.transform.localPosition = Vector3.zero; weapon.transform.localRotation = Quaternion.identity; }
        }
    }
    public void OnWeaponEquip()
    {
        if (weapon != null) { weapon.transform.SetParent(weaponParent); weapon.transform.localPosition = Vector3.zero; weapon.transform.localRotation = Quaternion.identity; }
    }

    // Hàm này sẽ được gọi bởi Relay (hoặc trực tiếp nếu Animator nằm trên cùng object)
    public void OnWeaponReload()
    {
        if (weapon != null)
        {
            weapon.RefillAmmo(); // Cộng đạn vào băng và kết thúc trạng thái reloading
        }
    }
}