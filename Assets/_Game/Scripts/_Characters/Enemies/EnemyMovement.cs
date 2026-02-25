using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for enemy movement.
/// Handles standard pathfinding via NavMeshAgent and base locomotion.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyMovement : MonoBehaviour
{
	#region Configuration
	[Header("Movement Settings")]
	[Tooltip("Angle threshold (in degrees) to start turning in place.")]
	[SerializeField] protected float _turnStartThreshold = 45f;

	[Tooltip("Angle threshold (in degrees) to stop turning in place.")]
	[SerializeField] protected float _turnEndThreshold = 10f;

	[Tooltip("Rotation speed when aligning with the target.")]
	[SerializeField] protected float _alignSpeed = 120f;

	[Tooltip("Distance threshold to stop the agent when reaching a destination.")]
	[SerializeField] protected float _stopDistance = 0.5f;

	[Tooltip("Enable/disable turn-in-place animation feature.")]
	[SerializeField] protected bool _useTurnInPlace = true;
	#endregion

	#region Internal State
	// Components
	protected NavMeshAgent _agent;
	protected Animator _animator;

	// State Flags
	protected bool _isTurningInPlace;
	protected bool _hasTarget;

	// Animator Parameter Hashes (Cached for performance)
	protected readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	protected readonly int _hashVertical = Animator.StringToHash("Vertical");
	protected readonly int _hashTurn = Animator.StringToHash("Turn");
	protected readonly int _hashIsMoving = Animator.StringToHash("IsMoving");
	#endregion

	#region Unity Lifecycle
	protected virtual void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		// Disable internal agent updates to allow Animator-driven movement
		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

	protected virtual void Update()
	{
		// Process standard navigation if a target is set and the agent is active
		if (_hasTarget && !_agent.isStopped)
		{
			HandleNormalMovement();
		}
	}

	protected virtual void OnAnimatorMove()
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

				// Hook for derived classes to handle custom logic upon hitting a wall
				OnWallHit();
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
	public virtual void MoveTo(Vector3 position)
	{
		if (_agent.destination != position) _agent.SetDestination(position);
		_hasTarget = true;
		_agent.isStopped = false;
	}

	/// <summary>
	/// Stops the agent completely and resets animator parameters.
	/// </summary>
	public virtual void Stop()
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

	#endregion

	#region Helper Logic (Internal Movement)

	/// <summary>
	/// Checks and performs Turn In Place if the angle deviation is too large.
	/// Returns true if the enemy IS currently in a turning state.
	/// </summary>
	protected virtual bool TryTurnInPlace(Vector3 targetPos)
	{
		// If the feature is disabled via Inspector -> Always return false
		if (!_useTurnInPlace) return false;

		Vector3 targetDir = (targetPos - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) return false;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// Hysteresis logic to prevent flickering
		if (!_isTurningInPlace && absAngle > _turnStartThreshold) _isTurningInPlace = true;
		else if (_isTurningInPlace && absAngle < _turnEndThreshold) _isTurningInPlace = false;

		if (_isTurningInPlace)
		{
			_animator.SetBool(_hashIsMoving, false);
			_animator.SetFloat(_hashTurn, Mathf.Sign(signedAngle), 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashHorizontal, 0);
			_animator.SetFloat(_hashVertical, 0);
			return true; // Currently turning
		}

		// Reset Turn parameter if not turning in place
		_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);
		return false;
	}

	/// <summary>
	/// Processes movement logic when simply travelling to a NavMesh destination.
	/// Handles turning in place versus moving forward based on angle.
	/// </summary>
	protected void HandleNormalMovement()
	{
		// Check if we have reached the destination
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// Use shared function. If currently turning, exit early without moving forward
		if (TryTurnInPlace(_agent.steeringTarget)) return;

		// Move forward logic
		_animator.SetBool(_hashIsMoving, true);
		RotateTowards(_agent.steeringTarget);

		Vector3 localVel = transform.InverseTransformDirection(_agent.desiredVelocity);
		float speedFactor = Mathf.Max(_agent.speed, 1f);

		// Apply light smoothing to standard movement inputs
		_animator.SetFloat(_hashHorizontal, localVel.x / speedFactor, 0.1f, Time.deltaTime);
		_animator.SetFloat(_hashVertical, localVel.z / speedFactor, 0.1f, Time.deltaTime);
	}

	/// <summary>
	/// Smoothly rotates the character to face a target position.
	/// </summary>
	protected void RotateTowards(Vector3 targetPos)
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
	/// Base implementation to reset Animator parameters.
	/// </summary>
	protected virtual void ResetAnimator()
	{
		_animator.SetBool(_hashIsMoving, false);
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
	}

	protected virtual void OnWallHit()
	{
		// Hook to be overridden by child classes if needed
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (_agent != null && _hasTarget)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, _agent.steeringTarget);
		}
	}
	#endregion
}