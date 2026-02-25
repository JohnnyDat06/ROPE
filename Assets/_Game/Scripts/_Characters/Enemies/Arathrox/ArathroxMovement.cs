using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the tactical movement logic specifically for the Arathrox enemy character.
/// Inherits standard pathfinding from EnemyMovement and implements custom combat steering behaviors.
/// </summary>
public class ArathroxMovement : EnemyMovement
{
	#region Configuration (Combat Specific)
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

	#region Internal State (Combat Specific)
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
	#endregion

	#region Public Methods (API)

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

		// [FIX] Force Turn In Place check first
		// If the Player moves behind the enemy, it must stop and rotate instead of sliding
		if (TryTurnInPlace(target.position))
		{
			// Reset movement momentum to prevent sliding while turning
			_currentSmoothInput = Vector3.zero;
			_smoothDampVelocity = Vector3.zero;
			return; // Exit function, skip strafe calculations
		}

		// If angle is small (finished turning or Player is in front), smoothly rotate and begin combat
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

		// Move closer
		if (distance > idealRange + 1.5f) return transform.forward;

		// Move away (Back up)
		if (distance < idealRange - 1.5f)
		{
			// Fix: Check if backing up is safe (not hitting NavMesh edge)
			// Cast a ray 1.5m backwards to see if we have space.
			// _agent.Raycast returns true if the line touches a NavMesh boundary (wall/edge).
			Vector3 backPos = transform.position - transform.forward * 1.5f;

			if (_agent.isOnNavMesh && _agent.Raycast(backPos, out NavMeshHit hit))
			{
				// Edge detected behind us, stop backing up to prevent jitter
				return Vector3.zero;
			}

			return -transform.forward;
		}

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

	#region Base Overrides

	/// <summary>
	/// Overrides the base Wall Hit hook to reset combat smoothing buffers.
	/// </summary>
	protected override void OnWallHit()
	{
		// Zero out smoothing velocity to prevent "pushing" into the wall
		_currentSmoothInput = Vector3.zero;
	}

	/// <summary>
	/// Resets all relevant Animator parameters and smoothing buffers to zero.
	/// </summary>
	protected override void ResetAnimator()
	{
		base.ResetAnimator();

		// Clear smoothing history to prevent "ghost" movement on restart
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
	}

	#endregion
}