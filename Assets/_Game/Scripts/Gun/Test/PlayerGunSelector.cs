using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent] // Ensure only one instance of this component per GameObject
public class PlayerGunSelector : MonoBehaviour
{
    [SerializeField] private GunType gunType;
    [SerializeField] private Transform gunParent;
    [SerializeField] private List<GunSO> guns;
    //[SerializeField] private PlayerIK invenseKinematics;

    [Space]
    [Header("Runtime Filled")]
    public GunSO activeGun;

    private void Start()
    {
        GunSO gun = guns.Find(g => g.type == gunType);

        if (gun == null)
        {
            Debug.LogError($"No gun of type {gunType} found in PlayerGunSelector on {gameObject.name}");
            return;
        }

        activeGun = gun;
        activeGun.Spawn(gunParent, this);

        // some magic for IK
        //Transform[] allChildren = gunParent.GetComponentsInChildren<Transform>();
        //invenseKinematics.LeftIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftHand");
        //invenseKinematics.RightIKTarget = allChildren.FirstOrDefault(child => child.name == "RightHand");
        //invenseKinematics.RightElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "RightElbow");
        //invenseKinematics.LeftElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftElbow");
    }
}
