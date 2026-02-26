using UnityEngine;

/// <summary>
/// Movement logic for the giant boss "Crustaspikan".
/// Inherits basic locomotion from EnemyMovement but implements heavy, high-momentum combat steering.
/// Removes strafing mechanics and focuses on smooth acceleration/deceleration.
/// </summary>
public class CrustaspikanMovement : EnemyMovement
{
	#region Configuration (Boss Specific)
	[Header("Heavy Boss Settings")]
	[Tooltip("Time to smooth damp the input vector. Higher values = heavier feel, slower to stop (gorilla momentum).")]
	[SerializeField] private float _heavySmoothTime = 0.6f;

	[Tooltip("Distance threshold to trigger backing up. Giant bosses rarely back up, so this can be small.")]
	[SerializeField] private float _backupDistanceThreshold = 1.5f;
	#endregion

	#region Internal State
	// Smoothing Variables for Damping
	private Vector3 _smoothDampVelocity;
	private Vector3 _currentSmoothInput;

	// Flag to detect state transition for seamless animation blending
	private bool _isEnteringCombat = true;
	#endregion

	#region Public Methods (API)

	/// <summary>
	/// Handles heavy combat movement. Strict forward/backward movement with large momentum.
	/// </summary>
	/// <param name="target">The player transform.</param>
	/// <param name="idealRange">The distance the boss wants to maintain before attacking.</param>
	public void HandleHeavyCombatMovement(Transform target, float idealRange)
	{
		if (_hasTarget)
		{
			_hasTarget = false;
			_agent.isStopped = true;
		}

		// [FIX 1] Bỏ đoạn lấy _animator.GetFloat() ở đây.
		// Vì biến _currentSmoothInput ĐÃ ĐƯỢC GIỮ LẠI từ hàm Stop() ở trên rồi!
		if (_isEnteringCombat)
		{
			_isEnteringCombat = false;
		}

		// [FIX HOÀN HẢO] 
		// Gọi hàm TryTurnInPlace mà ta vừa ghi đè. Nó sẽ tự động lo việc vừa vặn người vừa phanh!
		if (TryTurnInPlace(target.position))
		{
			_isEnteringCombat = true;
			return; // Đang bận xoay và phanh, không xử lý tiến lùi nữa
		}

		//// [FIX 2] Xử lý Turn In Place mượt mà
		//if (TryTurnInPlace(target.position))
		//{
		//	// Thay vì giật về Vector3.zero lập tức, ta cho nó trượt từ từ về 0
		//	// Boss sẽ lê chân chậm lại trong khi vặn người
		//	_currentSmoothInput = Vector3.SmoothDamp(
		//		_currentSmoothInput,
		//		Vector3.zero,
		//		ref _smoothDampVelocity,
		//		_heavySmoothTime
		//	);
		//}
		else
		{
			RotateTowards(target.position);

			Vector3 targetDirection = CalculateRangeForce(target.position, idealRange);

			_currentSmoothInput = Vector3.SmoothDamp(
				_currentSmoothInput,
				targetDirection,
				ref _smoothDampVelocity,
				_heavySmoothTime
			);
		}

		// --- APPLY TO ANIMATOR ---
		Vector3 localInput = transform.InverseTransformDirection(_currentSmoothInput);

		_animator.SetBool(_hashIsMoving, _currentSmoothInput.magnitude > 0.05f);

		// [FIX 3] Bỏ Double Smoothing! 
		// Vì _currentSmoothInput đã tự tính toán độ trượt, ta chỉ cần truyền thẳng vào Animator (Damp = 0)
		_animator.SetFloat(_hashHorizontal, localInput.x, 0f, Time.deltaTime);
		_animator.SetFloat(_hashVertical, localInput.z, 0f, Time.deltaTime);

		if (Application.isEditor)
		{
			Debug.DrawRay(transform.position + Vector3.up * 2f, _currentSmoothInput * 3, Color.cyan);
		}
	}

	#endregion

	#region Helper Logic

	/// <summary>
	/// Calculates movement towards or away from the target based on ideal range.
	/// </summary>
	private Vector3 CalculateRangeForce(Vector3 targetPos, float idealRange)
	{
		float distance = Vector3.Distance(transform.position, targetPos);

		// Move closer if too far
		if (distance > idealRange + _backupDistanceThreshold)
			return transform.forward;

		// Slowly back up if too close (Optional for giant bosses)
		if (distance < idealRange - _backupDistanceThreshold)
		{
			// Check if backing up is safe (from base class logic)
			Vector3 backPos = transform.position - transform.forward * 1.5f;
			if (_agent.isOnNavMesh && _agent.Raycast(backPos, out UnityEngine.AI.NavMeshHit hit))
			{
				return Vector3.zero; // Wall behind, stop
			}
			return -transform.forward;
		}

		// In the "sweet spot". Return zero. 
		// The SmoothDamp will cause the boss to slide to a halt naturally.
		return Vector3.zero;
	}

	#endregion

	#region Base Overrides

	/// <summary>
	/// Resets the combat flag when returning to standard NavMesh movement.
	/// </summary>
	public override void MoveTo(Vector3 position)
	{
		base.MoveTo(position);
		_isEnteringCombat = true; // Ready for next transition
	}

	/// <summary>
	/// Ghi đè logic Turn In Place của lớp cha.
	/// Boss khổng lồ sẽ VỪA phát Animation xoay, VỪA lê bước trượt tới trước (giảm tốc từ từ) thay vì khựng lại.
	/// </summary>
	/// <summary>
	/// Ghi đè logic Turn In Place của lớp cha.
	/// Boss khổng lồ sẽ VỪA phát Animation xoay, VỪA lê bước trượt tới trước (giảm tốc từ từ) thay vì khựng lại.
	/// </summary>
	protected override bool TryTurnInPlace(Vector3 targetPos)
	{
		if (!_useTurnInPlace) return false;

		Vector3 targetDir = (targetPos - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) return false;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		if (!_isTurningInPlace && absAngle > _turnStartThreshold) _isTurningInPlace = true;
		else if (_isTurningInPlace && absAngle < _turnEndThreshold) _isTurningInPlace = false;

		if (_isTurningInPlace)
		{
			// 1. Kích hoạt Animation vặn người (Turn)
			_animator.SetBool(_hashIsMoving, false);
			_animator.SetFloat(_hashTurn, Mathf.Sign(signedAngle), 0.1f, Time.deltaTime);

			// 2. [BÍ QUYẾT TẠO QUÁN TÍNH CHO BOSS]
			// KHÔNG ép Vertical về 0 lập tức như lớp cha.
			// Dùng SmoothDamp kéo vận tốc hiện tại từ từ về 0.
			_currentSmoothInput = Vector3.SmoothDamp(
				_currentSmoothInput,
				Vector3.zero, // Đích đến là dừng lại
				ref _smoothDampVelocity,
				_heavySmoothTime // Dùng độ nặng (momentum) của Boss
			);

			// 3. Truyền vận tốc đang giảm dần này vào Animator
			Vector3 localInput = transform.InverseTransformDirection(_currentSmoothInput);
			_animator.SetFloat(_hashHorizontal, localInput.x, 0f, Time.deltaTime); // Damp = 0 vì đã SmoothDamp ở trên
			_animator.SetFloat(_hashVertical, localInput.z, 0f, Time.deltaTime);

			return true; // Đang trong quá trình xoay
		}

		_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);
		return false;
	}

	// 2. Định nghĩa lại hành vi Stop
	public override void Stop()
	{
		// Vẫn lưu lại đà di chuyển trước khi gọi base.Stop()
		float currentV = _animator.GetFloat(_hashVertical);

		base.Stop();

		// Giữ lại quán tính để frame sau Boss trượt tiếp
		_currentSmoothInput = transform.forward * currentV;
		_isEnteringCombat = true;
	}

	/// <summary>
	/// Overrides the base Wall Hit hook to kill momentum when hitting a wall.
	/// </summary>
	protected override void OnWallHit()
	{
		// A giant boss hitting a wall loses its forward momentum instantly
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
	}

	protected override void ResetAnimator()
	{
		base.ResetAnimator();
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
	}

	#endregion
}