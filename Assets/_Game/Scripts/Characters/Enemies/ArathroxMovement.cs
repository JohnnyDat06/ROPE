using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ArathroxMovement : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Góc lệch lớn hơn giá trị này sẽ BẮT ĐẦU xoay tại chỗ")]
	[SerializeField] private float _turnStartThreshold = 45f;

	[Tooltip("Góc lệch nhỏ hơn giá trị này sẽ KẾT THÚC xoay tại chỗ")]
	[SerializeField] private float _turnEndThreshold = 10f;

	[Tooltip("Tốc độ xoay thủ công khi đang di chuyển (độ/giây)")]
	[SerializeField] private float _alignSpeed = 120f;

	[Tooltip("Khoảng cách chấp nhận đã đến đích")]
	[SerializeField] private float _stopDistance = 0.5f;

	// Components
	private NavMeshAgent _agent;
	private Animator _animator;

	// State Variables
	private bool _isTurningInPlace;
	private bool _hasTarget;

	// Animator Hashes
	private readonly int _hashHorizontal = Animator.StringToHash("Horizontal");
	private readonly int _hashVertical = Animator.StringToHash("Vertical");
	private readonly int _hashTurn = Animator.StringToHash("Turn");
	private readonly int _hashIsMoving = Animator.StringToHash("IsMoving");

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		_agent.updateRotation = false;
		_agent.updatePosition = false;
	}

	public void MoveTo(Vector3 position)
	{
		_agent.SetDestination(position);
		_hasTarget = true;
		_agent.isStopped = false;
	}

	public void Stop()
	{
		_agent.ResetPath();
		_hasTarget = false;
		_agent.isStopped = true;

		// Reset animator
		_animator.SetBool(_hashIsMoving, false);
		_animator.SetFloat(_hashHorizontal, 0);
		_animator.SetFloat(_hashVertical, 0);
		_animator.SetFloat(_hashTurn, 0);
	}

	private void Update()
	{
		if (!_hasTarget) return;

		// Check target reached
		if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
		{
			Stop();
			return;
		}

		// Calculate target direction
		Vector3 targetDir = (_agent.steeringTarget - transform.position).normalized;
		targetDir.y = 0;
		if (targetDir == Vector3.zero) targetDir = transform.forward;

		float signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		// Check turn in place state
		if (!_isTurningInPlace)
		{
			if (absAngle > _turnStartThreshold)
			{
				_isTurningInPlace = true;
			}
		}
		else
		{
			// Currently turning in place
			if (absAngle < _turnEndThreshold)
			{
				_isTurningInPlace = false;
			}
		}

		if (_isTurningInPlace)
		{
			_animator.SetBool(_hashIsMoving, false);

			float turnVal = Mathf.Sign(signedAngle);
			_animator.SetFloat(_hashTurn, turnVal, 0.1f, Time.deltaTime);

			// Reset Locomotion
			_animator.SetFloat(_hashHorizontal, 0);
			_animator.SetFloat(_hashVertical, 0);
		}
		else
		{
			_animator.SetBool(_hashIsMoving, true);
			_animator.SetFloat(_hashTurn, 0, 0.2f, Time.deltaTime);

			if (absAngle > 0.1f)
			{
				Quaternion targetRot = Quaternion.LookRotation(targetDir);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _alignSpeed * Time.deltaTime);
			}

			// Get local velocity
			Vector3 desiredVel = _agent.desiredVelocity;
			Vector3 localVel = transform.InverseTransformDirection(desiredVel);

			float speedFactor = _agent.speed > 0 ? _agent.speed : 1f;
			Vector3 normalizedInput = localVel / speedFactor;

			_animator.SetFloat(_hashHorizontal, normalizedInput.x, 0.1f, Time.deltaTime);
			_animator.SetFloat(_hashVertical, normalizedInput.z, 0.1f, Time.deltaTime);
		}
	}

	// Sync NavMeshAgent position with Animator root motion
	private void OnAnimatorMove()
	{
		Vector3 newPos = transform.position + _animator.deltaPosition;
		NavMeshHit hit;

		if (NavMesh.SamplePosition(newPos, out hit, 1.0f, NavMesh.AllAreas))
		{
			float targetY = hit.position.y;
			newPos.y = Mathf.Lerp(transform.position.y, targetY, 20f * Time.deltaTime);
		}

		transform.position = newPos;
		transform.rotation *= _animator.deltaRotation;

		_agent.nextPosition = transform.position;
	}

	// Debug Gizmos
	private void OnDrawGizmosSelected()
	{
		if (_agent != null && _agent.hasPath)
		{
			Gizmos.color = _isTurningInPlace ? Color.red : Color.green;
			Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
			Gizmos.DrawLine(transform.position, _agent.steeringTarget);
		}
	}
}