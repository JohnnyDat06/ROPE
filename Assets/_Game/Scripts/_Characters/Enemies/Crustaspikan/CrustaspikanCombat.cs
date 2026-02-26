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

	// [CẬP NHẬT] Đã xóa Collider, thay bằng Transform điểm trung tâm của tay và các thông số OverlapSphere
	[SerializeField] private Transform _leftHandPoint;
	[SerializeField] private Transform _rightHandPoint;
	[SerializeField] private float _handAttackRadius = 1.5f;
	[SerializeField] private float _handDamage = 30f;
	[SerializeField] private float _handHitboxDuration = 0.5f; // Thời gian duy trì vùng sát thương mỗi lần quơ tay
	[SerializeField] private LayerMask _targetLayer; // Chọn layer của Player

	[SerializeField] private AudioClip _swipeSound;

	[Header("Skill 2: Throw Rock (Ném đá)")]
	[SerializeField] private float _throwMinRange = 15f;
	[SerializeField] private float _throwMaxRange = 25f;
	[SerializeField] private GameObject _rockPrefab;
	[SerializeField] private Transform _rockSpawnBone;
	[SerializeField] private AudioClip _grabRockSound;
	[SerializeField] private float _grabSoundDelay = 0.3f;
	[SerializeField] private AudioClip _throwRockSound;
	[Tooltip("Thời gian gồng (giữ đá trên không) trước khi ném")]
	[SerializeField] private float _holdRockDuration = 1.5f;
	#endregion

	#region Internal State
	private Transform _currentTarget;
	private GameObject _currentHeldRock;
	private Coroutine _holdRockCoroutine;
	private bool _isStunned = false;

	// [CẬP NHẬT] State cho OverlapSphere
	private bool _hasDealtHandDamage = false;
	private Coroutine _handDamageCoroutine;

	// Biến khóa trạng thái
	public bool IsAttacking { get; private set; }
	#endregion

	#region Chống kẹt Animation (Clear Triggers)

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

	// [CẬP NHẬT] Thay thế logic bật/tắt Collider thành gọi Coroutine OverlapSphere
	// Vẫn giữ tên hàm AnimEvent cũ để bạn không phải sửa lại Timeline Animation
	public void AnimEvent_EnableLeftHand()
	{
		StopHandDamageRoutine();
		_handDamageCoroutine = StartCoroutine(HandHitboxRoutine(_leftHandPoint, _handHitboxDuration));
	}

	public void AnimEvent_EnableRightHand()
	{
		StopHandDamageRoutine();
		_handDamageCoroutine = StartCoroutine(HandHitboxRoutine(_rightHandPoint, _handHitboxDuration));
	}

	public void AnimEvent_DisableLeftHand() => StopHandDamageRoutine();
	public void AnimEvent_DisableRightHand() => StopHandDamageRoutine();

	private void StopHandDamageRoutine()
	{
		if (_handDamageCoroutine != null)
		{
			StopCoroutine(_handDamageCoroutine);
			_handDamageCoroutine = null;
		}
	}

	/// <summary>
	/// Coroutine quyét sát thương hình cầu liên tục trong khoảng thời gian (duration)
	/// </summary>
	private IEnumerator HandHitboxRoutine(Transform handPoint, float duration)
	{
		_hasDealtHandDamage = false;
		float timer = 0f;

		// Phòng hờ trường hợp chưa gán điểm tay trên Inspector
		if (handPoint == null) yield break;

		while (timer < duration)
		{
			if (_hasDealtHandDamage || _isStunned) yield break;

			Collider[] hits = Physics.OverlapSphere(handPoint.position, _handAttackRadius, _targetLayer);

			foreach (var hit in hits)
			{
				if (hit.CompareTag("Player"))
				{
					// Gọi script máu của Player (Cần đổi 'PlayerHealth' thành tên script máu thực tế của bạn nếu khác)
					PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

					if (playerHealth != null)
					{
						playerHealth.TakeDamage(_handDamage);
						Debug.Log($"[{name}] Tát trúng Player! Gây {_handDamage} sát thương.");

						// Đã trúng thì dừng quét để không gây sát thương 2 lần cho 1 cú đánh
						_hasDealtHandDamage = true;
						yield break;
					}
				}
			}

			timer += Time.deltaTime;
			yield return null;
		}
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
		Vector3 throwDirection = (targetPos - _currentHeldRock.transform.position).normalized;

		CrustaspikanRock rockScript = _currentHeldRock.GetComponent<CrustaspikanRock>();
		if (rockScript != null)
		{
			rockScript.Launch(throwDirection);
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

		// [CẬP NHẬT] Hủy coroutine gây sát thương tay nếu đang vung dở
		StopHandDamageRoutine();
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
		// [CẬP NHẬT] Hủy coroutine để đảm bảo tay không gây sát thương khi đã thu về
		StopHandDamageRoutine();
	}

	private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (!_isStunned) _audioSource.PlayOneShot(clip);
	}

	// [MỚI] Vẽ khung cầu màu đỏ trên Scene để bạn dễ dàng căn chỉnh độ bự của đòn tát
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		if (_leftHandPoint != null) Gizmos.DrawWireSphere(_leftHandPoint.position, _handAttackRadius);
		if (_rightHandPoint != null) Gizmos.DrawWireSphere(_rightHandPoint.position, _handAttackRadius);
	}

	#endregion
}