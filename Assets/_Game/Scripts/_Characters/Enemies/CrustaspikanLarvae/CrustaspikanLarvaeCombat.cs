using DatScript;
using UnityEngine;

public class CrustaspikanLarvaeCombat : MonoBehaviour
{
	[Header("Explosion Setup (Hình Cầu)")]
	[Tooltip("Bán kính vụ nổ tức thời")]
	[SerializeField] private float _explosionRadius = 3.5f;

	[Tooltip("Sát thương của vụ nổ ban đầu")]
	[SerializeField] private int _burstDamage = 50;

	[Tooltip("Layer chứa Player và Enemy để vụ nổ quét trúng cả hai")]
	[SerializeField] private LayerMask _targetLayers;

	[Header("VFX & Prefabs")]
	[Tooltip("Object VFX vụ nổ ĐÃ GẮN SẴN trên người con quái (Tắt sẵn trên Inspector)")]
	[SerializeField] private GameObject _explosionVfxObject;

	[Tooltip("Prefab bãi lửa để lại sau vụ nổ")]
	[SerializeField] private GameObject _fireHazardPrefab;

	private bool _hasExploded = false;

	public void TriggerExplosion()
	{
		if (_hasExploded) return;
		_hasExploded = true;

		// 1. Xử lý bật VFX có sẵn
		if (_explosionVfxObject != null)
		{
			_explosionVfxObject.SetActive(true);
		}

		// 2. Sinh ra bãi lửa
		if (_fireHazardPrefab != null)
		{
			Instantiate(_fireHazardPrefab, transform.position, transform.rotation);
		}

		// 3. Quét sát thương nổ AOE tức thời (OverlapSphere)
		Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius, _targetLayers);

		foreach (Collider hit in hits)
		{
			if (hit.gameObject == this.gameObject) continue;

			if (hit.CompareTag("Player"))
			{
				PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
				if (playerHealth != null) playerHealth.TakeDamage(_burstDamage);
			}
			else if (hit.CompareTag("Enemy") || hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
			{
				EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
				if (enemyHealth != null) enemyHealth.TakeDamage(_burstDamage);
			}
		}

		Debug.Log($"[{gameObject.name}] Đã nổ tung (Sphere)! Gây {_burstDamage} sát thương.");

		// 4. DỌN DẸP HỆ THỐNG TRƯỚC KHI CHẾT
		EnemyTestTakeDamage takeDamageScript = GetComponent<EnemyTestTakeDamage>();
		if (takeDamageScript != null)
		{
			// Gọi hàm tắt NavMesh và Behavior Graph để tránh lỗi ngầm
			takeDamageScript.Death_OnDeath(transform.position);
		}

		// 5. Tự hủy bản thân
		Destroy(gameObject,1.5f);
	}

	// Vẽ hình cầu màu cam trên Scene
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
		Gizmos.DrawWireSphere(transform.position, _explosionRadius);
	}
}