using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the movement logic for the Arathrox enemy character.
/// Handles standard pathfinding via NavMeshAgent and custom combat steering behaviors 
/// (strafing, separation, and range maintenance) driven by the Animator.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ArathroxMovement : MonoBehaviour
{
	#region Configuration
	[Header("Movement Settings")]
	[Tooltip("Angle threshold (in degrees) to start turning in place.")]
	[SerializeField] private float _turnStartThreshold = 45f;

	[Tooltip("Angle threshold (in degrees) to stop turning in place.")]
	[SerializeField] private float _turnEndThreshold = 10f;

	[Tooltip("Rotation speed when aligning with the target.")]
	[SerializeField] private float _alignSpeed = 120f;

	[Tooltip("Distance threshold to stop the agent when reaching a destination.")]
	[SerializeField] private float _stopDistance = 0.5f;

	[Header("Combat Settings")]
	[Tooltip("Min/Max time interval (in seconds) to change strafe direction.")]
	[SerializeField] private Vector2 _strafeChangeInterval = new Vector2(1f, 3f);

	[Tooltip("Layer mask defining allied units for avoidance calculations.")]
	[SerializeField] private LayerMask _allyLayer;

	[Tooltip("Magnitude of the separation force applied to avoid crowding with allies.")]
	[SerializeField] private float _separationWeight = 3.0f; 

	[Tooltip("Distance to check for obstacles in the strafe direction via Raycast.")]
	[SerializeField] private float _strafeCheckDistance = 3.5f;

	[Header("Smoothing (Fix Jitter)")]
	[Tooltip("Time to smooth damp the input vector. Lower values = faster response, Higher values = smoother/heavier movement.")]
	[SerializeField] private float _inputSmoothTime = 0.15f;
	#endregion

	#region Internal State
	// Components
	private NavMeshAgent _agent;
	private Animator _animator;

	// State Flags
	private bool _isTurningInPlace;
	private bool _hasTarget;

	// Combat Logic Variables
	private float _strafeTimer;
	private float _currentStrafeDir; // -1 (Left), 0 (Idle), 1 (Right)
	
	// Pre-allocated buffer for OverlapSphere to avoid garbage collection
	private Collider[] _allyBuffer = new Collider[10];

	// Smoothing Variables for Damping
	private Vector3 _smoothDampVelocity; // Tracks velocity for the SmoothDamp function
	private Vector3 _currentSmoothInput; // Stores the current smoothed input vector

	// Debugging
	private bool _debugCombat = false;

	// Animator Parameter Hashes (Cached for performance)
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

		// Disable internal agent updates to allow Animator-driven movement
		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

	private void Update()
	{
		// Process standard navigation if a target is set and the agent is active
		if (_hasTarget && !_agent.isStopped)
		{
			HandleNormalMovement();
		}
	}

	private void OnAnimatorMove()
	{
		// Sync NavMeshAgent position with Root Motion from the Animator
		Vector3 newPos = transform.position + _animator.deltaPosition;

		// Ensure we are working with a valid agent on the NavMesh
		if (_agent != null && _agent.isOnNavMesh)
		{
			// 1. Horizontal Constraint (Wall Check)
			// Check if the move crosses a NavMesh boundary (wall or cliff).
			// Agent.Raycast returns true if the line to newPos hits an edge.
			if (_agent.Raycast(newPos, out NavMeshHit hit))
			{
				// We hit a wall. Clamp the position to the impact point to stop movement.
				newPos.x = hit.position.x;
				newPos.z = hit.position.z;

				// Zero out smoothing velocity to prevent "pushing" into the wall
				_currentSmoothInput = Vector3.zero;
			}

			// 2. Vertical Constraint (Ground Snap)
			// Sample the height at the valid X,Z coordinates
			if (NavMesh.SamplePosition(newPos, out NavMeshHit heightHit, 1.0f, NavMesh.AllAreas))
			{
				// Smoothly interpolate the Y height to match the mesh
				newPos.y = Mathf.Lerp(transform.position.y, heightHit.position.y, 20f * Time.deltaTime);
			}
		}

		transform.position = newPos;
		transform.rotation *= _animator.deltaRotation;
		
		// Update the internal agent position to match the transform
		if (_agent != null && _agent.isOnNavMesh)
		{
			_agent.nextPosition = transform.position;
		}
	}
	#endregion

	#region Public Methods (API)

	/// <summary>
	/// Sets a destination for the agent to travel to using standard pathfinding.
	/// </summary>
	/// <param name="position">The world position to move towards.</param>
	public void MoveTo(Vector3 position)
	{
		if (_agent.destination != position) _agent.SetDestination(position);
		_hasTarget = true;
		_agent.isStopped = false;
	}

	/// <summary>
	/// Stops the agent completely and resets animator parameters.
	/// </summary>
	public void Stop()
	{
		// Validate agent state
		if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
		{
			if (_animator != null) ResetAnimator();
			return;
		}

		// Clear the current path
		if (_agent.isOnNavMesh) _agent.ResetPath();
		_hasTarget = false;
		_agent.isStopped = true;
		ResetAnimator();
	}

	/// <summary>
	/// Handles complex combat movement including facing the target, maintaining range, 
	/// strafing, and avoiding allies. calculated forces are smoothed before applying to the Animator.
	/// </summary>
	/// <param name="target">The combat target transform.</param>
	/// <param name="idealRange">The preferred distance to maintain from the target.</param>
	/// <param name="separationDist">The radius for ally avoidance checks.</param>
	public void HandleCombatMovement(Transform target, float idealRange, float separationDist)
	{
		_hasTarget = false;
		_agent.isStopped = true;

		// Ensure we are always facing the combat target
		RotateTowards(target.position);

		// --- CALCULATE RAW FORCES ---

		// 1. Separation force (Highest Priority)
		// Pushes the character away from nearby allies to avoid clipping/crowding.
		Vector3 separationForce = CalculateSeparationForce(separationDist);
		bool isCrowded = separationForce.magnitude > 0.3f; // Threshold to determine if the area is crowded

		// 2. Range maintenance force
		// Moves the character forward or backward to reach the ideal combat range.
		// If crowded, range force is dampened to prioritize separation.
		Vector3 rangeForce = CalculateRangeForce(target.position, idealRange);
		if (isCrowded) rangeForce *= 0.3f;

		// 3. Strafe force
		// Adds lateral movement for tactical variation.
		// Completely disabled if crowded to reduce movement noise.
		Vector3 strafeForce = isCrowded ? Vector3.zero : CalculateSmartStrafeForce();

		// --- COMBINE FORCES ---
		Vector3 targetDirection = rangeForce + (separationForce * _separationWeight) + strafeForce;

		// Normalize to ensure the blend tree input doesn't exceed 1.0
		if (targetDirection.magnitude > 1f) targetDirection.Normalize();

		// --- SMOOTHING ---
		// Apply SmoothDamp to the input vector to filter out high-frequency jitter
		// caused by rapidly changing steering forces.
		_currentSmoothInput = Vector3.SmoothDamp(_currentSmoothInput, targetDirection, ref _smoothDampVelocity, _inputSmoothTime);

		// --- APPLY TO ANIMATOR ---
		// Convert world space input to local space for the Blend Tree (Horizontal/Vertical)
		Vector3 localInput = transform.InverseTransformDirection(_currentSmoothInput);

		_animator.SetBool(_hashIsMoving, true);
		
		// Feed the smoothed values into the Animator
		_animator.SetFloat(_hashHorizontal, localInput.x, 0.1f, Time.deltaTime);
		_animator.SetFloat(_hashVertical, localInput.z, 0.1f, Time.deltaTime);

		// Debug visualization
		if (_debugCombat || Application.isEditor)
		{
			Debug.DrawRay(transform.position + Vector3.up, separationForce * 3, Color.red);
			Debug.DrawRay(transform.position + Vector3.up * 1.2f, _currentSmoothInput * 2, Color.green);
		}
	}
	#endregion

	#region Helper Logic (Steering Behaviors)

	/// <summary>
	/// Calculates the forward/backward force required to maintain ideal range.
	/// </summary>
	private Vector3 CalculateRangeForce(Vector3 targetPos, float idealRange)
	{
		float distance = Vector3.Distance(transform.position, targetPos);
		
		// Use a deadzone of +/- 1.5m to prevent oscillation when near the ideal range
		if (distance > idealRange + 1.5f) return transform.forward;
		if (distance < idealRange - 1.5f) return -transform.forward;
		
		return Vector3.zero;
	}

	/// <summary>
	/// Calculates a repulsive force to avoid overlapping with allies.
	/// </summary>
	private Vector3 CalculateSeparationForce(float radius)
	{
		int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _allyBuffer, _allyLayer);
		Vector3 separationVector = Vector3.zero;

		for (int i = 0; i < count; i++)
		{
			if (_allyBuffer[i].gameObject == gameObject) continue;

			Vector3 toAlly = _allyBuffer[i].transform.position - transform.position;
			
			// Only steer away if the ally is strictly within the radius
			if (toAlly.magnitude < radius)
			{
				// Nonlinear repulsion strength: exponentially stronger as distance decreases
				float strength = 1.0f / (toAlly.sqrMagnitude + 0.1f);
				separationVector -= toAlly.normalized * strength;
			}
		}
		return separationVector;
	}

	/// <summary>
	/// Calculates lateral strafing force, changing direction periodically or upon hitting obstacles.
	/// </summary>
	private Vector3 CalculateSmartStrafeForce()
	{
		UpdateStrafeTimer();

		if (_currentStrafeDir == 0) return Vector3.zero;

		Vector3 desiredDir = transform.right * _currentStrafeDir;

		// --- OBSTACLE CHECK ---
		// Cast a ray in the intended strafe direction to detect blockages
		Vector3 rayStart = transform.position + Vector3.up * 0.5f;

		if (Physics.Raycast(rayStart, desiredDir, out RaycastHit hit, _strafeCheckDistance, _allyLayer))
		{
			// If blocked, immediately invert direction to prevent stopping/stuttering
			_currentStrafeDir *= -1;

			// Reset timer to commit to the new direction for a while
			_strafeTimer = 2.0f;

			return transform.right * _currentStrafeDir;
		}

		return desiredDir;
	}

	/// <summary>
	/// Updates the timer for changing strafe direction randomly.
	/// </summary>
	private void UpdateStrafeTimer()
	{
		_strafeTimer -= Time.deltaTime;
		if (_strafeTimer <= 0)
		{
			_strafeTimer = Random.Range(_strafeChangeInterval.x, _strafeChangeInterval.y);

			float rand = Random.value;
			// Randomized behavior distribution:
			if (rand < 0.3f) _currentStrafeDir = -1f; // 30% Chance Left
			else if (rand < 0.6f) _currentStrafeDir = 1f;  // 30% Chance Right
			else _currentStrafeDir = 0f;                     // 40% Chance Idle
		}
	}
	#endregion

	#region Internal Logic (Normal Movement)
	
	/// <summary>
	/// Processes movement logic when simply travelling to a NavMesh destination.
	/// Handles turning in place versus moving forward based on angle.
	/// </summary>
	private void HandleNormalMovement()
	{
		// Check if we have reached the destination
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// Calculate direction to the next steering target
		Vector3 targetDir = (_agent.steeringTarget - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) targetDir = transform.forward;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// Hysteresis for turning in place state to prevent flickering
		if (!_isTurningInPlace && absAngle > _turnStartThreshold) _isTurningInPlace = true;
		else if (_isTurningInPlace && absAngle < _turnEndThreshold) _isTurningInPlace = false;

		if (_isTurningInPlace)
		{
			// Rotate in place logic
			_animator.SetBool(_hashIsMoving, false);
			_animator.SetFloat(_hashTurn, Mathf.Sign(signedAngle), 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, 0); 
			_animator.SetFloat(_hashVertical, 0);
		}
		else
		{
			// Move forward logic
			_animator.SetBool(_hashIsMoving, true);
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);
			RotateTowards(_agent.steeringTarget);

			Vector3 localVel = transform.InverseTransformDirection(_agent.desiredVelocity);
			float speedFactor = Mathf.Max(_agent.speed, 1f);

			// Apply light smoothing to standard movement inputs
			_animator.SetFloat(_hashHorizontal, localVel.x / speedFactor, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, localVel.z / speedFactor, 0.1f, Time.deltaTime);
		}
	}

	/// <summary>
	/// Smoothly rotates the character to face a target position.
	/// </summary>
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

	/// <summary>
	/// Resets all relevant Animator parameters and smoothing buffers to zero.
	/// </summary>
	private void ResetAnimator()
	{
		_animator.SetBool(_hashIsMoving, false);
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
		
		// Clear smoothing history to prevent "ghost" movement on restart
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
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