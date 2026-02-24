using UnityEngine;

namespace Script.Enemy
{
	/// <summary>
	/// Handles enemy attacks by synchronizing damage dealing with specific animation frames.
	/// Uses Normalized Time to define a "Hit Window".
	/// </summary>
	//public class EnemyAttackHandler : MonoBehaviour
	//{
	//	#region Configuration

	//	[Header("Animation Settings")]
	//	[Tooltip("Exact name of the Attack State in the Animator Controller.")]
	//	public string AttackStateName = "Enemy_Attack";

	//	[Header("Damage Settings")]
	//	[Tooltip("Amount of health to deduct from the player per hit.")]
	//	[SerializeField] private float damageAmount = 50f;

	//	[Tooltip("Start point of the damage window (Normalized Time 0.0 - 1.0).")]
	//	[Range(0f, 1f)] public float DamageStartPercent = 0.2f;

	//	[Tooltip("End point of the damage window (Normalized Time 0.0 - 1.0).")]
	//	[Range(0f, 1f)] public float DamageEndPercent = 0.5f;

	//	[Header("Range & Detection")]
	//	[Tooltip("Maximum distance to successfully hit the player.")]
	//	[SerializeField] private float attackRange = 2.0f;

	//	[Tooltip("Origin point for the attack radius check (e.g., Weapon tip or Hand). Defaults to Self if null.")]
	//	[SerializeField] private Transform attackPoint;

	//	#endregion

	//	#region References

	//	[Header("Target References")]
	//	[Tooltip("Reference to the Player's Transform.")]
	//	[SerializeField] private Transform playerTransform;

	//	#endregion

	//	#region Internal State

	//	private Animator _animator;
	//	private bool _hasDealtDamageInThisAnimation = false;

	//	#endregion

	//	#region Lifecycle Methods

	//	void Start()
	//	{
	//		_animator = GetComponent<Animator>();

	//		// Tự động tìm Player nếu chưa được gán thủ công
	//		if (playerTransform == null)
	//		{
	//			GameObject p = GameObject.FindGameObjectWithTag("Player");
	//			if (p != null) playerTransform = p.transform;
	//		}

	//		// Fallback: Nếu không gán Attack Point, dùng chính vị trí của Enemy
	//		if (attackPoint == null) attackPoint = transform;
	//	}

	//	void Update()
	//	{
	//		if (_animator == null || playerTransform == null) return;

	//		AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

	//		// Kiểm tra: Có đang trong trạng thái Tấn Công không?
	//		if (stateInfo.IsName(AttackStateName))
	//		{
	//			// Sử dụng % 1 để hỗ trợ Animation dạng Loop
	//			float time = stateInfo.normalizedTime % 1;

	//			// --- KIỂM TRA CỬA SỔ SÁT THƯƠNG (HIT WINDOW) ---
	//			if (time >= DamageStartPercent && time <= DamageEndPercent)
	//			{
	//				// Logic: Chỉ gây damage 1 lần duy nhất mỗi nhịp chém
	//				if (!_hasDealtDamageInThisAnimation)
	//				{
	//					CheckHit();
	//					_hasDealtDamageInThisAnimation = true;
	//				}
	//			}
	//			// Logic: Reset cờ khi Animation sắp hết (để chuẩn bị cho Loop tiếp theo)
	//			else if (time > 0.95f)
	//			{
	//				_hasDealtDamageInThisAnimation = false;
	//			}
	//		}
	//		else
	//		{
	//			// Nếu bị ngắt chiêu hoặc chuyển State -> Reset cờ
	//			_hasDealtDamageInThisAnimation = false;
	//		}
	//	}

	//	private void OnDrawGizmosSelected()
	//	{
	//		if (attackPoint == null) return;
	//		Gizmos.color = Color.red;
	//		Gizmos.DrawWireSphere(attackPoint.position, attackRange);
	//	}

	//	#endregion

	//	#region Core Logic

	//	/// <summary>
	//	/// Checks distance to Player and applies damage if within range.
	//	/// </summary>
	//	private void CheckHit()
	//	{
	//		float distance = Vector3.Distance(attackPoint.position, playerTransform.position);

	//		if (distance <= attackRange)
	//		{
	//			DealDamageToPlayer();
	//		}
	//	}

	//	private void DealDamageToPlayer()
	//	{
	//		// Ưu tiên dùng Singleton Pattern
	//		if (PlayerStats.Instance != null)
	//		{
	//			// Debug.Log($"Enemy: Hit Player! Dealt {damageAmount} damage.");
	//			PlayerStats.Instance.TakeDamage(damageAmount);
	//		}
	//		else
	//		{
	//			// Fallback: Tìm component thủ công (chậm hơn nhưng an toàn)
	//			PlayerStats stats = playerTransform.GetComponent<PlayerStats>();
	//			if (stats != null)
	//			{
	//				stats.TakeDamage(damageAmount);
	//			}
	//		}
	//	}

	//	#endregion
	//}
}