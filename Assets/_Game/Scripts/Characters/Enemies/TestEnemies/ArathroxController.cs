//using UnityEngine;

//// Định nghĩa các trạng thái
//public enum ArathroxState
//{
//	Patrol, // Đi tuần
//	Chase,  // Truy đuổi
//	Attack  // Tấn công
//}

//[RequireComponent(typeof(ArathroxMovement))]
//public class ArathroxController : MonoBehaviour
//{
//	[Header("References")]
//	[SerializeField] private WaypointManager _waypointManager; // Kéo WaypointManager vào đây

//	[Header("Settings")]
//	[SerializeField] private float _patrolWaitTime = 2f; // Thời gian nghỉ tại mỗi điểm tuần tra
//	[SerializeField] private float _attackRange = 2.0f;  // Khoảng cách bắt đầu tấn công

//	// --- STATE MANAGEMENT ---
//	public ArathroxState CurrentState = ArathroxState.Patrol;
//	private Transform _currentTarget; // Mục tiêu (Player) được truyền vào từ bên ngoài

//	// Component References
//	private ArathroxMovement _movement;
//	private Animator _animator;

//	// Internal Variables
//	private float _waitTimer;
//	private bool _isWaiting;
//	private Vector3 _currentPatrolPoint;
//	private bool _hasPatrolPoint;

//	// Hashes Animation (Giả sử bạn có parameter Attack)
//	private readonly int _hashAttack = Animator.StringToHash("Attack");

//	private void Awake()
//	{
//		_movement = GetComponent<ArathroxMovement>();
//		_animator = GetComponent<Animator>();
//	}

//	private void Start()
//	{
//		// Khởi tạo điểm đi tuần đầu tiên
//		PickNewPatrolPoint();
//	}

//	private void Update()
//	{
//		switch (CurrentState)
//		{
//			case ArathroxState.Patrol:
//				HandlePatrolState();
//				break;

//			case ArathroxState.Chase:
//				HandleChaseState();
//				break;

//			case ArathroxState.Attack:
//				HandleAttackState();
//				break;
//		}
//	}

//	// --- BEHAVIOR NODE INTERFACE ---
//	// Đây là hàm "Node" mà hệ thống Behavior của bạn sẽ gọi để truyền mục tiêu
//	public void SetTarget(Transform target)
//	{
//		_currentTarget = target;

//		// Tự động chuyển State dựa trên việc có mục tiêu hay không
//		// Logic này có thể tùy chỉnh tùy theo Behavior Tree của bạn
//		if (_currentTarget != null)
//		{
//			float dist = Vector3.Distance(transform.position, _currentTarget.position);
//			if (dist <= _attackRange)
//			{
//				ChangeState(ArathroxState.Attack);
//			}
//			else
//			{
//				ChangeState(ArathroxState.Chase);
//			}
//		}
//		else
//		{
//			ChangeState(ArathroxState.Patrol);
//		}
//	}

//	// --- STATE LOGIC ---

//	private void HandlePatrolState()
//	{
//		// Nếu không có WaypointManager hoặc list rỗng -> Đứng yên
//		if (_waypointManager == null || _waypointManager.waypoints.Count == 0)
//		{
//			_movement.Stop();
//			return;
//		}

//		// Nếu đang trong thời gian chờ
//		if (_isWaiting)
//		{
//			_waitTimer -= Time.deltaTime;
//			if (_waitTimer <= 0)
//			{
//				_isWaiting = false;
//				PickNewPatrolPoint();
//			}
//			return;
//		}

//		// Di chuyển đến điểm tuần tra
//		if (_hasPatrolPoint)
//		{
//			_movement.MoveTo(_currentPatrolPoint);

//			// Kiểm tra xem đến chưa
//			if (_movement.HasReachedDestination())
//			{
//				_movement.Stop();
//				_isWaiting = true;
//				_waitTimer = _patrolWaitTime;
//			}
//		}
//	}

//	private void HandleChaseState()
//	{
//		if (_currentTarget == null)
//		{
//			ChangeState(ArathroxState.Patrol);
//			return;
//		}

//		float dist = Vector3.Distance(transform.position, _currentTarget.position);

//		// Nếu trong tầm tấn công -> Chuyển sang Attack
//		if (dist <= _attackRange)
//		{
//			ChangeState(ArathroxState.Attack);
//			return;
//		}

//		// Tiếp tục truy đuổi
//		_movement.MoveTo(_currentTarget.position);
//	}

//	private void HandleAttackState()
//	{
//		if (_currentTarget == null)
//		{
//			ChangeState(ArathroxState.Patrol);
//			return;
//		}

//		float dist = Vector3.Distance(transform.position, _currentTarget.position);

//		// Nếu mục tiêu chạy thoát khỏi tầm tấn công -> Quay lại Chase
//		// (Thêm một chút buffer ví dụ +0.5 để tránh chuyển state liên tục tại ranh giới)
//		if (dist > _attackRange + 0.5f)
//		{
//			_animator.ResetTrigger(_hashAttack); // Reset trigger nếu đang dở
//			ChangeState(ArathroxState.Chase);
//			return;
//		}

//		// Logic Tấn công
//		_movement.Stop(); // Dừng di chuyển
//		_movement.FaceTarget(_currentTarget.position); // Luôn xoay mặt về mục tiêu

//		// Trigger Animation Attack (Bạn có thể thêm timer để đánh theo nhịp)
//		// Ví dụ đơn giản:
//		// _animator.SetTrigger(_hashAttack); 
//	}

//	// --- HELPER METHODS ---

//	private void PickNewPatrolPoint()
//	{
//		if (_waypointManager != null)
//		{
//			_currentPatrolPoint = _waypointManager.GetRandomWaypoint();

//			// Kiểm tra xem điểm lấy được có hợp lệ không (ví dụ khác vector zero)
//			if (_currentPatrolPoint != Vector3.zero)
//			{
//				_hasPatrolPoint = true;
//			}
//		}
//	}

//	public void ChangeState(ArathroxState newState)
//	{
//		if (CurrentState == newState) return;

//		CurrentState = newState;

//		// Reset một số thứ khi chuyển State nếu cần
//		if (newState == ArathroxState.Patrol)
//		{
//			_isWaiting = false; // Bắt đầu đi ngay
//			PickNewPatrolPoint();
//		}
//	}
//}