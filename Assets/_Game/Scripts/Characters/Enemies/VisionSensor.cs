using UnityEngine;
using Unity.Behavior;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class VisionSensor : MonoBehaviour
{
	[Header("Settings - Eyes")]
	[Tooltip("Assign eye objects here. If empty, the object's own transform will be used.")]
	public Transform[] eyes;

	[Tooltip("Field of View angle (Cone Angle).")]
	[Range(0, 360)] public float viewAngle = 110f;

	[Tooltip("Maximum view distance (View Radius).")]
	public float viewRadius = 15f;

	[Tooltip("Absolute close range detection (detects even if behind or blinded).")]
	public float closeRange = 3.0f;

	[Tooltip("Time (in seconds) to keep the 'Detected' state true after losing visual contact.")]
	public float detectionHoldTime = 3.0f;

	[Header("Settings - Layers")]
	[Tooltip("Player Layer (Must be set correctly on the Player GameObject).")]
	public LayerMask targetMask;

	[Tooltip("Obstacle Layers (Walls, Ground...). DO NOT INCLUDE THE ENEMY LAYER.")]
	public LayerMask obstacleMask;

	[Header("Blackboard Configuration")]
	public string playerVariableName = "Player";
	public string detectedVariableName = "IsDetected";

	[Header("Debug")]
	[SerializeField] private Transform _playerTarget;
	[SerializeField] private bool _canSeePlayer; // Final state sent to Blackboard
	[SerializeField] private bool _isPhysicallyVisible; // Real-time physical visibility status

	// Cached components
	private BehaviorGraphAgent _behaviorAgent;
	private Collider _playerCollider;

	// Combined mask for Raycasting (Includes both Obstacles and Player)
	// Ensures the ray stops at the FIRST object hit (whether it is a Wall or the Player)
	private int _combinedMask;

	// Timer for detection memory
	private float _lastTimeSeen = -100f;

	private void Start()
	{
		_behaviorAgent = GetComponent<BehaviorGraphAgent>();

		// Automatically find Player if not manually assigned
		if (_playerTarget == null)
		{
			GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
			if (playerObj != null)
			{
				_playerTarget = playerObj.transform;
				_playerCollider = playerObj.GetComponent<Collider>();
			}
		}

		// Set Player to Blackboard (Once at start)
		if (_playerTarget != null)
		{
			_behaviorAgent.SetVariableValue(playerVariableName, _playerTarget.gameObject);
		}
		else
		{
			Debug.LogWarning("VisionSensor: Player not found! Ensure Player has the 'Player' tag.");
		}

		// Default config for Obstacle Mask
		if (obstacleMask == 0)
		{
			obstacleMask = LayerMask.GetMask("Default");
		}

		_combinedMask = obstacleMask | targetMask;
	}

	private void Update()
	{
		// 1. Perform physical vision check
		_isPhysicallyVisible = CheckVision();

		// 2. Handle Detection Hold Time (Hysteresis)
		if (_isPhysicallyVisible)
		{
			_lastTimeSeen = Time.time;
			_canSeePlayer = true;
		}
		else
		{
			// If the time since last seen is within the hold duration, keep detected as true
			if (Time.time - _lastTimeSeen < detectionHoldTime)
			{
				_canSeePlayer = true;
			}
			else
			{
				_canSeePlayer = false;
			}
		}

		// 3. Write result to Blackboard
		_behaviorAgent.SetVariableValue(detectedVariableName, _canSeePlayer);
	}

	private bool CheckVision()
	{
		if (_playerTarget == null) return false;

		// Handle case where no eyes are assigned
		if (eyes == null || eyes.Length == 0)
		{
			return CheckEyesLogic(transform);
		}

		// Iterate through all eyes
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

		// 1. Check Radius
		if (distanceToPlayer > viewRadius) return false;

		// 2. Check View Angle
		float halfAngle = viewAngle * 0.5f;
		float angleToPlayer = Vector3.Angle(eye.forward, vectorToPlayer);

		// Success if very close (<1m) OR within view angle
		bool angleOK = (distanceToPlayer <= closeRange) || (angleToPlayer <= halfAngle);

		if (angleOK)
		{
			// 3. Raycast Check (Check 3 points: Center, Top, Bottom)
			if (_playerCollider != null)
			{
				// Prioritize Center check
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.center)) return true;

				// Check Top (Head) - helps when hiding behind low obstacles
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.max)) return true;

				// Check Bottom (Feet) - helps when only legs are visible
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.min)) return true;
			}
			else
			{
				// Fallback if Player has no Collider (check pivot + offset)
				// Raise check point slightly to avoid hitting ground
				if (CheckLineOfSight(eye.position, _playerTarget.position + Vector3.up * 1.0f)) return true;
			}
		}

		return false;
	}

	private bool CheckLineOfSight(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		float distance = direction.magnitude;

		// Raycast with CombinedMask
		// QueryTriggerInteraction.Ignore to look through trigger volumes
		if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance, _combinedMask, QueryTriggerInteraction.Ignore))
		{
			// LOGIC 1: Check if it hit the Player (or a child of the Player)
			if (hit.transform == _playerTarget || hit.transform.IsChildOf(_playerTarget))
			{
				return true; // First hit is Player -> VISIBLE
			}

			// LOGIC 2: If not Player, it must be an obstacle (since mask only has Player + Obstacle)
			return false; // First hit is Wall/Obstacle -> BLOCKED
		}

		// No hit occurred -> Clear line of sight -> VISIBLE
		return true;
	}

	// Draw Debug Gizmos
	private void OnDrawGizmos()
	{
		if (eyes == null) return;

		// Green if visible, Yellow if hidden but in memory (hold time), Red if lost
		if (_isPhysicallyVisible)
			Gizmos.color = Color.green;
		else if (_canSeePlayer)
			Gizmos.color = Color.yellow;
		else
			Gizmos.color = Color.red;

		foreach (var eye in eyes)
		{
			if (eye == null) continue;

			Gizmos.DrawWireSphere(eye.position, viewRadius);

			Vector3 viewAngleA = DirFromAngle(eye, -viewAngle / 2, false);
			Vector3 viewAngleB = DirFromAngle(eye, viewAngle / 2, false);

			Gizmos.DrawLine(eye.position, eye.position + viewAngleA * viewRadius);
			Gizmos.DrawLine(eye.position, eye.position + viewAngleB * viewRadius);

			// Draw debug lines to player bounds if applicable
			if (_playerTarget != null && _playerCollider != null)
			{
				Gizmos.color = new Color(1, 1, 0, 0.3f); // Faint yellow
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