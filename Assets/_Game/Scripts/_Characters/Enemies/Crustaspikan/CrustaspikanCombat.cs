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

	// [MỚI] Cài đặt xoay người khi đang đánh
	[Header("Attack Tracking (Xoay bám mục tiêu)")]
	[Tooltip("Tốc độ Boss xoay mặt theo Player khi đang gồng chiêu.")]
	[SerializeField] private float _attackRotationSpeed = 5f;

	[Header("Skill 1: Hand Attack (Tát gần)")]
	[SerializeField] private float _handAttackMinRange = 5f;
	[SerializeField] private float _handAttackMaxRange = 6f;
	[SerializeField] private float _handDamage = 30f;
	[SerializeField] private float _knockbackForce = 15f;

	[Tooltip("Kéo BoxCollider tay vào đây.")]
	[SerializeField] private BoxCollider _leftHandCollider;
	[SerializeField] private BoxCollider _rightHandCollider;
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
	
	[Header("Skill 3: Summon Minions (Triệu hồi)")]
	[Tooltip("Kéo script EnemyHealth của Boss vào đây để đo máu")]
	[SerializeField] private EnemyHealth _health;
	[SerializeField] private GameObject[] _minionPrefabs;
	[SerializeField] private int _minionCount = 3;
	[SerializeField] private float _summonRadius = 5f;
	[SerializeField] private float _summonFirstime = 0.6f;
	[SerializeField] private float _summonSecondtime = 0.4f;
	[SerializeField] private AudioClip _summonSound;
	[SerializeField] private float _summonSoundDelay = 0.2f;
	#endregion

	#region Internal State
	private Transform _currentTarget;
	private GameObject _currentHeldRock;
	private Coroutine _holdRockCoroutine;
	private bool _isStunned = false;

	private bool _hasDealtHandDamage = false;
	private bool _isLeftHandActive = false;
	private bool _isRightHandActive = false;
	
	private bool _hasSummonedAt60 = false;
	private bool _hasSummonedAt40 = false;

	public bool IsAttacking { get; private set; }

	// [MỚI] Cờ cho phép bám mục tiêu. Được bật khi ra đòn, tắt khi tay chuẩn bị vung xuống.
	private bool _isTrackingTarget = false;
	#endregion

	private void Start() { }

	private void Update()
	{
		if (!IsAttacking) return;

		// 1. [MỚI] Xử lý Tracking (Xoay nhẹ theo Player lúc đang gồng)
		if (_isTrackingTarget && _currentTarget != null && !_isStunned)
		{
			HandleAttackTracking();
		}

		// 2. Quét Hitbox cận chiến
		if (_hasDealtHandDamage) return;

		if (_isLeftHandActive && _leftHandCollider != null)
		{
			CheckMeleeHitbox(_leftHandCollider);
		}
		else if (_isRightHandActive && _rightHandCollider != null)
		{
			CheckMeleeHitbox(_rightHandCollider);
		}
	}

	#region Xoay Bám Mục Tiêu (Attack Tracking)

	private void HandleAttackTracking()
	{
		Vector3 direction = (_currentTarget.position - transform.position).normalized;
		direction.y = 0; // Triệt tiêu trục Y để Boss không ngóc lên cắm xuống

		if (direction != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(direction);
			// Xoay từ từ mượt mà thay vì giật cục
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _attackRotationSpeed * Time.deltaTime);
		}
	}

	/// <summary>
	/// [MỚI] GẮN VÀO ANIMATION EVENT: Gọi hàm này ở frame mà Boss bắt đầu vung tay xuống (hoặc ném đá đi).
	/// Giúp khóa hướng đánh lại, tạo cơ hội cho Player lộn nhào né đòn.
	/// </summary>
	public void AnimEvent_StopTracking()
	{
		_isTrackingTarget = false;
	}

	/// <summary>
	/// [MỚI] GẮN VÀO ANIMATION EVENT: Gọi hàm này để BẬT LẠI khả năng xoay bám mục tiêu.
	/// Dùng cho các chiêu thức kéo dài hoặc combo nhiều nhịp.
	/// </summary>
	public void AnimEvent_StartTracking()
	{
		_isTrackingTarget = true;
	}

	#endregion

	#region Chống kẹt Animation
	public void CancelPendingAttacks()
	{
		_animator.ResetTrigger("AttackRightHand");
		_animator.ResetTrigger("AttackLeftHand");
		_animator.ResetTrigger("UnearthRock");
		_animator.ResetTrigger("Summon");
		_animator.SetBool("ThrowRock", false);
		IsAttacking = false;
		_isTrackingTarget = false; // Reset tracking
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
		IsAttacking = true;
		_isStunned = false;

		// [CẬP NHẬT] Lưu Target và cho phép tracking
		_currentTarget = target;
		_isTrackingTarget = true;

		if (CheckHandAttackRange(target, out bool isRight))
		{
			string triggerName = isRight ? "AttackRightHand" : "AttackLeftHand";
			_animator.SetTrigger(triggerName);
			
			// Delay sound một chút để khớp với animation
			if (_audioSource && _swipeSound)
				StartCoroutine(PlaySoundDelayed(_swipeSound, 0.7f));

			_lastHandAttackTime = Time.time;
			return true;
		}

		IsAttacking = false;
		_isTrackingTarget = false;
		return false;
	}

	public void AnimEvent_EnableLeftHand()
	{
		_hasDealtHandDamage = false;
		_isLeftHandActive = true;
	}

	public void AnimEvent_EnableRightHand()
	{
		_hasDealtHandDamage = false;
		_isRightHandActive = true;
	}

	public void AnimEvent_DisableLeftHand() => _isLeftHandActive = false;
	public void AnimEvent_DisableRightHand() => _isRightHandActive = false;

	#endregion

	#region XỬ LÝ VA CHẠM CẬN CHIẾN (OverlapBox)

	private void CheckMeleeHitbox(BoxCollider handCol)
	{
		Vector3 center = handCol.transform.TransformPoint(handCol.center);
		Vector3 halfExtents = Vector3.Scale(handCol.size, handCol.transform.lossyScale) * 0.5f;
		halfExtents *= 1.2f;

		Collider[] hits = Physics.OverlapBox(center, halfExtents, handCol.transform.rotation);

		foreach (var hit in hits)
		{
			if (hit.CompareTag("Player"))
			{
				PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
				if (playerHealth != null)
				{
					playerHealth.TakeDamage(_handDamage);
					Debug.Log($"[{name}] Tát trúng Player (OverlapBox)! Gây {_handDamage} sát thương.");
				}

				_hasDealtHandDamage = true;
				break;
			}
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
		IsAttacking = true;
		_isStunned = false;

		// [CẬP NHẬT]
		_currentTarget = target;
		_isTrackingTarget = true;

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
	
	#region Skill 3: Summon Minions (Triệu hồi)
	
	public bool CheckSummonCondition()
	{
		if (_health == null) return false;
		float healthPercent = (float)_health.curentHealth / _health.maxHealth;
		
		if (healthPercent <= _summonFirstime && !_hasSummonedAt60) return true;
		if (healthPercent <= _summonSecondtime && !_hasSummonedAt40) return true;

		return false;
	}

	public bool ExecuteSummon()
	{
		if (!CheckSummonCondition()) return false; 

		CancelPendingAttacks();
		
		IsAttacking = true;
		_isStunned = false;
		_isTrackingTarget = false;

		_animator.SetFloat("Turn", 0f);

		float healthPercent = (float)_health.curentHealth / _health.maxHealth;
		
		if (healthPercent <= _summonSecondtime) 
		{
			_hasSummonedAt60 = true;
			_hasSummonedAt40 = true;
		}
		else if (healthPercent <= _summonFirstime) 
		{
			_hasSummonedAt60 = true;
		}

		_animator.SetTrigger("Summon");
		
		if (_audioSource && _summonSound)
			StartCoroutine(PlaySoundDelayed(_summonSound, _summonSoundDelay));

		return true;
	}
	
	public void AnimEvent_SpawnMinions()
	{
		if (_minionPrefabs == null || _minionPrefabs.Length == 0 || _isStunned) return;

		for (int i = 0; i < _minionCount; i++)
		{
			Vector2 randomCircle = Random.insideUnitCircle * _summonRadius;
			Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
			if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
			{
				int randomIndex = Random.Range(0, _minionPrefabs.Length);
				GameObject minion = Instantiate(_minionPrefabs[randomIndex], hit.position, Quaternion.identity);
				
				// Tuỳ chọn: Kích hoạt hiệu ứng Spawn của Minion tại đây nếu Minion có Script riêng
			}
		}
	}
	#endregion

	#region Tương tác Đặc biệt (Choáng & Ngắt đòn)
	public void TriggerStun()
	{
		_isStunned = true;
		IsAttacking = false;
		_isTrackingTarget = false; // Ngừng xoay nếu bị choáng

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

		_isLeftHandActive = false;
		_isRightHandActive = false;
	}

	public bool IsAnySkillReady(Transform target)
	{
		if (target == null || IsAttacking || _isStunned || IsBusyTurning()) return false;

		if (CheckSummonCondition()) return true;
		
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

	#region Utility Events & Debug
	public void AnimEvent_EndAttack()
	{
		IsAttacking = false;
		_isTrackingTarget = false; // Đảm bảo reset tracking khi kết thúc
		_isLeftHandActive = false;
		_isRightHandActive = false;
	}

	private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (!_isStunned) _audioSource.PlayOneShot(clip);
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying || !_hasDealtHandDamage)
		{
			Gizmos.color = new Color(1, 0, 0, 0.4f);
			if (_isLeftHandActive && _leftHandCollider != null) DrawBoxCastGizmo(_leftHandCollider);
			if (_isRightHandActive && _rightHandCollider != null) DrawBoxCastGizmo(_rightHandCollider);
		}
	}

	private void DrawBoxCastGizmo(BoxCollider box)
	{
		Gizmos.matrix = Matrix4x4.TRS(box.transform.TransformPoint(box.center), box.transform.rotation, box.transform.lossyScale);
		Gizmos.DrawWireCube(Vector3.zero, box.size * 1.2f);
	}
	#endregion
}