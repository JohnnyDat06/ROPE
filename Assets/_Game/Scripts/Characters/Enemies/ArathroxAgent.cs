using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class SpiderAgent : MonoBehaviour
{
	[Header("Rotation Threshold (Hysteresis)")]
	[Range(10f, 180f)][SerializeField] private float startTurnThreshold = 60f;
	[Range(1f, 30f)][SerializeField] private float stopTurnThreshold = 8f;

	[Header("Stop")]
	[SerializeField] private float stopDistance = 0.2f;

	private NavMeshAgent agent;
	private Animator animator;

	private bool isTurningInPlace;
	private bool isOffMesh;

	private readonly int hForward = Animator.StringToHash("Vertical");
	private readonly int hStrafe = Animator.StringToHash("Horizontal");
	private readonly int hTurn = Animator.StringToHash("Turn");
	private readonly int hMoving = Animator.StringToHash("IsMoving");

	void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

		agent.updatePosition = false;   // 🔥 RẤT QUAN TRỌNG
		agent.updateRotation = false;
		agent.angularSpeed = 0;
	}

	void Update()
	{
		HandleOffMeshLink();
		HandleMovement();
	}

	void OnAnimatorMove()
	{
		Vector3 nextPos = transform.position + animator.deltaPosition;

		// Ép vị trí lên NavMesh
		if (NavMesh.SamplePosition(nextPos, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
		{
			transform.position = hit.position;
		}
		else
		{
			// Nếu lệch quá xa → đứng yên
			transform.position = transform.position;
		}

		transform.rotation *= animator.deltaRotation;

		// Sync ngược lại cho agent
		agent.nextPosition = transform.position;
	}


	public void MoveTo(Vector3 pos)
	{
		if (!agent.isOnNavMesh) return;

		agent.nextPosition = transform.position;
		agent.SetDestination(pos);
	}

	private void StopMovementOnly()
	{
		animator.SetBool(hMoving, false);
		animator.SetFloat(hForward, 0);
		animator.SetFloat(hStrafe, 0);
		animator.SetFloat(hTurn, 0);
	}

	private void StopAgentCompletely()
	{
		agent.ResetPath();
		StopMovementOnly();
	}


	private void HandleMovement()
	{
		if (!agent.hasPath || isOffMesh)
			return;

		if (agent.pathPending)
			return;

		if (agent.remainingDistance <= stopDistance)
		{
			StopAgentCompletely();
			return;
		}

		Vector3 toTarget = agent.steeringTarget - transform.position;
		toTarget.y = 0;

		if (toTarget.sqrMagnitude < 0.001f)
			return;

		Vector3 dir = toTarget.normalized;
		float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		if (!isTurningInPlace && Mathf.Abs(angle) > startTurnThreshold)
			isTurningInPlace = true;
		else if (isTurningInPlace && Mathf.Abs(angle) < stopTurnThreshold)
			isTurningInPlace = false;

		if (isTurningInPlace)
			DoTurnInPlace(angle);
		else
			DoMove(dir);
	}

	void OnDrawGizmos()
	{
		if (agent == null) return;

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(transform.position, 0.05f);

		Gizmos.color = Color.green;
		Gizmos.DrawSphere(agent.nextPosition, 0.05f);
	}



	private void DoTurnInPlace(float angle)
	{
		animator.SetBool(hMoving, false);

		float turnDir = Mathf.Sign(angle);
		animator.SetFloat(hTurn, turnDir, 0.1f, Time.deltaTime);

		animator.SetFloat(hForward, 0);
		animator.SetFloat(hStrafe, 0);
	}

	private void DoMove(Vector3 worldDir)
	{
		animator.SetBool(hMoving, true);
		animator.SetFloat(hTurn, 0, 0.2f, Time.deltaTime);

		Vector3 localDir = transform.InverseTransformDirection(worldDir);
		animator.SetFloat(hForward, localDir.z, 0.2f, Time.deltaTime);
		animator.SetFloat(hStrafe, localDir.x, 0.2f, Time.deltaTime);
	}

	private void StopAgent()
	{
		agent.ResetPath();
		animator.SetBool(hMoving, false);
		animator.SetFloat(hTurn, 0);
		animator.SetFloat(hForward, 0);
		animator.SetFloat(hStrafe, 0);
		isTurningInPlace = false;
	}

	// ================= OFF MESH =================

	private void HandleOffMeshLink()
	{
		if (agent.isOnOffMeshLink && !isOffMesh)
		{
			StartCoroutine(OffMeshRoutine(agent.currentOffMeshLinkData));
		}
	}

	private IEnumerator OffMeshRoutine(OffMeshLinkData data)
	{
		isOffMesh = true;

		animator.CrossFade("Jump", 0.15f);

		Vector3 start = data.startPos;
		Vector3 end = data.endPos;

		float t = 0;
		float duration = 0.7f;

		while (t < duration)
		{
			t += Time.deltaTime;
			float lerp = t / duration;
			transform.position = Vector3.Lerp(start, end, lerp);
			agent.nextPosition = transform.position;
			yield return null;
		}

		transform.position = end;
		agent.CompleteOffMeshLink();
		isOffMesh = false;
	}
}
