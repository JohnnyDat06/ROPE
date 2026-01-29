using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ArathroxMovement : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Góc lệch lớn hơn giá trị này sẽ BẮT ĐẦU xoay tại chỗ")]
	[SerializeField] private float _turnStartThreshold = 45f;

	[Tooltip("Góc lệch nhỏ hơn giá trị này sẽ KẾT THÚC xoay tại chỗ")]
	[SerializeField] private float _turnEndThreshold = 10f;

	[Tooltip("Tốc độ xoay thủ công khi đang di chuyển (độ/giây)")]
	[SerializeField] private float _alignSpeed = 120f;

	[Tooltip("Khoảng cách chấp nhận đã đến đích")]
	[SerializeField] private float _stopDistance = 0.5f;

	// Components
	private NavMeshAgent _agent;
	private Animator _animator;

	// State Variables
	private bool _isTurningInPlace; // Biến khóa trạng thái xoay
	private bool _hasTarget;

	// Animator Hashes
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");
	private readonly int _hashIsMoving = Animator.StringToHash("IsMoving"); // Parameter mới

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		// QUAN TRỌNG: Tắt hoàn toàn việc Agent tự điều khiển Transform
		// Để Root Motion của Animation chịu trách nhiệm di chuyển và xoay
		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

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

		// Reset animator
		_animator.SetBool(_hashIsMoving, false);
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
	}

	private void Update()
	{
		if (!_hasTarget) return;

		// 1. Kiểm tra đích đến
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// 2. Tính toán góc lệch
		Vector3 targetDir = (_agent.steeringTarget - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) targetDir = transform.forward;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// --- STATE MACHINE LOGIC ---

		// Kiểm tra điều kiện để BẮT ĐẦU hoặc KẾT THÚC xoay tại chỗ
		if (!_isTurningInPlace)
		{
			// Nếu đang đi mà góc quá lớn > 45 -> Vào trạng thái xoay
			if (absAngle > _turnStartThreshold)
			{
				_isTurningInPlace = true;
			}
		}
		else
		{
			// Nếu đang xoay, chỉ thoát khi góc đã đủ nhỏ < 10
			if (absAngle < _turnEndThreshold)
			{
				_isTurningInPlace = false;
			}
		}

		// --- XỬ LÝ HÀNH VI DỰA TRÊN TRẠNG THÁI ---

		if (_isTurningInPlace)
		{
			// TRẠNG THÁI: TURN IN PLACE
			// Dừng di chuyển logic (để Animation Turn hoạt động đúng chỗ)
			_animator.SetBool(_hashIsMoving, false);

			// Cập nhật giá trị Turn (-1 hoặc 1)
			float turnVal = Mathf.Sign(signedAngle);
			_animator.SetFloat(_hashTurn, turnVal, 0.1f, Time.deltaTime);

			// Reset Locomotion
			_animator.SetFloat(_hashHorizontal, 0);
			_animator.SetFloat(_hashVertical, 0);
		}
		else
		{
			// TRẠNG THÁI: LOCOMOTION
			_animator.SetBool(_hashIsMoving, true);
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime); // Trả Turn về 0

			// Hỗ trợ xoay thủ công cho thẳng hàng (chỉ khi góc < 10 độ nhưng > 0)
			// Logic này giúp nhân vật luôn hướng mặt về đích khi di chuyển
			if (absAngle > 0.1f)
			{
				Quaternion targetRot = Quaternion.LookRotation(targetDir);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _alignSpeed * Time.deltaTime);
			}

			// Tính toán Input cho Blend Tree Locomotion
			// Vì dùng Root Motion, ta lấy hướng mong muốn từ Agent (Desired Velocity)
			// để bảo Animator biết nên chạy hướng nào
			Vector3 desiredVel = _agent.desiredVelocity; // Vận tốc mong muốn của Agent
			Vector3 localVel = transform.InverseTransformDirection(desiredVel);

			// Normalize để lấy hướng (-1 đến 1) thay vì độ lớn vận tốc
			// (Vì tốc độ thực tế sẽ do Root Motion quyết định)
			float speedFactor = _agent.speed > 0 ? _agent.speed : 1f;
			Vector3 normalizedInput = localVel / speedFactor;

			_animator.SetFloat(_hashHorizontal, normalizedInput.x, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, normalizedInput.z, 0.1f, Time.deltaTime);
		}
	}

	// --- XỬ LÝ ROOT MOTION ĐÃ NÂNG CẤP ---
	private void OnAnimatorMove()
	{
		// 1. Tính toán vị trí dự kiến dựa trên Animation (chỉ X và Z)
		Vector3 newPos = transform.position + _animator.deltaPosition;

		// 2. Xử lý độ cao (Y-Axis) để bám địa hình/cầu thang
		// Tìm điểm gần nhất trên NavMesh trong bán kính nhỏ (VD: 1.0f) tại vị trí newPos
		NavMeshHit hit;

		// SamplePosition giúp tìm độ cao thực tế của sàn NavMesh tại tọa độ X,Z đó
		if (NavMesh.SamplePosition(newPos, out hit, 1.0f, NavMesh.AllAreas))
		{
			// Gán lại độ cao Y của nhân vật bằng độ cao của NavMesh
			// Dùng Lerp nhẹ để khi leo cầu thang không bị giật cục (snap) quá gắt
			float targetY = hit.position.y;
			newPos.y = Mathf.Lerp(transform.position.y, targetY, 20f * Time.deltaTime);
		}

		// 3. Áp dụng vị trí mới
		transform.position = newPos;

		// 4. Áp dụng xoay từ Animation
		transform.rotation *= _animator.deltaRotation;

		// 5. ĐỒNG BỘ NGƯỢC: Kéo NavMeshAgent theo vị trí thực tế của nhân vật
		_agent.nextPosition = transform.position;
	}

	private void OnDrawGizmosSelected()
	{
		if (_agent != null && _agent.hasPath)
		{
			Gizmos.color = _isTurningInPlace ? Color.red : Color.green;
			Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
			Gizmos.DrawLine(transform.position, _agent.steeringTarget);
		}
	}
}