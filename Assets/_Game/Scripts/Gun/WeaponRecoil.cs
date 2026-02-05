using StarterAssets;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [HideInInspector] public Cinemachine.CinemachineFreeLook playerCamera;
    [HideInInspector] public ThirdPersonController playerController;
    [HideInInspector] public Animator rigCotroller;
    public float verticalRecoil;
    public float horizoltalRecoil;

    public void GenerateRecoil(string weaponName)
    {
        if (playerController != null)
        {
            playerController.AddRecoil(verticalRecoil, horizoltalRecoil);
        }

        rigCotroller.Play("Weapon_Recoil_" + weaponName, 1, 0f);
    }
}
