using DatScript;
using System.Collections;
using UnityEngine;

public class CrustaspikanCombat : MonoBehaviour
{
	#region Configuration
	[Header("General Setup")]
	[SerializeField] private Animator _animator;
	[SerializeField] private Transform _eyePoint;
	[SerializeField] private AudioSource _audioSource;

	[Header("Cooldown Settings")]
	[SerializeField] private float _handAttackCooldown = 2f;
	[SerializeField] private float _throwRockCooldown = 6f;

	private float _lastHandAttackTime = -999f;
	private float _lastThrowTime = -999f;

	[Header("Skill 1: Hand Attack (Tát gần)")]
	[SerializeField] private float _handAttackMinRange = 5f;
	[SerializeField] private float _handAttackMaxRange = 6f;
	[SerializeField] private float _handDamage = 30f;
	[SerializeField] private float _knockbackForce = 15f;

	[Tooltip("Kéo Box/Sphere Collider tay vào đây. Nhớ BẬT ENABLE sẵn trên Inspector.")]
	[SerializeField] private Collider _leftHandCollider;
	[SerializeField] private Collider _rightHandCollider;
	[SerializeField] private AudioClip _swipeSound;

	[Header("Skill 2: Throw Rock (Ném đá)")]
	[SerializeField] private float _throwMinRange = 15f;
	[SerializeField] private float _throwMaxRange = 25f;
	[SerializeField] private GameObject _rockPrefab;
	[SerializeField] private Transform _rockSpawnBone;
	[SerializeField] private AudioClip _grabRockSound;
	[SerializeField] private float _grabSoundDelay = 0.3f;
	[SerializeField] private AudioClip _throwRockSound;
	[SerializeField] private float _holdRockDuration = 1.5f;
	#endregion

	#region Internal State
	private Transform _currentTarget;
	private GameObject _currentHeldRock;
	private Coroutine _holdRockCoroutine;
	private bool _isStunned = false;

	// [CẬP NHẬT] Biến cờ kiểm soát khung hình gây sát thương
	private bool _hasDealtHandDamage = false;
	private bool _isLeftHandActive = false;
	private bool _isRightHandActive = false;

	public bool IsAttacking { get; private set; }
	#endregion

	private void Start()
	{
		// ĐÃ XÓA LỆNH TẮT COLLIDER. Bây giờ tay Boss lúc nào cũng có va chạm vật lý.
	}

	#region Chống kẹt Animation

	public void CancelPendingAttacks()
	{
		_animator.ResetTrigger("AttackRightHand");
		_animator.ResetTrigger("AttackLeftHand");
		_animator.ResetTrigger("UnearthRock");
		_animator.SetBool("ThrowRock", false);
		IsAttacking = false;
	}

	private bool IsBusyTurning()
	{
		return Mathf.Abs(_animator.GetFloat(Animator.StringToHash("Turn"))) > 0.05f;
	}

	#endregion

	#region Skill 1: Hand Attack (Tát)

	public bool CheckHandAttackRange(Transform target, out bool isRightSide)
	{
		isRightSide = true;
		if (target == null || _eyePoint == null) return false;

		float distance = Vector3.Distance(_eyePoint.position, target.position);
		if (distance < _handAttackMinRange || distance > _handAttackMaxRange) return false;

		Vector3 dirToTarget = (target.position - _eyePoint.position).normalized;
		float rightDot = Vector3.Dot(transform.right, dirToTarget);
		isRightSide = rightDot > 0;

		return true;
	}

	public bool ExecuteHandAttack(Transform target)
	{
		if (IsBusyTurning()) { CancelPendingAttacks(); return false; }
		IsAttacking = true; _isStunned = false;

		if (CheckHandAttackRange(target, out bool isRight))
		{
			string triggerName = isRight ? "AttackRightHand" : "AttackLeftHand";
			_animator.SetTrigger(triggerName);
			if (_audioSource && _swipeSound) _audioSource.PlayOneShot(_swipeSound);

			_lastHandAttackTime = Time.time;
			return true;
		}
		IsAttacking = false; return false;
	}

	// [CẬP NHẬT] Thay vì bật/tắt Collider, ta chỉ bật/tắt biến cờ (Logic Frame)
	public void AnimEvent_EnableLeftHand()
	{
		_hasDealtHandDamage = false;
		_isLeftHandActive = true; // Bắt đầu tính sát thương tay trái
	}

	public void AnimEvent_EnableRightHand()
	{
		_hasDealtHandDamage = false;
		_isRightHandActive = true; // Bắt đầu tính sát thương tay phải
	}

	public void AnimEvent_DisableLeftHand() => _isLeftHandActive = false;
	public void AnimEvent_DisableRightHand() => _isRightHandActive = false;

	#endregion

	#region XỬ LÝ VA CHẠM CẬN CHIẾN (OnCollisionEnter)

	private void OnCollisionEnter(Collision collision)
	{
		// Nếu đang không ra đòn hoặc đã gây sát thương rồi -> Bỏ qua
		if (!IsAttacking || _hasDealtHandDamage) return;

		// Nếu đối tượng va chạm không phải là Player -> Bỏ qua
		if (!collision.gameObject.CompareTag("Player")) return;

		// Lấy Collider cụ thể đang va chạm với Player
		Collider myColliderHit = collision.GetContact(0).thisCollider;

		// [BÍ QUYẾT] Kiểm tra xem cái tay chạm vào có ĐANG ĐƯỢC BẬT CỜ SÁT THƯƠNG không
		bool isHitByActiveLeft = (myColliderHit == _leftHandCollider && _isLeftHandActive);
		bool isHitByActiveRight = (myColliderHit == _rightHandCollider && _isRightHandActive);

		// Nếu quệt trúng tay nhưng lúc đó tay chưa tới Frame sát thương (hoặc quệt trúng bụng) -> Bỏ qua
		if (!isHitByActiveLeft && !isHitByActiveRight) return;

		// 1. GÂY SÁT THƯƠNG
		PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.TakeDamage(_handDamage);
			Debug.Log($"[{name}] Tát trúng Player! Gây {_handDamage} sát thương.");
		}

		// 2. LỰC ĐẨY VĂNG (KNOCKBACK NGANG)
		Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
		if (playerRb != null)
		{
			Vector3 pushDirection = collision.transform.position - transform.position;
			pushDirection.y = 0; // Triệt tiêu Y để tránh văng lên trời
			pushDirection.Normalize();

			// Khóa vận tốc hiện tại để lực văng luôn ổn định
			playerRb.linearVelocity = new Vector3(0, playerRb.linearVelocity.y, 0);
			playerRb.AddForce(pushDirection * _knockbackForce, ForceMode.Impulse);
		}

		_hasDealtHandDamage = true;
	}

	#endregion

	#region Skill 2: Throw Rock (Ném đá)

	public bool CheckThrowRange(Transform target)
	{
		if (target == null || _eyePoint == null) return false;
		float dist = Vector3.Distance(_eyePoint.position, target.position);
		return dist >= _throwMinRange && dist <= _throwMaxRange;
	}

	public bool ExecuteThrowRock(Transform target)
	{
		if (IsBusyTurning()) { CancelPendingAttacks(); return false; }
		IsAttacking = true; _isStunned = false; _currentTarget = target;

		_animator.SetTrigger("UnearthRock");
		if (_audioSource && _grabRockSound)
			StartCoroutine(PlaySoundDelayed(_grabRockSound, _grabSoundDelay));

		_lastThrowTime = Time.time;
		return true;
	}

	public void AnimEvent_SpawnRockInHand()
	{
		if (_rockPrefab == null || _rockSpawnBone == null || _isStunned) return;

		_currentHeldRock = Instantiate(_rockPrefab, _rockSpawnBone.position, _rockSpawnBone.rotation);
		_currentHeldRock.transform.SetParent(_rockSpawnBone);
	}

	public void AnimEvent_HoldRockReady()
	{
		if (_isStunned) return;
		_holdRockCoroutine = StartCoroutine(HoldRockRoutine());
	}

	private IEnumerator HoldRockRoutine()
	{
		yield return new WaitForSeconds(_holdRockDuration);
		if (!_isStunned)
		{
			_animator.SetBool("ThrowRock", true);
		}
	}

	public void AnimEvent_ReleaseRock()
	{
		_animator.SetBool("ThrowRock", false);

		if (_currentHeldRock == null || _currentTarget == null) return;

		_currentHeldRock.transform.SetParent(null);

		if (_audioSource && _throwRockSound)
			_audioSource.PlayOneShot(_throwRockSound);

		Vector3 targetPos = _currentTarget.position + Vector3.up * 1.5f;

		CrustaspikanRock rockScript = _currentHeldRock.GetComponent<CrustaspikanRock>();
		if (rockScript != null)
		{
			rockScript.Launch(targetPos);
		}

		_currentHeldRock = null;
	}

	#endregion

	#region Tương tác Đặc biệt (Choáng & Ngắt đòn)

	public void TriggerStun()
	{
		_isStunned = true;
		IsAttacking = false;

		if (_holdRockCoroutine != null) StopCoroutine(_holdRockCoroutine);

		CancelPendingAttacks();

		_animator.SetTrigger("IsStun");

		if (_currentHeldRock != null)
		{
			CrustaspikanRock rockScript = _currentHeldRock.GetComponent<CrustaspikanRock>();
			if (rockScript != null)
			{
				rockScript.ExplodeImmediate();
			}
			else
			{
				Destroy(_currentHeldRock);
			}
			_currentHeldRock = null;
		}

		// [CẬP NHẬT] Đảm bảo reset cờ sát thương tay khi bị choáng
		_isLeftHandActive = false;
		_isRightHandActive = false;
	}

	public bool IsAnySkillReady(Transform target)
	{
		if (target == null || IsAttacking || _isStunned || IsBusyTurning()) return false;

		if (Time.time >= _lastHandAttackTime + _handAttackCooldown)
		{
			if (CheckHandAttackRange(target, out _)) return true;
		}

		if (Time.time >= _lastThrowTime + _throwRockCooldown)
		{
			if (CheckThrowRange(target)) return true;
		}

		return false;
	}

	#endregion

	#region Utility Events

	public void AnimEvent_EndAttack()
	{
		IsAttacking = false;
		// [CẬP NHẬT] Reset cờ an toàn khi thu tay về
		_isLeftHandActive = false;
		_isRightHandActive = false;
	}

	private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (!_isStunned) _audioSource.PlayOneShot(clip);
	}

	#endregion
}