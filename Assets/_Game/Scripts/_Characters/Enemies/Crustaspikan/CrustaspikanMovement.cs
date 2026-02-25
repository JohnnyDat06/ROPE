using UnityEngine;

/// <summary>
/// Movement logic for the giant boss "Crustaspikan".
/// Inherits basic locomotion from EnemyMovement but implements heavy, high-momentum combat steering.
/// Removes strafing mechanics and focuses on smooth acceleration/deceleration.
/// </summary>
public class CrustaspikanMovement : EnemyMovement
{
	#region Configuration (Boss Specific)
	[Header("Heavy Boss Settings")]
	[Tooltip("Time to smooth damp the input vector. Higher values = heavier feel, slower to stop (gorilla momentum).")]
	[SerializeField] private float _heavySmoothTime = 0.6f;

	[Tooltip("Distance threshold to trigger backing up. Giant bosses rarely back up, so this can be small.")]
	[SerializeField] private float _backupDistanceThreshold = 1.5f;
	#endregion

	#region Internal State
	// Smoothing Variables for Damping
	private Vector3 _smoothDampVelocity;
	private Vector3 _currentSmoothInput;

	// Flag to detect state transition for seamless animation blending
	private bool _isEnteringCombat = true;
	#endregion

	#region Public Methods (API)

	/// <summary>
	/// Handles heavy combat movement. Strict forward/backward movement with large momentum.
	/// </summary>
	/// <param name="target">The player transform.</param>
	/// <param name="idealRange">The distance the boss wants to maintain before attacking.</param>
	public void HandleHeavyCombatMovement(Transform target, float idealRange)
	{
		// Disable base NavMesh navigation during combat
		if (_hasTarget)
		{
			_hasTarget = false;
			_agent.isStopped = true;
		}

		// [FIX] Giant bosses especially need Turn In Place when flanked
		if (TryTurnInPlace(target.position))
		{
			// Reset momentum to prevent sliding while turning
			_currentSmoothInput = Vector3.zero;
			_smoothDampVelocity = Vector3.zero;

			// Set flag to true so after turning, it inherits new velocity smoothly
			_isEnteringCombat = true;
			return;
		}

		// [SEAMLESS BLENDING] Inherit velocity from the Animator when first entering combat state
		// This prevents the boss from suddenly stopping when switching from Chase to Combat node.
		if (_isEnteringCombat)
		{
			float currentVertical = _animator.GetFloat(_hashVertical);
			// Assume we were moving forward. Seed the smoother with current animator momentum.
			_currentSmoothInput = transform.forward * currentVertical;
			_isEnteringCombat = false;
		}

		// 1. Rotate towards target (Slowly align to face the player)
		RotateTowards(target.position);

		// 2. Calculate Forces (Forward or Backward only, NO Strafing)
		Vector3 targetDirection = CalculateRangeForce(target.position, idealRange);

		// 3. Heavy Smoothing (The Momentum Core)
		// If targetDirection is Vector3.zero (reached destination), _currentSmoothInput will slowly decay to zero.
		_currentSmoothInput = Vector3.SmoothDamp(
			_currentSmoothInput,
			targetDirection,
			ref _smoothDampVelocity,
			_heavySmoothTime
		);

		// --- APPLY TO ANIMATOR ---
		// Convert world space input to local space
		Vector3 localInput = transform.InverseTransformDirection(_currentSmoothInput);

		// Check if we still have momentum
		_animator.SetBool(_hashIsMoving, _currentSmoothInput.magnitude > 0.05f);

		// Use _heavySmoothTime instead of 0.1f to enforce the heavy, sluggish animation transition
		_animator.SetFloat(_hashHorizontal, localInput.x, _heavySmoothTime, Time.deltaTime);
		_animator.SetFloat(_hashVertical, localInput.z, _heavySmoothTime, Time.deltaTime);

		// Debug visualization
		if (Application.isEditor)
		{
			Debug.DrawRay(transform.position + Vector3.up * 2f, _currentSmoothInput * 3, Color.cyan);
		}
	}

	#endregion

	#region Helper Logic

	/// <summary>
	/// Calculates movement towards or away from the target based on ideal range.
	/// </summary>
	private Vector3 CalculateRangeForce(Vector3 targetPos, float idealRange)
	{
		float distance = Vector3.Distance(transform.position, targetPos);

		// Move closer if too far
		if (distance > idealRange + _backupDistanceThreshold)
			return transform.forward;

		// Slowly back up if too close (Optional for giant bosses)
		if (distance < idealRange - _backupDistanceThreshold)
		{
			// Check if backing up is safe (from base class logic)
			Vector3 backPos = transform.position - transform.forward * 1.5f;
			if (_agent.isOnNavMesh && _agent.Raycast(backPos, out UnityEngine.AI.NavMeshHit hit))
			{
				return Vector3.zero; // Wall behind, stop
			}
			return -transform.forward;
		}

		// In the "sweet spot". Return zero. 
		// The SmoothDamp will cause the boss to slide to a halt naturally.
		return Vector3.zero;
	}

	#endregion

	#region Base Overrides

	/// <summary>
	/// Resets the combat flag when returning to standard NavMesh movement.
	/// </summary>
	public override void MoveTo(Vector3 position)
	{
		base.MoveTo(position);
		_isEnteringCombat = true; // Ready for next transition
	}

	public override void Stop()
	{
		base.Stop();
		_isEnteringCombat = true;
	}

	/// <summary>
	/// Overrides the base Wall Hit hook to kill momentum when hitting a wall.
	/// </summary>
	protected override void OnWallHit()
	{
		// A giant boss hitting a wall loses its forward momentum instantly
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
	}

	protected override void ResetAnimator()
	{
		base.ResetAnimator();
		_currentSmoothInput = Vector3.zero;
		_smoothDampVelocity = Vector3.zero;
	}

	#endregion
}