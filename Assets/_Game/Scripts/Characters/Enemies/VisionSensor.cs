using UnityEngine;
using Unity.Behavior;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class VisionSensor : MonoBehaviour
{
	[Header("Settings - Eyes")]
	[Tooltip("Gán các object mắt vào đây. Nếu để trống sẽ dùng vị trí của chính object này.")]
	public Transform[] eyes;

	[Tooltip("Góc nhìn (Cone Angle)")]
	[Range(0, 360)] public float viewAngle = 110f;

	[Tooltip("Tầm xa (View Radius)")]
	public float viewRadius = 15f;

	[Tooltip("Tầm nhìn gần tuyệt đối (Mù cũng thấy nếu đứng sát sạt)")]
	public float closeRange = 1.0f;

	[Header("Settings - Layers")]
	[Tooltip("Layer của Player (Bắt buộc phải set đúng Layer cho Player GameObject)")]
	public LayerMask targetMask;

	[Tooltip("Layer vật cản (Tường, Đất...). TUYỆT ĐỐI KHÔNG CHỌN LAYER CỦA QUÁI.")]
	public LayerMask obstacleMask;

	[Header("Blackboard Configuration")]
	public string playerVariableName = "Player";
	public string detectedVariableName = "IsDetected";

	[Header("Debug")]
	[SerializeField] private Transform _playerTarget;
	[SerializeField] private bool _canSeePlayer;

	// Cache component
	private BehaviorGraphAgent _behaviorAgent;
	private Collider _playerCollider; // Cache collider để lấy bounds

	// Mask tổng hợp dùng để bắn Raycast (Bao gồm cả Tường và Player)
	// Để đảm bảo tia bắn ra sẽ dừng lại ở vật cản ĐẦU TIÊN mà nó gặp (dù là Tường hay Player)
	private int _combinedMask;

	private void Start()
	{
		_behaviorAgent = GetComponent<BehaviorGraphAgent>();

		// Tự động tìm Player
		if (_playerTarget == null)
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj != null)
			{
				_playerTarget = playerObj.transform;
				_playerCollider = playerObj.GetComponent<Collider>();
			}
		}

		// Gán Player vào Blackboard (1 lần)
		if (_playerTarget != null)
		{
			_behaviorAgent.SetVariableValue(playerVariableName, _playerTarget.gameObject);
		}
		else
		{
			Debug.LogWarning("VisionSensor: Chưa tìm thấy Player! Hãy chắc chắn Player có Tag 'Player'.");
		}

		// Config mặc định cho Obstacle Mask
		if (obstacleMask == 0)
		{
			obstacleMask = LayerMask.GetMask("Default");
		}

		_combinedMask = obstacleMask | targetMask;
	}

	private void Update()
	{
		// Tính toán tầm nhìn
		_canSeePlayer = CheckVision();

		// Ghi vào Blackboard
		_behaviorAgent.SetVariableValue(detectedVariableName, _canSeePlayer);
	}

	private bool CheckVision()
	{
		if (_playerTarget == null) return false;

		// Xử lý trường hợp không gán mắt
		if (eyes == null || eyes.Length == 0)
		{
			return CheckEyesLogic(transform);
		}

		// Duyệt qua từng mắt
		foreach (var eye in eyes)
		{
			if (eye != null && CheckEyesLogic(eye)) return true;
		}

		return false;
	}

	private bool CheckEyesLogic(Transform eye)
	{
		Vector3 vectorToPlayer = _playerTarget.position - eye.position;
		float distanceToPlayer = vectorToPlayer.magnitude;

		// 1. Kiểm tra Khoảng cách (Radius)
		if (distanceToPlayer > viewRadius) return false;

		// 2. Logic góc nhìn (Kết hợp logic cũ của bạn)
		float halfAngle = viewAngle * 0.5f;
		float angleToPlayer = Vector3.Angle(eye.forward, vectorToPlayer);

		// Nếu ở quá gần (<1m) HOẶC góc nhìn thỏa mãn -> OK
		bool angleOK = (distanceToPlayer <= closeRange) || (angleToPlayer <= halfAngle);

		if (angleOK)
		{
			// 3. Raycast kiểm tra vật cản (Check 3 điểm: Tâm, Đỉnh, Đáy)
			if (_playerCollider != null)
			{
				// Ưu tiên check Tâm trước (Center)
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.center)) return true;

				// Check Đỉnh đầu (Top) - giúp thấy khi núp sau vật thấp
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.max)) return true;

				// Check Chân (Bottom) - giúp thấy khi lòi chân ra
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.min)) return true;
			}
			else
			{
				// Fallback nếu Player không có Collider (chỉ check pivot)
				// Nâng điểm check lên 1 chút (thường pivot ở chân) để tránh chạm đất
				if (CheckLineOfSight(eye.position, _playerTarget.position + Vector3.up * 1.0f)) return true;
			}
		}

		return false;
	}

	private bool CheckLineOfSight(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		float distance = direction.magnitude;

		if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance, _combinedMask, QueryTriggerInteraction.Ignore))
		{
			// LOGIC 1: Kiểm tra xem có trúng Player (hoặc bộ phận của Player) không?
			// Dùng IsChildOf để đảm bảo nếu bắn trúng tay/chân/đầu (collider con) thì vẫn tính là trúng Player
			if (hit.transform == _playerTarget || hit.transform.IsChildOf(_playerTarget))
			{
				return true; // Vật cản đầu tiên là Player -> NHÌN THẤY
			}

			// LOGIC 2: Nếu không phải Player, thì chắc chắn là vật cản môi trường (do Mask chỉ có Player + Obstacle)
			// (Ví dụ: Trúng Tường, Đất...)
			return false; // Vật cản đầu tiên là Tường -> KHÔNG THẤY
		}

		// Không trúng vật cản gì cả -> Đường thoáng -> Nhìn thấy
		return true;
	}

	// Vẽ Debug
	private void OnDrawGizmos()
	{
		if (eyes == null) return;

		Gizmos.color = _canSeePlayer ? Color.green : Color.red;

		foreach (var eye in eyes)
		{
			if (eye == null) continue;

			Gizmos.DrawWireSphere(eye.position, viewRadius);

			Vector3 viewAngleA = DirFromAngle(eye, -viewAngle / 2, false);
			Vector3 viewAngleB = DirFromAngle(eye, viewAngle / 2, false);

			Gizmos.DrawLine(eye.position, eye.position + viewAngleA * viewRadius);
			Gizmos.DrawLine(eye.position, eye.position + viewAngleB * viewRadius);

			// Vẽ tia debug đến 3 điểm của Player nếu có thể
			if (_playerTarget != null && _playerCollider != null)
			{
				Gizmos.color = new Color(1, 1, 0, 0.3f); // Vàng mờ
				Gizmos.DrawLine(eye.position, _playerCollider.bounds.center);
				Gizmos.DrawLine(eye.position, _playerCollider.bounds.max);
				Gizmos.DrawLine(eye.position, _playerCollider.bounds.min);
			}
		}
	}

	private Vector3 DirFromAngle(Transform eye, float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal) angleInDegrees += eye.eulerAngles.y;
		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}
}