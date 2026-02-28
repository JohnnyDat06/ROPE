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

	[Tooltip("Time (in seconds) to keep chasing the player AFTER losing visual contact.")]
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
	private int _combinedMask;

	// Core Memory Timer: The exact time when the monster will "forget" the player
	private float _memoryEndTime = -100f;

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

		// Initialize Blackboard Target
		if (_playerTarget != null)
		{
			_behaviorAgent.SetVariableValue(playerVariableName, _playerTarget.gameObject);
		}
		else
		{
			Debug.LogWarning("VisionSensor: Player not found! Ensure Player has the 'Player' tag.");
		}

		// Default config for Obstacle Mask
		if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Default");

		_combinedMask = obstacleMask | targetMask;
	}

	private void Update()
	{
		// 1. Perform physical vision check (Raycast & Angle)
		_isPhysicallyVisible = CheckVision();

		// 2. Handle Detection Memory (Hysteresis)
		if (_isPhysicallyVisible)
		{
			// Continuously push the memory expiration time further into the future
			// as long as the monster can actively see the player.
			_memoryEndTime = Time.time + detectionHoldTime;
			_canSeePlayer = true;
		}
		else
		{
			// If physically blind, check if the memory timer is still running
			_canSeePlayer = Time.time < _memoryEndTime;
		}

		// 3. Write the final computed state to the Blackboard
		if (_behaviorAgent != null)
		{
			_behaviorAgent.SetVariableValue(detectedVariableName, _canSeePlayer);
		}
	}

	/// <summary>
	/// Forces the enemy to enter the "Detected" state for a specific duration.
	/// Call this when the enemy takes damage from behind.
	/// </summary>
	/// <param name="alertDuration">How long the alert lasts. If left at 0, it uses default detectionHoldTime.</param>
	public void TriggerAlert(float alertDuration = 0f)
	{
		float duration = alertDuration > 0f ? alertDuration : detectionHoldTime;

		// Extend the memory timer. We use Mathf.Max to ensure a new short alert 
		// doesn't override an existing long memory if the enemy is already chasing.
		_memoryEndTime = Mathf.Max(_memoryEndTime, Time.time + duration);

		_canSeePlayer = true;

		// Force write to Blackboard immediately so the Behavior Graph reacts this exact frame
		if (_behaviorAgent != null)
		{
			_behaviorAgent.SetVariableValue(detectedVariableName, true);
		}

		Debug.Log($"[{gameObject.name}] VisionSensor: Alert triggered! Enemy will search for {duration} seconds.");
	}

	// =========================================================
	// PHYSICAL VISION LOGIC
	// =========================================================

	private bool CheckVision()
	{
		if (_playerTarget == null) return false;

		if (eyes == null || eyes.Length == 0)
		{
			return CheckEyesLogic(transform);
		}

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

		// 2. Check View Angle & Close Range
		float halfAngle = viewAngle * 0.5f;
		float angleToPlayer = Vector3.Angle(eye.forward, vectorToPlayer);

		bool angleOK = (distanceToPlayer <= closeRange) || (angleToPlayer <= halfAngle);

		if (angleOK)
		{
			// 3. Raycast Checks (Center, Top, Bottom)
			if (_playerCollider != null)
			{
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.center)) return true;
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.max)) return true;
				if (CheckLineOfSight(eye.position, _playerCollider.bounds.min)) return true;
			}
			else
			{
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
			// If the ray hits the player (or their children), vision is clear
			if (hit.transform == _playerTarget || hit.transform.IsChildOf(_playerTarget)) return true;

			// Otherwise, it hit a wall
			return false;
		}

		return true; // No obstacles hit
	}

	// =========================================================
	// DEBUG GIZMOS
	// =========================================================

	private void OnDrawGizmos()
	{
		if (eyes == null) return;

		// Green: Seeing right now | Yellow: Remembering (Hold Time) | Red: Lost target
		if (_isPhysicallyVisible) Gizmos.color = Color.green;
		else if (_canSeePlayer) Gizmos.color = Color.yellow;
		else Gizmos.color = Color.red;

		foreach (var eye in eyes)
		{
			if (eye == null) continue;

			Gizmos.DrawWireSphere(eye.position, viewRadius);

			Vector3 viewAngleA = DirFromAngle(eye, -viewAngle / 2, false);
			Vector3 viewAngleB = DirFromAngle(eye, viewAngle / 2, false);

			Gizmos.DrawLine(eye.position, eye.position + viewAngleA * viewRadius);
			Gizmos.DrawLine(eye.position, eye.position + viewAngleB * viewRadius);

			if (_playerTarget != null && _playerCollider != null)
			{
				Gizmos.color = new Color(1, 1, 0, 0.3f);
				Gizmos.DrawLine(eye.position, _playerCollider.bounds.center);
			}
		}
	}

	private Vector3 DirFromAngle(Transform eye, float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal) angleInDegrees += eye.eulerAngles.y;
		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}
}