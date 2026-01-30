using UnityEngine;

public class CrossHairtarget : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    Ray ray;
    RaycastHit hitInfo;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        ray.origin = mainCamera.transform.position;
        ray.direction = mainCamera.transform.forward;
        if (Physics.Raycast(ray, out hitInfo, float.MaxValue, aimColliderLayerMask))
        {
            transform.position = hitInfo.point;
        }
        else
        {
            transform.position = ray.origin + ray.direction * 1000.0f;
        }
    }
}
