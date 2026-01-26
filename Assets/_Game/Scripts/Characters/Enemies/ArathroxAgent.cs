using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Điều khiển chuyển động của Agent (nhện) kết hợp NavMeshAgent và Root Motion Animation.
/// Sử dụng cơ chế Hysteresis (Ngưỡng đôi) để xử lý mượt mà chuyển đổi giữa xoay tại chỗ và di chuyển.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class SpiderAgent : MonoBehaviour
{
    #region Configuration
    
    [Header("Rotation Settings")]
    [Tooltip("Ngưỡng BẮT ĐẦU xoay (Hysteresis High): Nếu góc lệch lớn hơn số này, Agent sẽ dừng lại để xoay tại chỗ.")]
    [Range(10f, 180f)]
    [SerializeField] private float _startTurnThreshold = 60f;

    [Tooltip("Ngưỡng KẾT THÚC xoay (Hysteresis Low): Khi góc lệch nhỏ hơn số này, Agent sẽ ngừng xoay và bắt đầu di chuyển.")]
    [Range(1f, 20f)]
    [SerializeField] private float _stopTurnThreshold = 5f;

    [Tooltip("Tốc độ hỗ trợ xoay của Transform khi đang di chuyển (Steering assist).")]
    [SerializeField] private float _runRotationSpeed = 2f;

    [Header("Movement Settings")]
    [Tooltip("Khoảng cách tới đích để coi như đã dừng hẳn.")]
    [SerializeField] private float _stopDistance = 0.2f;

    #endregion

    #region Internal State

    private NavMeshAgent _agent;
    private Animator _animator;
    private bool _isOffMesh;
    private bool _isTurningInPlace = false;

    // Animator Hashes (Cached for performance)
    private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
    private readonly int _hashVertical = Animator.StringToHash("Vertical");
    private readonly int _hashTurn = Animator.StringToHash("Turn");
    private readonly int _hashIsMoving = Animator.StringToHash("IsMoving");

    /// <summary>
    /// Kiểm tra xem Agent có đang thực sự di chuyển trên đường đi hay không.
    /// </summary>
    public bool IsMoving => _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // Cấu hình NavMeshAgent để hoạt động với Root Motion
        _agent.updateRotation = false; // Tắt xoay tự động của NavMesh
        _agent.updatePosition = true;  // Giữ đồng bộ vị trí vật lý
        _agent.angularSpeed = 0;       // Loại bỏ hoàn toàn lực xoay của Agent
    }

    private void Update()
    {
        HandleOffMeshLink();
        HandleMovementAndRotation();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Ra lệnh cho Agent di chuyển đến vị trí chỉ định.
    /// </summary>
    /// <param name="targetPosition">Tọa độ thế giới (World Position) cần đến.</param>
    public void MoveTo(Vector3 targetPosition)
    {
        // Đảm bảo Agent đang hoạt động trên NavMesh trước khi set đích
        if (_agent.isOnNavMesh)
        {
            _agent.SetDestination(targetPosition);
            _agent.isStopped = false;
        }
    }

    /// <summary>
    /// Dừng Agent ngay lập tức và reset các trạng thái Animation.
    /// </summary>
    public void Stop()
    {
        if (_agent.hasPath) _agent.ResetPath();
        
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        
        ResetAnimatorParameters();
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Reset toàn bộ tham số Animator về trạng thái Idle.
    /// </summary>
    private void ResetAnimatorParameters()
    {
        _animator.SetFloat(_hashHorizontal, 0);
        _animator.SetFloat(_hashVertical, 0);
        _animator.SetFloat(_hashTurn, 0);
        _animator.SetBool(_hashIsMoving, false);
        _isTurningInPlace = false;
    }

    private void HandleMovementAndRotation()
    {
        // 1. Kiểm tra điều kiện dừng
        if (!_agent.hasPath || _isOffMesh || _agent.remainingDistance <= _stopDistance)
        {
            Stop();
            return;
        }

        // 2. Tính toán Vector hướng và Góc lệch
        Vector3 vectorToTarget = _agent.steeringTarget - transform.position;

        // Bỏ qua nếu vector quá nhỏ (lỗi Floating point)
        if (vectorToTarget.sqrMagnitude < 0.01f) return;

        Vector3 directionToTarget = vectorToTarget.normalized;
        float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // 3. Logic Hysteresis (Ngưỡng đôi) để ổn định trạng thái xoay
        // Tránh hiện tượng nhân vật bị giật (jitter) ở ranh giới góc
        if (!_isTurningInPlace && Mathf.Abs(angle) > _startTurnThreshold)
        {
            _isTurningInPlace = true; // Bắt đầu xoay
        }
        else if (_isTurningInPlace && Mathf.Abs(angle) < _stopTurnThreshold)
        {
            _isTurningInPlace = false; // Kết thúc xoay, chuyển sang đi
        }

        // 4. Thực thi hành động dựa trên trạng thái
        if (_isTurningInPlace)
        {
            ProcessTurnInPlace(angle);
        }
        else
        {
            ProcessMovement(directionToTarget);
        }
    }

    private void ProcessTurnInPlace(float angle)
    {
        // Dừng di chuyển tịnh tiến
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        _animator.SetBool(_hashIsMoving, false);

        // Xoay dứt khoát theo hướng (-1: Trái, 1: Phải)
        float turnDir = Mathf.Sign(angle);
        _animator.SetFloat(_hashTurn, turnDir, 0.1f, Time.deltaTime);

        // Reset các thông số di chuyển để tránh trượt chân
        _animator.SetFloat(_hashVertical, 0);
        _animator.SetFloat(_hashHorizontal, 0);
    }

    private void ProcessMovement(Vector3 directionToTarget)
    {
        // Cho phép di chuyển tịnh tiến
        _agent.isStopped = false;
        _animator.SetBool(_hashIsMoving, true);
        
        // Tắt xoay tại chỗ
        _animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);

        // Tính toán Strafe (Đi ngang)
        Vector3 localDir = transform.InverseTransformDirection(directionToTarget);
        _animator.SetFloat(_hashVertical, localDir.z, 0.2f, Time.deltaTime);
        _animator.SetFloat(_hashHorizontal, localDir.x, 0.2f, Time.deltaTime);

        // Steering Assist: Hỗ trợ xoay nhẹ Transform để bám sát hướng NavMesh
        // Giúp nhân vật không bị trôi lệch hướng khi Animation Strafe không hoàn hảo
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _runRotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Off-Mesh Link Handling (Jumping)

    private void HandleOffMeshLink()
    {
        if (_agent.isOnOffMeshLink && !_isOffMesh)
        {
            _isOffMesh = true;
            StartCoroutine(DoOffMeshLinkCoroutine(_agent.currentOffMeshLinkData));
        }
    }

    private IEnumerator DoOffMeshLinkCoroutine(OffMeshLinkData data)
    {
        // Giai đoạn 1: Xoay hướng về điểm đáp
        Vector3 jumpDir = (data.endPos - data.startPos).normalized;
        while (Vector3.Dot(transform.forward, jumpDir) < 0.99f)
        {
            Quaternion goalRot = Quaternion.LookRotation(jumpDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRot, 120f * Time.deltaTime);
            yield return null;
        }

        // Giai đoạn 2: Thực hiện nhảy
        _animator.CrossFade("Jump", 0.2f);

        float totalTime = 0.7f; // Thời gian nhảy giả định (nên khớp với Animation)
        float currentTime = totalTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            float t = 1 - (currentTime / totalTime);
            Vector3 goalPos = Vector3.Lerp(data.startPos, data.endPos, t);

            // Lerp vị trí để tạo chuyển động mượt mà thay vì snap
            float elapsed = totalTime - currentTime;
            if (elapsed < 0.3f)
                transform.position = Vector3.Lerp(transform.position, goalPos, elapsed / 0.3f);
            else
                transform.position = goalPos;

            yield return null;
        }

        // Kết thúc nhảy
        transform.position = data.endPos;
        _agent.CompleteOffMeshLink();
        _isOffMesh = false;
    }

    #endregion
}