using DatScript;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class CrustaspikanRock : MonoBehaviour
{
	[Header("Rock Settings")]
	[Tooltip("Thời gian bay cố định để đá rớt trúng đích (Giây).")]
	[SerializeField] private float _flightTime = 1.2f;

	[Tooltip("Hệ số trọng lực. Càng lớn đá bay càng bổng và rớt xuống càng gắt (Tạo cảm giác nặng nề).")]
	[SerializeField] private float _gravityMultiplier = 2.5f; // [MỚI] Tăng trọng lực lên 2.5 lần

	[Tooltip("Lực xoay (để cục đá lộn nhào trên không cho tự nhiên)")]
	[SerializeField] private float _tumbleSpeed = 10f;
	[SerializeField] private float _lifeTime = 5f;

	[Header("Damage Settings")]
	[SerializeField] private float _directDamage = 40f;
	[SerializeField] private float _aoeDamage = 15f;
	[SerializeField] private float _aoeRadius = 3f;

	[Header("Effects")]
	[SerializeField] private GameObject _rockVisuals;
	[SerializeField] private GameObject _explosionVFX;

	private Rigidbody _rb;
	private bool _isLaunched = false;
	private bool _hasExploded = false;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_rb.isKinematic = true;

		// Luôn tắt trọng lực mặc định để dùng trọng lực tự chế
		_rb.useGravity = false;

		if (_explosionVFX != null) _explosionVFX.SetActive(false);
	}

	public void Launch(Vector3 targetPosition)
	{
		_isLaunched = true;
		_rb.isKinematic = false;

		// Lưu ý: Không bật _rb.useGravity = true nữa. Ta sẽ tự hút nó xuống ở FixedUpdate.

		// 1. Tính toán Vector Quãng đường (S)
		Vector3 displacement = targetPosition - transform.position;

		// 2. [CẬP NHẬT] Tính gia tốc rơi mới bằng cách nhân hệ số
		Vector3 customGravity = Physics.gravity * _gravityMultiplier;

		// 3. Tính toán Vận tốc ban đầu (V0) bù trừ với trọng lực mới
		Vector3 initialVelocity = (displacement - 0.5f * customGravity * (_flightTime * _flightTime)) / _flightTime;

		// 4. Áp dụng vận tốc
		_rb.linearVelocity = initialVelocity;
		_rb.angularVelocity = Random.insideUnitSphere * _tumbleSpeed;

		Destroy(gameObject, _lifeTime);
	}

	// [MỚI] Dùng FixedUpdate để kéo cục đá xuống bằng trọng lực tự chế
	private void FixedUpdate()
	{
		if (_isLaunched && !_hasExploded)
		{
			// ForceMode.Acceleration giúp tác dụng lực kéo rơi tự do mà không phụ thuộc vào khối lượng (Mass) của cục đá
			Vector3 customGravity = Physics.gravity * _gravityMultiplier;
			_rb.AddForce(customGravity, ForceMode.Acceleration);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!_isLaunched || _hasExploded) return;
		if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.collider.isTrigger) return;

		Collider directHitCollider = null;

		if (collision.gameObject.CompareTag("Player"))
		{
			directHitCollider = collision.collider;
			PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(_directDamage);
				Debug.Log($"[{name}] Rock DIRECT hit Player! Dealt {_directDamage} damage.");
			}
		}

		Explode(directHitCollider);
	}

	public void Explode(Collider excludeCollider = null)
	{
		_hasExploded = true;

		if (_rockVisuals != null) _rockVisuals.SetActive(false);
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		_rb.linearVelocity = Vector3.zero;
		_rb.isKinematic = true;
		GetComponent<Collider>().enabled = false;

		Collider[] hits = Physics.OverlapSphere(transform.position, _aoeRadius);
		foreach (var hit in hits)
		{
			if (hit == excludeCollider || hit.isTrigger) continue;

			if (hit.CompareTag("Player"))
			{
				PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
				if (playerHealth != null)
				{
					playerHealth.TakeDamage(_aoeDamage);
					Debug.Log($"[{name}] Rock AOE hit Player! Dealt {_aoeDamage} splash damage.");
				}
			}
		}

		Destroy(gameObject, 2f);
	}

	public void ExplodeImmediate()
	{
		_hasExploded = true;

		if (_rockVisuals != null) _rockVisuals.SetActive(false);
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		if (_rb != null)
		{
			_rb.linearVelocity = Vector3.zero;
			_rb.isKinematic = true;
		}

		Collider col = GetComponent<Collider>();
		if (col != null) col.enabled = false;

		Destroy(gameObject, 3.5f);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1, 0, 0, 0.4f);
		Gizmos.DrawWireSphere(transform.position, _aoeRadius);
	}
}