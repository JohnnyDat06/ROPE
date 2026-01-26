using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Is Player Detected",
				 story: "Is [Self] detecting [Player]",
				 category: "MutantAI",
				 id: "IsPlayerDetectedAction")]
/// <summary>
/// Checks if the Player is within the Agent's vision cone or chase radius using Raycasts.
/// Updates the [LastKnownPos] blackboard variable.
/// </summary>
public partial class IsPlayerDetectedAction : Action
{
	#region Input / Output Variables

	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<GameObject> Player;
	[SerializeReference] public BlackboardVariable<GameObject> EyePosition;

	[Tooltip("Output variable to store the player's position when detected.")]
	[SerializeReference] public BlackboardVariable<Vector3> LastKnownPos;

	[Tooltip("Flag indicating a target is active.")]
	[SerializeReference] public BlackboardVariable<bool> HasInvestigateTarget;

	#endregion

	#region Sensor Configuration

	[Header("Vision Settings")]
	[Tooltip("Field of view angle in degrees.")]
	[SerializeReference] public BlackboardVariable<float> VisionAngle = new BlackboardVariable<float>(90f);

	[Tooltip("Maximum sight distance.")]
	[SerializeReference] public BlackboardVariable<float> VisionDistance = new BlackboardVariable<float>(10f);

	[Tooltip("Radius within which the agent will keep chasing even without line of sight (Pseudo-sense).")]
	[SerializeReference] public BlackboardVariable<float> ChaseRadius = new BlackboardVariable<float>(15f);

	[Header("Yield Settings")]
	[Tooltip("If distance is less than this (usually AttackRange), return Failure to stop chasing and allow Attacking.")]
	[SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(1.5f);

	#endregion

	#region Internal State

	private int _blockedLayerMask;

	#endregion

	#region Lifecycle Methods

	protected override Status OnStart()
	{
		// Pre-calculate layer mask for Obstacles (Walls, Default objects)
		_blockedLayerMask = LayerMask.GetMask("Obstacle", "Default", "Wall");
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Self.Value == null || Player.Value == null || EyePosition.Value == null)
			return Status.Failure;

		Transform eyeTransform = EyePosition.Value.transform;
		Transform playerTransform = Player.Value.transform;
		Vector3 vectorToPlayer = playerTransform.position - eyeTransform.position;
		float distanceToPlayer = vectorToPlayer.magnitude;

		// --- LOGIC 1: NHƯỜNG QUYỀN TẤN CÔNG (Yield to Attack) ---
		// Nếu Player đã nằm trong tầm đánh (StopDistance), Node này trả về FAILURE.
		// Mục đích: Ngắt nhánh Chase (Đuổi theo) để Tree chuyển sang nhánh Attack (Tấn công).
		if (distanceToPlayer <= StopDistance.Value)
		{
			// Debug.Log("Target in range. Yielding to Attack Logic.");
			return Status.Failure;
		}

		// --- LOGIC 2: KIỂM TRA TẦM NHÌN (Vision Check) ---
		bool hasVisualContact = false;

		if (distanceToPlayer <= VisionDistance.Value)
		{
			float halfAngle = VisionAngle.Value * 0.5f;
			float angleToPlayer = Vector3.Angle(eyeTransform.forward, vectorToPlayer);

			// Nếu ở quá gần (<1m) thì coi như nhìn thấy luôn (kể cả sau lưng)
			bool angleOK = (distanceToPlayer <= 1.0f) || (angleToPlayer <= halfAngle);

			if (angleOK)
			{
				Collider playerCollider = Player.Value.GetComponent<Collider>();
				if (playerCollider != null)
				{
					// Check 3 điểm: Tâm, Đỉnh đầu, Chân để tránh bị che khuất một phần
					if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.center)) hasVisualContact = true;
					else if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.max)) hasVisualContact = true;
					else if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.min)) hasVisualContact = true;
				}
				else
				{
					// Fallback nếu không có collider
					if (CheckLineOfSight(eyeTransform.position, playerTransform.position)) hasVisualContact = true;
				}
			}
		}

		// --- LOGIC 3: VÙNG TRUY ĐUỔI (Chase Radius) ---
		// Nếu đã ở trong vùng này, AI sẽ "cảm nhận" được Player kể cả khi mất dấu nhìn (giả lập nghe tiếng bước chân gần)
		bool isInsideChaseRadius = distanceToPlayer <= ChaseRadius.Value;

		if (hasVisualContact || isInsideChaseRadius)
		{
			UpdateTargetData();
			return Status.Running; // Tiếp tục Chase
		}
		else
		{
			return Status.Failure; // Mất dấu -> Chuyển sang Investigate hoặc Patrol
		}
	}

	protected override void OnEnd() { }

	#endregion

	#region Helper Methods

	/// <summary>
	/// Performs a Raycast to check for obstacles between the eye and the target point.
	/// </summary>
	private bool CheckLineOfSight(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		float distance = direction.magnitude;

		if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance))
		{
			// Nếu raycast trúng Player hoặc con của Player
			if (hit.collider.gameObject == Player.Value || hit.transform.IsChildOf(Player.Value.transform))
				return true;

			// Nếu raycast trúng vật cản (Layer Obstacle/Wall)
			if (((1 << hit.collider.gameObject.layer) & _blockedLayerMask) != 0)
				return false;
		}
		// Không trúng gì cả (nghĩa là không có vật cản) -> Nhìn thấy
		return true;
	}

	private void UpdateTargetData()
	{
		if (LastKnownPos != null && Player.Value != null)
		{
			LastKnownPos.Value = Player.Value.transform.position;

			if (HasInvestigateTarget != null)
			{
				HasInvestigateTarget.Value = true;
			}
		}
	}

	#endregion
}