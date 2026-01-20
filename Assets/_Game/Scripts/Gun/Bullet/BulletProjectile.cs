using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private Transform vfxHitGreen;
    [SerializeField] private Transform vfxHitRed;

    private Rigidbody bulletRigidbody;

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float speed = 10f;
        bulletRigidbody.linearVelocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BulletTarget>() != null)
        {
            Instantiate(vfxHitGreen, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(vfxHitRed, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
