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
	[Tooltip("Thời gian trượt để dừng hẳn/đạt tốc độ tối đa. Càng lớn Boss càng ì ạch.")]
	[SerializeField] private float _heavySmoothTime = 0.6f;

	[Tooltip("Khoảng cách dư ra trước khi Boss lùi lại.")]
	[SerializeField] private float _backupDistanceThreshold = 1.5f;
	#endregion

	#region Internal State
	// [CẬP NHẬT] Sử dụng Local Momentum (Float) thay vì World Vector3 để tránh lỗi trượt ngang (Strafing)
	private float _currentVerticalMomentum = 0f;
	private float _currentHorizontalMomentum = 0f;

	// Các biến tham chiếu cho SmoothDamp
	private float _verticalVelocityRef;
	private float _horizontalVelocityRef;
	#endregion

	#region Public Methods (API)

	/// <summary>
	/// Xử lý di chuyển nặng nề trong Combat.
	/// </summary>
	public void HandleHeavyCombatMovement(Transform target, float idealRange)
	{
		if (_hasTarget)
		{
			_hasTarget = false;
			_agent.isStopped = true;
		}

		// 1. Nếu Boss cần vặn người tại chỗ, nó sẽ tự động giảm tốc từ từ
		if (TryTurnInPlace(target.position))
		{
			return;
		}

		// 2. Nếu không xoay, tiến hành xoay mặt và di chuyển tiến/lùi
		RotateTowards(target.position);

		float targetVertical = CalculateVerticalForce(target.position, idealRange);
		float targetHorizontal = 0f; // Boss khổng lồ không đi ngang (Strafe)

		// Cập nhật gia tốc mượt mà
		UpdateMomentum(targetVertical, targetHorizontal);
	}

	/// <summary>
	/// [MỚI] Hàm phanh mượt mà. 
	/// Behavior Node "Smooth Stop" cần gọi hàm này liên tục trong OnUpdate().
	/// Trả về TRUE khi Boss đã dừng hẳn, lúc này Node mới được return Status.Success.
	/// </summary>
	public bool ExecuteSmoothStop()
	{
		// Kéo dần vận tốc hiện tại về 0
		UpdateMomentum(0f, 0f);

		// Kiểm tra xem đã dừng hẳn chưa (sai số 0.05f)
		bool isStopped = Mathf.Abs(_currentVerticalMomentum) < 0.05f && Mathf.Abs(_currentHorizontalMomentum) < 0.05f;

		if (isStopped)
		{
			// Đảm bảo ép về 0 tròn trĩnh khi đã trượt xong
			_currentVerticalMomentum = 0f;
			_currentHorizontalMomentum = 0f;
			_animator.SetFloat(_hashHorizontal, 0f);
			_animator.SetFloat(_hashVertical, 0f);
			_animator.SetBool(_hashIsMoving, false);
		}

		return isStopped;
	}

	#endregion

	#region Helper Logic

	/// <summary>
	/// Cập nhật vận tốc Local vào Animator thông qua SmoothDamp
	/// </summary>
	private void UpdateMomentum(float targetVertical, float targetHorizontal)
	{
		_currentVerticalMomentum = Mathf.SmoothDamp(
			_currentVerticalMomentum,
			targetVertical,
			ref _verticalVelocityRef,
			_heavySmoothTime
		);

		_currentHorizontalMomentum = Mathf.SmoothDamp(
			_currentHorizontalMomentum,
			targetHorizontal,
			ref _horizontalVelocityRef,
			_heavySmoothTime
		);

		_animator.SetBool(_hashIsMoving, Mathf.Abs(_currentVerticalMomentum) > 0.05f || Mathf.Abs(_currentHorizontalMomentum) > 0.05f);

		// Truyền trực tiếp vào Animator (DampTime = 0 vì đã làm mượt bằng toán học ở trên)
		_animator.SetFloat(_hashHorizontal, _currentHorizontalMomentum, 0f, Time.deltaTime);
		_animator.SetFloat(_hashVertical, _currentVerticalMomentum, 0f, Time.deltaTime);
	}

	/// <summary>
	/// Trả về 1 (Tiến), -1 (Lùi) hoặc 0 (Đứng yên)
	/// </summary>
	private float CalculateVerticalForce(Vector3 targetPos, float idealRange)
	{
		float distance = Vector3.Distance(transform.position, targetPos);

		if (distance > idealRange + _backupDistanceThreshold) return 1f;

		if (distance < idealRange - _backupDistanceThreshold)
		{
			Vector3 backPos = transform.position - transform.forward * 1.5f;
			if (_agent.isOnNavMesh && _agent.Raycast(backPos, out UnityEngine.AI.NavMeshHit hit))
			{
				return 0f;
			}
			return -1f;
		}

		return 0f;
	}

	#endregion

	#region Base Overrides

	/// <summary>
	/// Ghi đè logic Turn In Place. Bao gồm phanh mượt mà và Hỗ trợ xoay 180 độ.
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
			// [BÍ QUYẾT TẠO QUÁN TÍNH]
			// Trong lúc đang vặn người, tiếp tục kéo đà di chuyển về 0 thay vì đứng khựng lại
			UpdateMomentum(0f, 0f);
			_animator.SetBool(_hashIsMoving, false);

			// [MỚI] LOGIC XOAY 180 ĐỘ
			// Nếu góc cần xoay > 90 độ, ta truyền giá trị 2 (hoặc -2) vào Animator Blend Tree.
			// Nếu <= 90 độ, ta truyền giá trị 1 (hoặc -1).
			float turnMagnitude = absAngle > 90f ? 2f : 1f;
			float finalTurnValue = Mathf.Sign(signedAngle) * turnMagnitude;

			_animator.SetFloat(_hashTurn, finalTurnValue, 0.1f, Time.deltaTime);

			return true;
		}

		_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);
		return false;
	}

	public override void Stop()
	{
		// Cập nhật lại đà di chuyển thực tế từ Animator trước khi Base xóa nó
		_currentVerticalMomentum = _animator.GetFloat(_hashVertical);
		_currentHorizontalMomentum = _animator.GetFloat(_hashHorizontal);

		base.Stop();
	}

	protected override void OnWallHit()
	{
		_currentVerticalMomentum = 0f;
		_currentHorizontalMomentum = 0f;
		_verticalVelocityRef = 0f;
		_horizontalVelocityRef = 0f;
	}

	protected override void ResetAnimator()
	{
		base.ResetAnimator();
		_currentVerticalMomentum = 0f;
		_currentHorizontalMomentum = 0f;
		_verticalVelocityRef = 0f;
		_horizontalVelocityRef = 0f;
	}

	#endregion
}