using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAction : MonoBehaviour
{
    [SerializeField] PlayerGunSelector gunSelector;
    [SerializeField] bool autoReload = true;

    [SerializeField] private Animator playerAnimator;
    //[SerializeField] private PlayerIK invenseKeinematics;

    private bool isReloading;

    private StarterAssetsInputs starterAssetsInputs;

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        if (gunSelector.activeGun != null)
        {
            gunSelector.activeGun.Tick(Application.isFocused && starterAssetsInputs.shoot);
        }

        if (ShouldAutoReload() || ShouldManualReload())
        {
            gunSelector.activeGun.StartReloading();
            isReloading = true;
            playerAnimator.SetTrigger("Reload");
            //invenseKeinematics.HandIKAmount = 0.25;
            //invenseKeinematics.EblowIKAmount = 0.25;

        }
    }

    public void EndReload()
    {
        gunSelector.activeGun.EndReload();
        //invenseKeinematics.HandIKAmount = 1;
        //invenseKeinematics.EblowIKAmount = 1;
        isReloading = false;
    }

    private bool ShouldManualReload()
    {
        return !isReloading &&
               starterAssetsInputs.reload &&
               gunSelector.activeGun.CanReload();
    }

    private bool ShouldAutoReload()
    {
        return !isReloading &&
               autoReload &&
               gunSelector.activeGun.ammoConfig.currentClipAmmo <= 0 &&
               gunSelector.activeGun.CanReload();
    }
}
