using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAction : MonoBehaviour
{
    [Header("Aiming")]
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;

    [SerializeField] PlayerGunSelector gunSelector;
    [SerializeField] bool autoReload = true;

    [SerializeField] private Animator playerAnimator;
    //[SerializeField] private PlayerIK invenseKeinematics;

    private bool isReloading;

    private StarterAssetsInputs starterAssetsInputs;
    private ThirdPersonController thirdPersonController;

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
        Aiming();
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

    private void Aiming()
    {
        Vector3 mouseWorldPosition = Vector3.zero;

        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit rayCastHit, 999f, aimColliderLayerMask))
        {
            debugTransform.position = rayCastHit.point;
            mouseWorldPosition = rayCastHit.point;
        }

        if (starterAssetsInputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            if (thirdPersonController != null)
            {
                thirdPersonController.SetSensitivity(aimSensitivity);
                thirdPersonController.SetRotateOnMove(false);
            }

            playerAnimator.SetLayerWeight(1, Mathf.Lerp(playerAnimator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            if (thirdPersonController != null)
            {
                thirdPersonController.SetSensitivity(normalSensitivity);
                thirdPersonController.SetRotateOnMove(true);
            }
            playerAnimator.SetLayerWeight(1, Mathf.Lerp(playerAnimator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

        }
    }
}
