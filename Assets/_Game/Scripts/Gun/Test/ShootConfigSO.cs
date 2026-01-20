using UnityEngine;

[CreateAssetMenu(fileName = "ShootConfigSO", menuName = "Guns/ShootConfigSO", order = 2)]
public class ShootConfigSO : ScriptableObject
{
    public LayerMask hitMask;
    public Vector3 spread = Vector3.zero;
    public float fireRate = 0.25f;
}
