using UnityEngine;
using System.Collections;
using DatScript;

public class ArathroxCombat : MonoBehaviour
{
	#region Configuration
	[Header("References")]
	[Tooltip("Vị trí đầu nòng bắn ra từ đuôi")]
	[SerializeField] private Transform _tailFirePoint;

	[Tooltip("Vị trí miệng để tính vùng cắn")]
	[SerializeField] private Transform _mouthPoint;

	[Tooltip("Prefab đạn chất độc")]
	[SerializeField] private GameObject _poisonProjectilePrefab;

	[Header("Bite Settings (Cắn)")]
	[SerializeField] private float _biteDamage = 25f;
	[SerializeField] private float _biteRadius = 1.2f; // Tăng nhẹ radius để dễ trúng hơn
	[SerializeField] private LayerMask _targetLayer;

	[Header("Roar Settings (Hét)")]
	[SerializeField] private float _roarRadius = 6f;
	[SerializeField] private float _knockbackForce = 15f;
	[SerializeField] private float _knockbackUpward = 1f;
	[SerializeField] private float _roarDamage = 5f; // Sát thương nhẹ khi bị hét trúng
	#endregion

	#region Internal State
	private Transform _playerTarget;
	private bool _hasDealtBiteDamage = false;
	#endregion

	#region Unity Lifecycle
	private void Start()
	{
		if (_playerTarget == null)
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj) _playerTarget = playerObj.transform;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (_mouthPoint != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(_mouthPoint.position, _biteRadius);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _roarRadius);
	}
	#endregion

	#region Animation Event Methods

	public void SpawnTailPoison()
	{
		if (_poisonProjectilePrefab == null || _tailFirePoint == null)
		{
			Debug.LogWarning($"[{name}] Missing Projectile Prefab or Tail Fire Point!");
			return;
		}

		Vector3 shootDirection = _tailFirePoint.forward;

		if (_playerTarget != null)
		{
			// [FIXED] Sửa lỗi bắn xuống chân
			// Cộng thêm Vector3.up * 1.3f (khoảng tầm ngực/đầu người chơi)
			// Player.position thường nằm ở gót chân (Pivot), nên ta cần nhắm cao hơn
			Vector3 targetCenter = _playerTarget.position + Vector3.up * 1.3f;

			Vector3 toPlayer = targetCenter - _tailFirePoint.position;
			shootDirection = toPlayer.normalized;
		}

		GameObject projectile = Instantiate(_poisonProjectilePrefab, _tailFirePoint.position, Quaternion.LookRotation(shootDirection));
	}

	public void PerformBiteCheck(float duration)
	{
		StartCoroutine(BiteHitboxRoutine(duration));
	}

	public void PerformRoarKnockback()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, _roarRadius);

		foreach (var hit in hits)
		{
			if (hit.transform == transform) continue;

			Rigidbody rb = hit.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.AddExplosionForce(_knockbackForce, transform.position, _roarRadius, _knockbackUpward, ForceMode.Impulse);
			}

			if (hit.CompareTag("Player"))
			{
				// [UPDATED] Gây sát thương và đẩy lùi
				PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
				if (playerHealth != null)
				{
					playerHealth.TakeDamage(_roarDamage); // Trừ ít máu khi bị hét
					Debug.Log($"[{name}] Roar hit Player! Dealt {_roarDamage} damage.");
				}
			}
		}
	}

	#endregion

	#region Coroutines

	private IEnumerator BiteHitboxRoutine(float duration)
	{
		_hasDealtBiteDamage = false;
		float timer = 0f;

		while (timer < duration)
		{
			if (_hasDealtBiteDamage) yield break;

			Collider[] hits = Physics.OverlapSphere(_mouthPoint.position, _biteRadius, _targetLayer);

			foreach (var hit in hits)
			{
				if (hit.CompareTag("Player"))
				{
					// [UPDATED] Tích hợp hệ thống máu PlayerHealth
					PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

					if (playerHealth != null)
					{
						playerHealth.TakeDamage(_biteDamage);
						Debug.Log($"[{name}] Bite Hit Player! Dealing {_biteDamage} damage.");

						_hasDealtBiteDamage = true;
						yield break;
					}
				}
			}

			timer += Time.deltaTime;
			yield return null;
		}
	}

	#endregion
}