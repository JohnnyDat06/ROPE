using StarterAssets;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditor.Animations;

public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] private Rig HandIK;
    public Transform crossHairTarget;
    private bool isAiming = false;

    [Header("Weapon")]
    [SerializeField] private Transform weaponLeftGrip;
    [SerializeField] private Transform weaponRightGrip;
    public Transform weaponParent;

    private RaycastWeapon weapon;
    private Animator anim;
    private AnimatorOverrideController overrides;
    private StarterAssetsInputs _input;

    private void Start()
    {
        anim = GetComponent<Animator>();
        overrides = anim.runtimeAnimatorController as AnimatorOverrideController;

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

            weapon.UpdateBullets(Time.deltaTime);
        }
        else
        {
            isAiming = false;
            HandIK.weight = 0f;
            anim.SetLayerWeight(1, 0f);
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

        anim.SetLayerWeight(1, 1f);
        HandIK.weight = 1f;

        Invoke("SetAnimationDelayed", 0.001f);
    }

    private void SetAnimationDelayed()
    {
        if (weapon.weaponAnimation != null)
        {
            overrides["Weapon_Aim_Empty"] = weapon.weaponAnimation;
            Debug.Log($"Đang thay đổi animation sang: {weapon.weaponAnimation.name}");
        }
        else
        {
            Debug.LogError("Weapon Animation đang bị Null! Hãy kiểm tra Prefab súng.");
        }
    }

    [ContextMenu("Save Weapon Pose")]
    private void SaveWeaponPose()
    {
        GameObjectRecorder rerecorder = new GameObjectRecorder(gameObject);
        rerecorder.BindComponentsOfType<Transform>(weaponParent.gameObject, false);
        rerecorder.BindComponentsOfType<Transform>(weaponLeftGrip.gameObject, false);
        rerecorder.BindComponentsOfType<Transform>(weaponRightGrip.gameObject, false);
        rerecorder.TakeSnapshot(0f);
        rerecorder.SaveToClip(weapon.weaponAnimation);
        UnityEditor.AssetDatabase.SaveAssets();
    }
}
