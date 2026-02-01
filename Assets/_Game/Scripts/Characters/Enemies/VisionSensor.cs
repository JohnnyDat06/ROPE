using UnityEngine;
using Unity.Behavior; // Thư viện Unity Behavior mới

[RequireComponent(typeof(BehaviorGraphAgent))]
public class VisionSensor : MonoBehaviour
{
	[Header("Settings - Eyes")]
	[Tooltip("Gán các object mắt vào đây (Tối đa 2, hoặc 1 cũng được). Nếu để trống sẽ dùng vị trí của chính object này.")]
	public Transform[] eyes;

	[Tooltip("Góc nhìn (Cone Angle)")]
	[Range(0, 360)] public float viewAngle = 110f;

	[Tooltip("Tầm xa (View Radius)")]
	public float viewRadius = 15f;

	[Header("Settings - Layers")]
	[Tooltip("Layer của Player")]
	public LayerMask targetMask;

	[Tooltip("Layer vật cản (Tường, Đất...). Mặc định đã bao gồm Default.")]
	public LayerMask obstacleMask;

	[Header("Blackboard Configuration")]
	[Tooltip("Tên biến trong Blackboard dùng để lưu Player (Dạng GameObject/Transform)")]
	public string playerVariableName = "PlayerTarget";

	[Tooltip("Tên biến trong Blackboard dùng để lưu trạng thái phát hiện (Dạng Bool)")]
	public string detectedVariableName = "IsDetected";

	[Header("Debug")]
	[SerializeField] private Transform _playerTarget; // Để xem ở Inspector cho dễ
	[SerializeField] private bool _canSeePlayer; // Để xem debug

	// Cache component
	private BehaviorGraphAgent _behaviorAgent;

	private void Start()
	{
		_behaviorAgent = GetComponent<BehaviorGraphAgent>();

		// 1. Tự động tìm Player nếu chưa được gán thủ công
		if (_playerTarget == null)
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj != null)
			{
				_playerTarget = playerObj.transform;
			}
			else
			{
				Debug.LogWarning("VisionSensor: Không tìm thấy object nào có Tag 'Player'!");
			}
		}

		// 2. Gán Player vào Blackboard (Chỉ làm 1 lần lúc đầu)
		if (_playerTarget != null)
		{
			// SetVariableValue là hàm chuẩn để script ngoài giao tiếp với Behavior Graph
			_behaviorAgent.SetVariableValue(playerVariableName, _playerTarget.gameObject);
		}

		// Config mặc định cho Obstacle Mask nếu người dùng quên set
		if (obstacleMask == 0)
		{
			obstacleMask = LayerMask.GetMask("Default");
		}
	}

	private void Update()
	{
		// 3. Tính toán tầm nhìn mỗi frame
		_canSeePlayer = CheckVision();

		// 4. Ghi kết quả vào Blackboard
		_behaviorAgent.SetVariableValue(detectedVariableName, _canSeePlayer);
	}

	/// <summary>
	/// Logic tính toán hình nón (Cone) và Raycast
	/// </summary>
	private bool CheckVision()
	{
		if (_playerTarget == null) return false;

		// Nếu không gán mắt nào, dùng chính transform của quái
		if (eyes == null || eyes.Length == 0)
		{
			if (IsTargetVisibleFromEye(transform)) return true;
		}
		else
		{
			// Duyệt qua từng mắt (Hỗ trợ 1 hoặc 2 mắt)
			foreach (var eye in eyes)
			{
				if (eye != null && IsTargetVisibleFromEye(eye))
				{
					return true; // Chỉ cần 1 mắt thấy là coi như thấy
				}
			}
		}

		return false;
	}

	private bool IsTargetVisibleFromEye(Transform eye)
	{
		Vector3 dirToTarget = (_playerTarget.position - eye.position);
		float distanceToTarget = dirToTarget.magnitude;

		// A. Kiểm tra Khoảng cách (Radius)
		if (distanceToTarget > viewRadius) return false;

		// B. Kiểm tra Góc nhìn (Cone Angle)
		// Vector3.Angle trả về góc giữa hướng mắt nhìn (Forward) và hướng tới mục tiêu
		if (Vector3.Angle(eye.forward, dirToTarget) < viewAngle / 2)
		{
			// C. Kiểm tra Vật cản (Raycast)
			// Bắn tia từ mắt đến Player. Nếu chạm vào Obstacle thì trả về false.
			if (!Physics.Raycast(eye.position, dirToTarget.normalized, distanceToTarget, obstacleMask))
			{
				// Không chạm vật cản -> Nhìn thấy
				return true;
			}
		}

		return false;
	}

	// Vẽ Debug trực quan trong Scene View
	private void OnDrawGizmos()
	{
		if (eyes == null || eyes.Length == 0) return;

		foreach (var eye in eyes)
		{
			if (eye == null) continue;

			// Màu xanh nếu thấy, đỏ nếu không
			Gizmos.color = _canSeePlayer ? Color.green : Color.red;

			// Vẽ tầm xa
			Gizmos.DrawWireSphere(eye.position, viewRadius);

			// Vẽ 2 đường giới hạn góc nhìn (Hình nón 2D minh họa)
			Vector3 viewAngleA = DirFromAngle(eye, -viewAngle / 2, false);
			Vector3 viewAngleB = DirFromAngle(eye, viewAngle / 2, false);

			Gizmos.DrawLine(eye.position, eye.position + viewAngleA * viewRadius);
			Gizmos.DrawLine(eye.position, eye.position + viewAngleB * viewRadius);

			if (_canSeePlayer && _playerTarget != null)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(eye.position, _playerTarget.position);
			}
		}
	}

	// Hàm phụ trợ để vẽ Gizmos góc
	private Vector3 DirFromAngle(Transform eye, float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal)
		{
			angleInDegrees += eye.eulerAngles.y;
		}
		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}
}