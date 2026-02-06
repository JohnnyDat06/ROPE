using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý hệ thống chiến đấu của Arathrox.
/// Các hàm trong script này được thiết kế để gọi từ Animation Events.
/// </summary>
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
	[SerializeField] private float _biteRadius = 1f;
	[SerializeField] private LayerMask _targetLayer; // Layer của Player

	[Header("Roar Settings (Hét)")]
	[SerializeField] private float _roarRadius = 3f;
	[SerializeField] private float _knockbackForce = 15f;
	[SerializeField] private float _knockbackUpward = 1f;
	#endregion

	#region Internal State
	// Biến lưu tham chiếu Player (để aim bắn súng)
	private Transform _playerTarget;
	// Cờ kiểm tra để tránh gây damage nhiều lần trong 1 cú cắn
	private bool _hasDealtBiteDamage = false;
	#endregion

	#region Unity Lifecycle
	private void Start()
	{
		// Tự động tìm Player nếu chưa có (Hoặc gán từ Behavior Graph)
		if (_playerTarget == null)
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj) _playerTarget = playerObj.transform;
		}
	}

	// Vẽ Gizmos để debug vùng đánh trong Editor
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

	#region Animation Event Methods (Core Logic)

	/// <summary>
	/// [ANIMATION EVENT] Gọi tại frame bắn của animation đuôi.
	/// Sinh ra đạn bay từ đuôi về phía Player.
	/// </summary>
	public void SpawnTailPoison()
	{
		if (_poisonProjectilePrefab == null || _tailFirePoint == null)
		{
			Debug.LogWarning($"[{name}] Missing Projectile Prefab or Tail Fire Point!");
			return;
		}

		// 1. Xác định hướng bắn
		// Mặc định là hướng của đuôi (Tail Forward)
		Vector3 shootDirection = _tailFirePoint.forward;

		// Logic: "Chỉ chiều dọc là hướng về phía Player"
		// Nghĩa là: Giữ nguyên độ xoay ngang của đuôi, nhưng chỉnh độ cao (Pitch) để trúng người
		if (_playerTarget != null)
		{
			// Vector từ đuôi đến Player
			Vector3 toPlayer = _playerTarget.position - _tailFirePoint.position;

			// Kết hợp: Lấy hướng ngang của đuôi + hướng dọc của Player
			// Hoặc đơn giản hơn: Bắn thẳng vào Player (Aim Assist)
			// Ở đây tôi dùng phương pháp Aim Assist từ vị trí đuôi để đảm bảo trúng
			shootDirection = toPlayer.normalized;
		}

		// 2. Sinh ra đạn
		GameObject projectile = Instantiate(_poisonProjectilePrefab, _tailFirePoint.position, Quaternion.LookRotation(shootDirection));

		// 3. Setup Projectile (Giả sử bạn có script Projectile riêng)
		// ProjectileScript proScript = projectile.GetComponent<ProjectileScript>();
		// if (proScript) proScript.Initialize(shootDirection, _biteDamage); // Reuse biến damage hoặc tạo biến riêng

		Debug.Log($"[{name}] Poison Shot Fired from Tail!");
	}

	/// <summary>
	/// [ANIMATION EVENT] Gọi tại frame bắt đầu cú cắn (Start Bite).
	/// Kích hoạt Coroutine kiểm tra va chạm trong thời gian 'duration'.
	/// </summary>
	/// <param name="duration">Thời gian hitbox tồn tại (giây).</param>
	public void PerformBiteCheck(float duration)
	{
		StartCoroutine(BiteHitboxRoutine(duration));
	}

	/// <summary>
	/// [ANIMATION EVENT] Gọi tại frame quái hét lên.
	/// Đẩy lùi mọi vật thể xung quanh.
	/// </summary>
	public void PerformRoarKnockback()
	{
		// Tìm tất cả Collider trong vùng ảnh hưởng
		Collider[] hits = Physics.OverlapSphere(transform.position, _roarRadius);
		bool hitPlayer = false;

		foreach (var hit in hits)
		{
			// Bỏ qua chính mình
			if (hit.transform == transform) continue;

			// 1. Xử lý Knockback vật lý (Rigidbody)
			Rigidbody rb = hit.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.AddExplosionForce(_knockbackForce, transform.position, _roarRadius, _knockbackUpward, ForceMode.Impulse);
			}

			// 2. Xử lý Knockback cho Player (CharacterController/NavMeshAgent)
			// Giả sử Player có script nhận Knockback riêng hoặc dùng Tag
			if (hit.CompareTag("Player"))
			{
				hitPlayer = true;
				// Gọi hàm TakeDamage hoặc ApplyKnockback trên Player
				Debug.Log($"[{name}] Roar hit Player! Applying Knockback.");

				// Demo gọi hàm TakeDamage
				// var playerStats = hit.GetComponent<PlayerStats>();
				// if (playerStats) playerStats.TakeDamage(5f); 
			}
		}

		// Play Sound / VFX tại đây
	}

	#endregion

	#region Coroutines & Helpers

	private IEnumerator BiteHitboxRoutine(float duration)
	{
		_hasDealtBiteDamage = false;
		float timer = 0f;

		while (timer < duration)
		{
			// Nếu đã cắn trúng rồi thì thôi (tránh 1 cú cắn trừ máu 10 lần mỗi frame)
			if (_hasDealtBiteDamage) yield break;

			// Kiểm tra va chạm quanh miệng
			Collider[] hits = Physics.OverlapSphere(_mouthPoint.position, _biteRadius, _targetLayer);

			foreach (var hit in hits)
			{
				if (hit.CompareTag("Player"))
				{
					// Gây sát thương
					Debug.Log($"[{name}] Bite Hit Player! Dealing {_biteDamage} damage.");

					// Gọi hàm TakeDamage của Player
					// hit.GetComponent<IDamageable>()?.TakeDamage(_biteDamage);

					_hasDealtBiteDamage = true;
					yield break; // Kết thúc ngay khi cắn trúng
				}
			}

			timer += Time.deltaTime;
			yield return null; // Đợi frame tiếp theo
		}
	}

	#endregion
}