using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ArathroxMovement : MonoBehaviour
{
	#region Configuration
	[Header("Movement Settings")]
	[SerializeField] private float _turnStartThreshold = 45f;
	[SerializeField] private float _turnEndThreshold = 10f;
	[SerializeField] private float _alignSpeed = 120f;
	[SerializeField] private float _stopDistance = 0.5f;

	[Header("Combat Settings")]
	[Tooltip("Thời gian đổi hướng strafe ngẫu nhiên (Min-Max)")]
	[SerializeField] private Vector2 _strafeChangeInterval = new Vector2(2f, 4f);
	[Tooltip("Layer của các quái vật khác (để né nhau)")]
	[SerializeField] private LayerMask _allyLayer;
	#endregion

	#region Internal State
	private NavMeshAgent _agent;
	private Animator _animator;
	private bool _isTurningInPlace;
	private bool _hasTarget;

	// Combat Variables
	private float _strafeTimer;
	private float _currentStrafeDir; // -1 (Trái), 0 (Đứng), 1 (Phải)
	private Collider[] _allyBuffer = new Collider[10]; // Buffer để tối ưu memory (NonAlloc)

	// Animator Hashes
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");
	private readonly int _hashIsMoving = Animator.StringToHash("IsMoving");
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

	private void Update()
	{
		// Nếu đang trong trạng thái Combat (được gọi từ Graph), bỏ qua logic di chuyển thông thường
		// Logic di chuyển thông thường chỉ chạy khi có path
		if (_hasTarget && !_agent.isStopped)
		{
			HandleNormalMovement();
		}
	}

	private void OnAnimatorMove()
	{
		// Đồng bộ vị trí NavMesh theo Root Motion
		Vector3 newPos = transform.position + _animator.deltaPosition;

		// Snap xuống NavMesh để tránh bay lơ lửng
		if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
		{
			newPos.y = Mathf.Lerp(transform.position.y, hit.position.y, 20f * Time.deltaTime);
		}

		transform.position = newPos;
		transform.rotation *= _animator.deltaRotation;

		_agent.nextPosition = transform.position;
	}
	#endregion

	#region Public Methods (API)

	// Gọi khi rượt đuổi bình thường
	public void MoveTo(Vector3 position)
	{
		if (_agent.destination != position)
			_agent.SetDestination(position);

		_hasTarget = true;
		_agent.isStopped = false;
	}

	// Gọi khi muốn dừng hẳn
	public void Stop()
	{
		if (_agent.isOnNavMesh) _agent.ResetPath();
		_hasTarget = false;
		_agent.isStopped = true;

		ResetAnimator();
	}

	/// <summary>
	/// Xử lý di chuyển chiến thuật (Giữ range, Strafe, Né đồng đội).
	/// Hàm này được gọi liên tục từ Behavior Graph Action Node.
	/// </summary>
	public void HandleCombatMovement(Transform target, float idealRange, float separationDist)
	{
		_hasTarget = false; // Tắt mode dẫn đường thông thường
		_agent.isStopped = true; // Ngắt NavMesh pathfinding

		// 1. Luôn xoay mặt về phía mục tiêu
		RotateTowards(target.position);

		// --- TÍNH TOÁN LỰC (STEERING FORCES) ---
		Vector3 finalDirection = Vector3.zero;

		// Force A: Maintain Range (Tiến/Lùi)
		float distanceToTarget = Vector3.Distance(transform.position, target.position);
		if (distanceToTarget > idealRange + 1f)
			finalDirection += transform.forward; // Tiến lên
		else if (distanceToTarget < idealRange - 1f)
			finalDirection -= transform.forward; // Lùi lại

		// Force B: Separation (Né đồng đội)
		int numColliders = Physics.OverlapSphereNonAlloc(transform.position, separationDist, _allyBuffer, _allyLayer);
		Vector3 separationVector = Vector3.zero;
		for (int i = 0; i < numColliders; i++)
		{
			if (_allyBuffer[i].gameObject == gameObject) continue; // Bỏ qua chính mình

			Vector3 awayFromAlly = transform.position - _allyBuffer[i].transform.position;
			// Càng gần đẩy càng mạnh (Inverse Square Law đơn giản hóa)
			separationVector += awayFromAlly.normalized / (awayFromAlly.magnitude + 0.1f);
		}
		finalDirection += separationVector * 1.5f; // Hệ số ưu tiên né (1.5)

		// Force C: Strafe (Di chuyển ngang ngẫu nhiên)
		UpdateStrafeLogic();
		finalDirection += transform.right * _currentStrafeDir;

		// --- GỬI VÀO ANIMATOR ---
		// Chuyển vector tổng hợp từ World Space -> Local Space để Animator hiểu (Horizontal/Vertical)
		Vector3 localInput = transform.InverseTransformDirection(finalDirection.normalized);

		// Kích hoạt Blend Tree
		_animator.SetBool(_hashIsMoving, true);
		_animator.SetFloat(_hashHorizontal, localInput.x, 0.2f, Time.deltaTime); // Strafe
		_animator.SetFloat(_hashVertical, localInput.z, 0.2f, Time.deltaTime);   // Tiến/Lùi

		// Debug visual
		Debug.DrawRay(transform.position, separationVector * 2, Color.red); // Lực né
		Debug.DrawRay(transform.position, transform.right * _currentStrafeDir, Color.blue); // Lực Strafe
	}
	#endregion

	#region Internal Logic

	private void HandleNormalMovement()
	{
		// (Giữ nguyên logic cũ của bạn, đã tối ưu lại gọn hơn)
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		Vector3 targetDir = (_agent.steeringTarget - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) targetDir = transform.forward;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// Turn In Place Logic
		if (!_isTurningInPlace && absAngle > _turnStartThreshold) _isTurningInPlace = true;
		else if (_isTurningInPlace && absAngle < _turnEndThreshold) _isTurningInPlace = false;

		if (_isTurningInPlace)
		{
			_animator.SetBool(_hashIsMoving, false);
			_animator.SetFloat(_hashTurn, Mathf.Sign(signedAngle), 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, 0); _animator.SetFloat(_hashVertical, 0);
		}
		else
		{
			_animator.SetBool(_hashIsMoving, true);
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);
			RotateTowards(_agent.steeringTarget);

			// Locomotion
			Vector3 localVel = transform.InverseTransformDirection(_agent.desiredVelocity);
			float speedFactor = Mathf.Max(_agent.speed, 1f);
			_animator.SetFloat(_hashHorizontal, localVel.x / speedFactor, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, localVel.z / speedFactor, 0.1f, Time.deltaTime);
		}
	}

	private void RotateTowards(Vector3 targetPos)
	{
		Vector3 dir = (targetPos - transform.position).normalized;
		dir.y = 0;
		if (dir != Vector3.zero)
		{
			Quaternion targetRot = Quaternion.LookRotation(dir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _alignSpeed * Time.deltaTime);
		}
	}

	private void UpdateStrafeLogic()
	{
		_strafeTimer -= Time.deltaTime;
		if (_strafeTimer <= 0)
		{
			_strafeTimer = Random.Range(_strafeChangeInterval.x, _strafeChangeInterval.y);

			// Random chiến thuật: 
			// 30% đi trái, 30% đi phải, 40% đứng yên bắn (chỉ né đồng đội)
			float rand = Random.value;
			if (rand < 0.3f) _currentStrafeDir = -1f;
			else if (rand < 0.6f) _currentStrafeDir = 1f;
			else _currentStrafeDir = 0f;
		}
	}

	private void ResetAnimator()
	{
		_animator.SetBool(_hashIsMoving, false);
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
	}

	private void OnDrawGizmosSelected()
	{
		if (_agent != null && _hasTarget)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, _agent.steeringTarget);
		}
	}
	#endregion
}