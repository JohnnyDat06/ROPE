using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ArathroxMovement : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Góc lệch lớn hơn giá trị này sẽ kích hoạt xoay tại chỗ")]
	[SerializeField] private float _turnThreshold = 45f;

	[Tooltip("Góc lệch nhỏ hơn giá trị này sẽ kích hoạt xoay chỉnh hướng nhẹ khi đang đi")]
	[SerializeField] private float _alignThreshold = 10f;

	[Tooltip("Tốc độ xoay thủ công khi góc lệch nhỏ (độ/giây)")]
	[SerializeField] private float _alignSpeed = 120f;

	[Tooltip("Khoảng cách chấp nhận đã đến đích")]
	[SerializeField] private float _stopDistance = 0.5f;

	// Components
	private NavMeshAgent _agent;
	private Animator _animator;

	// State Variables
	private bool _isTurningInPlace;
	private Vector3 _lookTarget;
	private bool _hasTarget;

	// Animator Hashes (Tối ưu hiệu năng thay vì dùng string)
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		// Cấu hình Agent để ta tự kiểm soát việc xoay
		_agent.updateRotation = false;
		_agent.updatePosition = true; // Vẫn để Agent lo vị trí khi di chuyển
	}

	/// <summary>
	/// Hàm API để các script khác (Input, AI) gọi
	/// </summary>
	public void MoveTo(Vector3 position)
	{
		_agent.SetDestination(position);
		_hasTarget = true;
		_agent.isStopped = false;
	}

	public void Stop()
	{
		_agent.ResetPath();
		_hasTarget = false;
		_agent.isStopped = true;

		// Reset animator về Idle
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
	}

	private void Update()
	{
		if (!_hasTarget) return;

		// 1. Kiểm tra nếu đã đến đích
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// 2. Tính toán góc lệch so với điểm tiếp theo trên đường đi (SteeringTarget)
		// Lưu ý: Dùng steeringTarget thay vì destination để xử lý NavMesh Corner tốt hơn
		Vector3 targetDir = (_agent.steeringTarget - transform.position).normalized;

		// Loại bỏ trục Y để tính góc phẳng (tránh lỗi khi địa hình dốc)
		targetDir.y = 0;
		if (targetDir == Vector3.zero) targetDir = transform.forward;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// --- LOGIC XỬ LÝ THEO GÓC ---

		// CASE 1: Góc lớn (> 45 độ) -> Xoay tại chỗ (Root Motion)
		if (absAngle > _turnThreshold)
		{
			_isTurningInPlace = true;
			_agent.isStopped = true; // Dừng di chuyển vị trí của Agent

			// Truyền tham số xoay cho Animator (-1 là Trái, 1 là Phải)
			// SignedAngle dương là bên phải (Right), âm là trái (Left) 
			// Blend Tree: -1 (Left), 1 (Right) -> Logic này khớp
			float turnVal = Mathf.Sign(signedAngle);
			_animator.SetFloat(_hashTurn, turnVal, 0.1f, Time.deltaTime);

			// Reset Locomotion để tránh trôi
			_animator.SetFloat(_hashHorizontal, 0);
			_animator.SetFloat(_hashVertical, 0);
		}
		// CASE 2 & 3: Góc nhỏ hoặc trung bình (<= 45 độ) -> Di chuyển
		else
		{
			_isTurningInPlace = false;
			_agent.isStopped = false; // Agent tiếp tục đẩy nhân vật đi
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime); // Tắt xoay tại chỗ

			// CASE 3: Góc siêu nhỏ (< 10 độ) -> Hỗ trợ xoay thủ công cho thẳng hàng
			if (absAngle < _alignThreshold)
			{
				Quaternion targetRot = Quaternion.LookRotation(targetDir);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _alignSpeed * Time.deltaTime);
			}
			// CASE 2 (10-45 độ): Không làm gì cả, để Locomotion tự lo hướng chéo

			// --- XỬ LÝ LOCOMOTION BLEND TREE ---
			// Chuyển vận tốc thế giới (World Velocity) sang cục bộ (Local Velocity)
			// Agent.velocity mượt hơn SteeringTarget cho animation
			Vector3 localVel = transform.InverseTransformDirection(_agent.velocity);

			// Normalize velocity theo speed tối đa của agent để map vào Blend Tree (0-1)
			// Tránh chia cho 0
			float speedFactor = _agent.speed > 0 ? _agent.speed : 1f;
			Vector3 normalizedLocalVel = localVel / speedFactor;

			_animator.SetFloat(_hashHorizontal, normalizedLocalVel.x, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, normalizedLocalVel.z, 0.1f, Time.deltaTime);
		}
	}

	// Xử lý Root Motion khi đang Xoay tại chỗ
	private void OnAnimatorMove()
	{
		// Chỉ áp dụng Root Motion cho Rotation khi đang xoay tại chỗ
		if (_isTurningInPlace)
		{
			// Cộng dồn độ xoay từ Animation vào Transform thực
			transform.rotation *= _animator.deltaRotation;

			// Tùy chọn: Nếu Animation xoay có dịch chuyển vị trí nhẹ, có thể áp dụng:
			// transform.position += _animator.deltaPosition;
		}
		else
		{
			// Khi đang di chuyển Locomotion, để NavMeshAgent lo Position.
			// Nhưng cần đồng bộ vị trí Agent với Animation nếu có sai lệch (thường không cần nếu setup chuẩn)
			_agent.velocity = _animator.deltaPosition / Time.deltaTime; // Nếu muốn dùng Root Motion Move hoàn toàn
		}
	}

	// Debug trực quan
	private void OnDrawGizmosSelected()
	{
		if (_agent != null && _agent.hasPath)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, _agent.steeringTarget);

			// Vẽ góc Threshold
			Vector3 leftDir = Quaternion.Euler(0, -_turnThreshold, 0) * transform.forward;
			Vector3 rightDir = Quaternion.Euler(0, _turnThreshold, 0) * transform.forward;
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(transform.position, leftDir * 2);
			Gizmos.DrawRay(transform.position, rightDir * 2);
		}
	}
}