using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[Condition(name: "Is Player Detected",
		   story: "Checks if [Self] can see [Player] from [EyePosition] (Cone & Raycast)",
		   category: "MutantAI",
		   id: "IsPlayerDetectedCondition")]
/// <summary>
/// A Condition node that validates visibility.
/// It checks Distance -> View Angle -> Raycast Line of Sight (3 points).
/// Returns True if the player is visible, False otherwise.
/// </summary>
public partial class IsPlayerDetectedCondition : Condition
{
	#region Input Data

	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<GameObject> Player;
	[SerializeReference] public BlackboardVariable<GameObject> EyePosition;

	#endregion

	#region Configuration

	[Header("Vision Settings")]
	[Tooltip("The field of view angle (in degrees).")]
	[SerializeReference] public BlackboardVariable<float> VisionAngle = new BlackboardVariable<float>(90f);

	[Tooltip("The maximum distance the AI can see.")]
	[SerializeReference] public BlackboardVariable<float> VisionDistance = new BlackboardVariable<float>(10f);

	#endregion

	#region Internal State

	private int _blockedLayerMask;

	#endregion

	#region Lifecycle

	public override void OnStart()
	{
		// Pre-calculate the LayerMask for obstacles (Performance Optimization)
		_blockedLayerMask = LayerMask.GetMask("Obstacle", "Default", "Wall");
	}

	public override void OnEnd() { }

	#endregion

	#region Core Logic

	public override bool IsTrue()
	{
		// 1. Safety Checks
		if (Self.Value == null || Player.Value == null || EyePosition.Value == null)
		{
			return false;
		}

		Transform eyeTransform = EyePosition.Value.transform;
		Transform playerTransform = Player.Value.transform;
		Vector3 vectorToPlayer = playerTransform.position - eyeTransform.position;
		float distanceToPlayer = vectorToPlayer.magnitude;

		// 2. Distance Check
		if (distanceToPlayer > VisionDistance.Value)
		{
			return false; // Out of range
		}

		// 3. Angle Check (With Proximity Exception)
		float halfAngle = VisionAngle.Value * 0.5f;
		float angleToPlayer = Vector3.Angle(eyeTransform.forward, vectorToPlayer);

		if (angleToPlayer > halfAngle)
		{
			// Logic: Nếu ở quá gần (< 1m) thì bỏ qua góc nhìn (Cảm nhận được ngay cả khi ở sau lưng)
			if (distanceToPlayer > 1.0f)
				return false;
		}

		// 4. Raycast Line of Sight (Multi-point Check)
		// Kiểm tra 3 điểm: Tâm, Đỉnh đầu, Chân để tránh trường hợp bị che khuất một phần.
		Collider playerCollider = Player.Value.GetComponent<Collider>();

		if (playerCollider != null)
		{
			if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.center)) return true;
			if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.max)) return true;
			if (CheckLineOfSight(eyeTransform.position, playerCollider.bounds.min)) return true;
		}
		else
		{
			// Fallback: Chỉ kiểm tra tâm nếu không có Collider
			if (CheckLineOfSight(eyeTransform.position, playerTransform.position)) return true;
		}

		return false; // Blocked by obstacle
	}

	/// <summary>
	/// Performs a raycast to check visibility against blocked layers.
	/// </summary>
	private bool CheckLineOfSight(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		float distance = direction.magnitude;

		if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance))
		{
			// A. Trúng Player hoặc con của Player -> THẤY
			if (hit.collider.gameObject == Player.Value || hit.transform.IsChildOf(Player.Value.transform))
			{
				return true;
			}

			// B. Trúng vật cản (Layer nằm trong danh sách chặn) -> KHÔNG THẤY
			if (((1 << hit.collider.gameObject.layer) & _blockedLayerMask) != 0)
			{
				return false;
			}
		}

		// Không trúng gì cả (hoặc trúng layer xuyên thấu) -> THẤY
		return true;
	}

	#endregion
}