using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SpiderAgent : MonoBehaviour
{
	[Header("Configuration")]

	[SerializeField] private float _alignThreshold = 5f;


	private float _correctionTurnSpeed = 120f;

	private float _stopDistance = 0.2f;

	private NavMeshAgent _agent;
	private Animator _animator;
	private bool _isOffMesh;

	// Animator Hashes
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");
	private readonly int _hashIsMoving = Animator.StringToHash("IsMoving");

	public bool IsMoving => _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance;

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		_agent.updateRotation = false;
		_agent.updatePosition = true;
		_agent.angularSpeed = 0; // Tắt xoay của AI
	}

	public void MoveTo(Vector3 targetPosition)
	{
		_agent.SetDestination(targetPosition);
		_agent.isStopped = false;
	}

	public void Stop()
	{
		if (_agent.hasPath) _agent.ResetPath();
		_agent.isStopped = true;
		_agent.velocity = Vector3.zero;

		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
		_animator.SetBool(_hashIsMoving, false);
	}

	private void Update()
	{
		HandleOffMeshLink();
		HandleMovementAndRotation();
	}

	private void HandleMovementAndRotation()
	{
		// 1. Kiểm tra điều kiện dừng
		if (!_agent.hasPath || _isOffMesh || _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// 2. Tính toán góc lệch
		Vector3 vectorToTarget = _agent.steeringTarget - transform.position;
		if (vectorToTarget.sqrMagnitude < 0.01f) return;

		Vector3 directionToTarget = vectorToTarget.normalized;
		Vector3 localDir = transform.InverseTransformDirection(directionToTarget);
		float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

		// 3. Logic: Xoay thuần Root Motion hay Di chuyển + Sửa lỗi?

		// Nếu góc lệch lớn hơn 5 độ -> CHỈ XOAY (Dùng Root Motion)
		if (Mathf.Abs(angle) > _alignThreshold)
		{
			_agent.isStopped = true;
			_agent.velocity = Vector3.zero;

			// Tắt trạng thái di chuyển
			_animator.SetBool(_hashIsMoving, false);

			// Bật Animation Xoay dứt khoát (-1 hoặc 1)
			// Nhện sẽ tự xoay nhờ settings "Apply Root Motion" trong Animator
			float targetTurn = Mathf.Sign(angle);
			_animator.SetFloat(_hashTurn, targetTurn, 0.1f, Time.deltaTime);

			// Đảm bảo chân không bước tới
			_animator.SetFloat(_hashVertical, 0);
			_animator.SetFloat(_hashHorizontal, 0);
		}
		// Nếu góc lệch nhỏ (<= 5 độ) -> DI CHUYỂN + SỬA NHẸ HƯỚNG
		else
		{
			_agent.isStopped = false;
			_animator.SetBool(_hashIsMoving, true);

			// Tắt xoay
			_animator.SetFloat(_hashTurn, 0, 0.1f, Time.deltaTime);

			// Cập nhật bước đi
			_animator.SetFloat(_hashVertical, localDir.z, 0.2f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, localDir.x, 0.2f, Time.deltaTime);

			// --- CƠ CHẾ SỬA LỖI (CORRECTION) ---
			// Chỉ chạy khi sai số nhỏ để "nắn" nhện thẳng hàng tuyệt đối với mục tiêu
			// Giúp nhện không bị trượt dần ra khỏi đường dẫn NavMesh
			Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _correctionTurnSpeed * Time.deltaTime);
		}
	}

	// (Giữ nguyên phần OffMeshLink...)
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

		_animator.CrossFade("Jump", 0.2f);

		float totalTime = 0.7f;
		float currentTime = totalTime;

		while (currentTime > 0)
		{
			currentTime -= Time.deltaTime;
			float t = 1 - (currentTime / totalTime);
			Vector3 goalPos = Vector3.Lerp(data.startPos, data.endPos, t);
			float elapsed = totalTime - currentTime;

			if (elapsed < 0.3f)
				transform.position = Vector3.Lerp(transform.position, goalPos, elapsed / 0.3f);
			else
				transform.position = goalPos;

			yield return null;
		}

		transform.position = data.endPos;
		_agent.CompleteOffMeshLink();
		_isOffMesh = false;
	}
}