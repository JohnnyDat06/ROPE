using UnityEngine;

public class RaycastWeapon : MonoBehaviour
{
    public bool isFiring = false;
    [SerializeField] private ParticleSystem[] muzzleFlash;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private Transform raycastDestination;

    Ray ray;
    RaycastHit hitInfo;

    public void StartFiring()
    {
        isFiring = true;
        foreach (var particleSystem in muzzleFlash)
            particleSystem.Emit(1);

        ray.origin = raycastOrigin.position;
        Vector3 direction = raycastDestination.position - raycastOrigin.position;
        ray.direction = direction.normalized;

        if (Physics.Raycast(ray, out hitInfo))
        {
            hitEffect.transform.position = hitInfo.point;
            hitEffect.transform.forward = hitInfo.normal;
            hitEffect.Emit(1);
        }
    }

    public void StopFiring()
    {
        isFiring = false;
    }
}
