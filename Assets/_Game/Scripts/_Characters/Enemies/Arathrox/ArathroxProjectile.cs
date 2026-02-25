using UnityEngine;
using DatScript;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ArathroxProjectile : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float _speed = 20f;
	[SerializeField] private float _damage = 25f; // Sát thương của đạn
	[SerializeField] private float _maxLifetime = 5f;

	[Header("Visuals")]
	[SerializeField] private GameObject _projectileVisuals;
	[SerializeField] private GameObject _explosionVFX;

	private bool _hasHit = false;
	private Rigidbody _rb;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_rb.useGravity = false;
		_rb.isKinematic = true;
	}

	private void Start()
	{
		Destroy(gameObject, _maxLifetime);
	}

	private void Update()
	{
		if (_hasHit) return;
		transform.Translate(Vector3.forward * _speed * Time.deltaTime);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_hasHit) return;
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.isTrigger) return;

		// Xử lý trúng Player
		if (other.CompareTag("Player"))
		{
			// [UPDATED] Gọi hàm TakeDamage
			PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(_damage);
				Debug.Log($"[{name}] Projectile hit Player! Dealt {_damage} damage.");
			}

			// Hủy ngay lập tức
			Destroy(gameObject);
			return;
		}

		// Xử lý trúng môi trường (Nổ)
		HandleEnvironmentHit();
	}

	private void HandleEnvironmentHit()
	{
		_hasHit = true;
		if (_projectileVisuals != null) _projectileVisuals.SetActive(false);
		if (_explosionVFX != null) _explosionVFX.SetActive(true);
		Destroy(gameObject, 1f);
	}
}