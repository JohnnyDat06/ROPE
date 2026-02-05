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
	[Tooltip("Min/Max time to change strafe direction")]
	[SerializeField] private Vector2 _strafeChangeInterval = new Vector2(1.5f, 3f);

	[Tooltip("Layer of allies to avoid")]
	[SerializeField] private LayerMask _allyLayer;

	[Tooltip("How strongly to push away from allies")]
	[SerializeField] private float _separationWeight = 2.5f;

	[Tooltip("Distance to check for obstacles before strafing")]
	[SerializeField] private float _strafeCheckDistance = 1.5f;
	#endregion

	#region Internal State
	private NavMeshAgent _agent;
	private Animator _animator;
	private bool _isTurningInPlace;
	private bool _hasTarget;

	// Combat Variables
	private float _strafeTimer;
	private float _currentStrafeDir; // -1 (Left), 0 (Idle), 1 (Right)
	private Collider[] _allyBuffer = new Collider[10]; // NonAlloc buffer

	// Debugging
	private bool _debugCombat = false; // Toggle this via Inspector/Code to see lines

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

		// Disable NavMesh updates to rely on Root Motion
		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

	private void Update()
	{
		// Normal movement logic (Chase phase)
		if (_hasTarget && !_agent.isStopped)
		{
			HandleNormalMovement();
		}
	}

	private void OnAnimatorMove()
	{
		// Sync NavMesh with Root Motion
		Vector3 newPos = transform.position + _animator.deltaPosition;

		// Snap to NavMesh
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

	public void MoveTo(Vector3 position)
	{
		if (_agent.destination != position)
			_agent.SetDestination(position);

		_hasTarget = true;
		_agent.isStopped = false;
	}

	public void Stop()
	{
		if (_agent.isOnNavMesh) _agent.ResetPath();
		_hasTarget = false;
		_agent.isStopped = true;

		ResetAnimator();
	}

	/// <summary>
	/// Handles tactical combat movement: Maintain range, Strafe, and Avoid allies.
	/// Includes validation to prevent strafing into allies.
	/// </summary>
	public void HandleCombatMovement(Transform target, float idealRange, float separationDist)
	{
		_hasTarget = false; // Disable normal pathfinding
		_agent.isStopped = true;

		// 1. Always face the target
		RotateTowards(target.position);

		// --- CALCULATE STEERING FORCES ---
		Vector3 finalDirection = Vector3.zero;

		// A. Maintain Range Force
		Vector3 rangeForce = CalculateRangeForce(target.position, idealRange);

		// B. Separation Force (Crucial for not overlapping)
		Vector3 separationForce = CalculateSeparationForce(separationDist);

		// C. Strafe Force (With validation)
		UpdateStrafeTimer();
		// Check if strafing is safe. If Separation force is too high, cancel strafe to prioritize avoidance.
		bool isCrowded = separationForce.magnitude > 0.5f;
		Vector3 strafeForce = isCrowded ? Vector3.zero : CalculateValidatedStrafeForce();

		// --- COMBINE FORCES ---
		// If crowded, prioritize separation heavily
		if (isCrowded)
		{
			// Mostly separation, little range adjustment
			finalDirection = (separationForce * _separationWeight) + (rangeForce * 0.5f);
			if (_debugCombat) Debug.Log($"[{name}] Crowded! Prioritizing Separation.");
		}
		else
		{
			// Balanced movement
			finalDirection = rangeForce + (separationForce * _separationWeight) + strafeForce;
		}

		// --- APPLY TO ANIMATOR ---
		Vector3 localInput = transform.InverseTransformDirection(finalDirection.normalized);

		_animator.SetBool(_hashIsMoving, true);
		_animator.SetFloat(_hashHorizontal, localInput.x, 0.2f, Time.deltaTime);
		_animator.SetFloat(_hashVertical, localInput.z, 0.2f, Time.deltaTime);

		// Visual Debugging
		if (_debugCombat || Application.isEditor)
		{
			Debug.DrawRay(transform.position + Vector3.up, separationForce * 3, Color.red); // Avoidance
			Debug.DrawRay(transform.position + Vector3.up * 1.1f, strafeForce * 2, Color.blue); // Strafe
		}
	}
	#endregion

	#region Helper Logic (Steering Behaviors)

	private Vector3 CalculateRangeForce(Vector3 targetPos, float idealRange)
	{
		float distance = Vector3.Distance(transform.position, targetPos);

		// Deadzone of 1 unit to prevent jittering
		if (distance > idealRange + 1f) return transform.forward; // Move Closer
		if (distance < idealRange - 1f) return -transform.forward; // Back away

		return Vector3.zero; // Hold position
	}

	private Vector3 CalculateSeparationForce(float radius)
	{
		int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _allyBuffer, _allyLayer);
		Vector3 separationVector = Vector3.zero;

		for (int i = 0; i < count; i++)
		{
			if (_allyBuffer[i].gameObject == gameObject) continue;

			Vector3 toAlly = _allyBuffer[i].transform.position - transform.position;
			float dist = toAlly.magnitude;

			// Calculate repulsion strength (Inverse Square Law: Closer = Stronger push)
			// Added 0.01f to avoid division by zero
			float strength = 1.0f / (dist * dist + 0.01f);

			// Push away direction
			separationVector -= toAlly.normalized * strength;
		}

		// Normalize only if meaningful to keep direction logic
		if (separationVector.sqrMagnitude > 1f) separationVector.Normalize();

		return separationVector;
	}

	private Vector3 CalculateValidatedStrafeForce()
	{
		if (_currentStrafeDir == 0) return Vector3.zero;

		Vector3 desiredDir = transform.right * _currentStrafeDir;

		// --- VALIDATION: Raycast to check if side is clear ---
		// Ray start slightly up to hit body colliders (not feet)
		Vector3 rayStart = transform.position + Vector3.up * 0.5f;

		if (Physics.Raycast(rayStart, desiredDir, out RaycastHit hit, _strafeCheckDistance, _allyLayer))
		{
			if (_debugCombat) Debug.Log($"[{name}] Strafe Blocked by {hit.collider.name}. Stopping Strafe.");
			return Vector3.zero; // Path blocked, stop strafing
		}

		return desiredDir;
	}

	private void UpdateStrafeTimer()
	{
		_strafeTimer -= Time.deltaTime;
		if (_strafeTimer <= 0)
		{
			_strafeTimer = Random.Range(_strafeChangeInterval.x, _strafeChangeInterval.y);

			// Randomize: 30% Left, 30% Right, 40% Idle
			float rand = Random.value;
			if (rand < 0.3f) _currentStrafeDir = -1f;
			else if (rand < 0.6f) _currentStrafeDir = 1f;
			else _currentStrafeDir = 0f;
		}
	}
	#endregion

	#region Internal Logic (Normal Movement)
	// ... (Giữ nguyên phần HandleNormalMovement cũ của bạn) ...
	private void HandleNormalMovement()
	{
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