using UnityEngine;
using UnityEngine.AI;
using System.Collections;


public class SpiderAgent : MonoBehaviour
{
	[Header("Configuration")]
	private float _turnThreshold = 60f; // Góc lệch > 60 độ sẽ kích hoạt xoay tại chỗ
	private float _turnSmoothTime = 0.2f;
	private float _stopDistance = 0.5f;

	private NavMeshAgent _agent;
	private Animator _animator;
	private bool _isOffMesh;

	// Animator Hashes (Tối ưu hiệu năng so với dùng string)
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");
	private readonly int _hashIsMoving = Animator.StringToHash("IsMoving");

	public bool IsMoving => _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance;

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		// Cấu hình bắt buộc cho Root Motion Agent
		_agent.updateRotation = false; // Tắt xoay của NavMesh để Animator tự xử lý
		_agent.updatePosition = true;
	}

	// Hàm public để Behavior Graph hoặc Input Script gọi
	public void MoveTo(Vector3 targetPosition)
	{
		_agent.SetDestination(targetPosition);
		_agent.isStopped = false;
	}

	public void Stop()
	{
		if (_agent.hasPath) _agent.ResetPath();
		_agent.isStopped = true;

		// Reset animator về Idle
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetBool(_hashIsMoving, false);
	}

	private void Update()
	{
		HandleOffMeshLink();
		HandleMovementAndRotation();
	}

	private void HandleMovementAndRotation()
	{
		if (!_agent.hasPath || _isOffMesh)
		{
			// Dừng Animation nếu không có đường đi
			_animator.SetBool(_hashIsMoving, false);
			_animator.SetFloat(_hashHorizontal, 0, 0.2f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, 0, 0.2f, Time.deltaTime);
			return;
		}

		// Kiểm tra đích đến
		if (_agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// 1. Tính toán vector hướng
		// steeringTarget là điểm tiếp theo trên đường đi (góc cua)
		Vector3 directionToTarget = (_agent.steeringTarget - transform.position).normalized;

		// Chuyển hướng thế giới sang hướng cục bộ của Nhện
		Vector3 localDir = transform.InverseTransformDirection(directionToTarget);

		// Tính góc lệch giữa mặt nhện và mục tiêu
		float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

		// 2. Logic điều khiển Animation
		// Nếu góc quá lớn (>60 độ), ưu tiên xoay tại chỗ (nếu nhện đang đứng yên hoặc đi rất chậm)
		bool needTurnInPlace = Mathf.Abs(angle) > _turnThreshold;

		if (needTurnInPlace)
		{
			// Gửi tín hiệu xoay sang Animator (Turn Blend Tree)
			// angle > 0 là phải, angle < 0 là trái
			float turnVal = Mathf.Clamp(angle / 90f, -1f, 1f);
			_animator.SetFloat(_hashTurn, turnVal, _turnSmoothTime, Time.deltaTime);

			// Giảm tốc độ đi thẳng khi đang xoay gắt
			_animator.SetFloat(_hashVertical, 0, 0.2f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, 0, 0.2f, Time.deltaTime);
		}
		else
		{
			// Góc nhỏ, cho phép di chuyển + Strafing
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);

			// localDir.z là đi tới/lùi, localDir.x là đi ngang (Strafing)
			_animator.SetFloat(_hashVertical, localDir.z, 0.2f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, localDir.x, 0.2f, Time.deltaTime);
		}

		_animator.SetBool(_hashIsMoving, true);

		// 3. Hỗ trợ xoay Transform mượt mà (Bổ trợ cho Root Motion)
		// Root Motion sẽ xoay model, nhưng ta phụ trợ thêm để đảm bảo chính xác
		if (directionToTarget != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
			// Xoay chậm hơn nếu đang di chuyển để tạo độ trễ tự nhiên
			float rotateSpeed = needTurnInPlace ? 2.0f : 5.0f;
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
		}
	}

	private void HandleOffMeshLink()
	{
		if (_agent.isOnOffMeshLink && !_isOffMesh)
		{
			_isOffMesh = true;
			StartCoroutine(DoOffMeshLink(_agent.currentOffMeshLinkData));
		}
	}

	private IEnumerator DoOffMeshLink(OffMeshLinkData data)
	{
		//... (Giữ nguyên logic Jump cũ của bạn ở đây)
		// Lưu ý: Nhớ reset _isOffMesh = false và gọi _agent.CompleteOffMeshLink() khi xong
		yield return null;
		_agent.CompleteOffMeshLink();
		_isOffMesh = false;
	}
}