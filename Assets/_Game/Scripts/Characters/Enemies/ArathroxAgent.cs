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
		// 1. Kiểm tra điều kiện cơ bản
		if (!_agent.hasPath || _isOffMesh || _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// 2. Tính toán Vector hướng
		Vector3 directionToTarget = (_agent.steeringTarget - transform.position).normalized;
		Vector3 localDir = transform.InverseTransformDirection(directionToTarget);
		float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

		// 3. Logic Quyết định: Xoay hay Đi?
		// Nếu góc lệch > 45 độ, coi như cần "Xoay tại chỗ"
		bool needTurnInPlace = Mathf.Abs(angle) > 45f;

		if (needTurnInPlace)
		{
			// --- TRẠNG THÁI XOAY ---

			// Tạm dừng NavMeshAgent để không bị trượt khi đang xoay
			_agent.isStopped = true;

			// Báo cho Animator biết là đang ĐỨNG YÊN (để chuyển sang state Turn/Idle)
			_animator.SetBool(_hashIsMoving, false);

			// Gửi giá trị Turn (-1 trái, 1 phải)
			// Chia cho 90 để chuẩn hóa giá trị về khoảng -1 đến 1
			float turnVal = Mathf.Clamp(angle / 90f, -1f, 1f);
			_animator.SetFloat(_hashTurn, turnVal, _turnSmoothTime, Time.deltaTime);

			// Reset các thông số di chuyển về 0
			_animator.SetFloat(_hashVertical, 0, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, 0, 0.1f, Time.deltaTime);
		}
		else
		{
			// --- TRẠNG THÁI DI CHUYỂN ---

			// Cho phép đi tiếp
			_agent.isStopped = false;

			// Báo cho Animator biết là đang DI CHUYỂN
			_animator.SetBool(_hashIsMoving, true);

			// Reset Turn về 0 (để không bị kẹt ở pose xoay)
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);

			// Cập nhật hướng di chuyển (Strafing)
			_animator.SetFloat(_hashVertical, localDir.z, 0.2f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, localDir.x, 0.2f, Time.deltaTime);
		}

		// 4. Phụ trợ xoay Transform thực tế (quan trọng để góc angle giảm dần theo thời gian)
		if (directionToTarget != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
			// Khi đang đứng xoay (needTurnInPlace), ta xoay nhanh hơn một chút
			float rotateSpeed = needTurnInPlace ? 3.0f : 5.0f;
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
		Vector3 jumpDir = (data.endPos - data.startPos).normalized;
		while (Vector3.Dot(transform.forward, jumpDir) < 0.99f)
		{
			Quaternion goalRot = Quaternion.LookRotation(jumpDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRot, 180f * Time.deltaTime);
			yield return null;
		}

		// Giai đoạn 2: Kích hoạt Animation
		_animator.CrossFade("Jump", 0.2f);

		// Giai đoạn 3: Di chuyển vị trí (Lerp)
		float totalTime = 0.7f; // Thời gian nhảy giả định
		float currentTime = totalTime;

		while (currentTime > 0)
		{
			currentTime -= Time.deltaTime;
			float t = 1 - (currentTime / totalTime);

			Vector3 goalPos = Vector3.Lerp(data.startPos, data.endPos, t);
			float elapsed = totalTime - currentTime;

			// Blend vị trí hiện tại vào quỹ đạo trong 0.3s đầu
			// [00:21:56]
			if (elapsed < 0.3f)
			{
				transform.position = Vector3.Lerp(transform.position, goalPos, elapsed / 0.3f);
			}
			else
			{
				transform.position = goalPos;
			}

			yield return null;
		}
		//... (Giữ nguyên logic Jump cũ của bạn ở đây)
		// Lưu ý: Nhớ reset _isOffMesh = false và gọi _agent.CompleteOffMeshLink() khi xong
		yield return null;
		_agent.CompleteOffMeshLink();
		_isOffMesh = false;
	}
}