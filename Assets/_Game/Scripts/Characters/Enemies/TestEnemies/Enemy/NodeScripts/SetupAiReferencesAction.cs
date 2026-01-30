using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Setup AI References",
				 story: "Auto-assign references: Player, Eye, Waypoints, and Animator",
				 category: "Custom",
				 id: "SetupAIReferences")]
/// <summary>
/// An initialization node that automatically finds and assigns external references 
/// (Player, Eye Transform, Patrol Waypoints, Animator) to the Blackboard.
/// Usually placed at the very beginning of the behavior tree (OnStart).
/// </summary>
public partial class SetupAIReferences : Action
{
	#region Configuration (Search Settings)

	[Header("Search Criteria")]
	[Tooltip("The tag used to find the Player object.")]
	[SerializeReference]
	public BlackboardVariable<string> PlayerTag = new BlackboardVariable<string>("Player");

	[Tooltip("The name of the child object representing the AI's eyes.")]
	[SerializeReference]
	public BlackboardVariable<string> EyeName = new BlackboardVariable<string>("EyePosition");

	[Tooltip("The tag used to find all patrol waypoints in the scene.")]
	[SerializeReference]
	public BlackboardVariable<string> WaypointTag = new BlackboardVariable<string>("Waypoint");

	#endregion

	#region Blackboard Outputs (Target Variables)

	[Header("Blackboard References")]
	[Tooltip("The AI Agent itself.")]
	[SerializeReference] public BlackboardVariable<GameObject> Self;

	[Tooltip("Output variable for the Player.")]
	[SerializeReference] public BlackboardVariable<GameObject> PlayerVariable;

	[Tooltip("Output variable for the Eye position.")]
	[SerializeReference] public BlackboardVariable<GameObject> EyePositionVariable;

	[Tooltip("Output variable for the list of waypoints.")]
	[SerializeReference] public BlackboardVariable<List<GameObject>> WaypointsVariable;

	[Tooltip("Output variable for the Animator component.")]
	[SerializeReference] public BlackboardVariable<Animator> AnimatorVariable;

	#endregion

	#region Lifecycle Methods

	protected override Status OnStart()
	{
		// 0. VALIDATE SELF
		if (Self.Value == null)
		{
			Debug.LogError($"[AI Setup] '{nameof(Self)}' variable is null. Cannot proceed.");
			return Status.Failure;
		}

		// 1. AUTO-FIND PLAYER
		// Chỉ tìm nếu biến chưa được gán giá trị
		if (PlayerVariable.Value == null)
		{
			GameObject foundPlayer = GameObject.FindGameObjectWithTag(PlayerTag.Value);
			if (foundPlayer != null)
				PlayerVariable.Value = foundPlayer;
			else
				Debug.LogError($"[AI Setup] Could not find Player with tag: {PlayerTag.Value}");
		}

		// 2. AUTO-FIND EYE POSITION
		// Tìm đệ quy trong các object con của Self
		if (EyePositionVariable.Value == null)
		{
			Transform foundEye = FindChildRecursive(Self.Value.transform, EyeName.Value);

			if (foundEye != null)
				EyePositionVariable.Value = foundEye.gameObject;
			else
			{
				// Fallback: Nếu không tìm thấy mắt, dùng chính vị trí của AI
				EyePositionVariable.Value = Self.Value;
				// Debug.LogWarning($"[AI Setup] Eye child '{EyeName.Value}' not found. Defaulting to Self.");
			}
		}

		// 3. AUTO-FIND WAYPOINTS
		if (WaypointsVariable.Value == null)
		{
			WaypointsVariable.Value = new List<GameObject>();
		}

		// Tìm tất cả object có tag Waypoint trong scene
		GameObject[] foundPoints = GameObject.FindGameObjectsWithTag(WaypointTag.Value);

		if (foundPoints.Length > 0)
		{
			WaypointsVariable.Value.Clear(); // Xóa dữ liệu cũ để tránh trùng lặp khi restart
			WaypointsVariable.Value.AddRange(foundPoints);
		}

		// 4. AUTO-FIND ANIMATOR
		// Ưu tiên tìm trên Self, sau đó tìm trong Children (Model con)
		if (AnimatorVariable != null && AnimatorVariable.Value == null)
		{
			Animator anim = Self.Value.GetComponent<Animator>();

			if (anim == null)
			{
				anim = Self.Value.GetComponentInChildren<Animator>();
			}

			if (anim != null)
				AnimatorVariable.Value = anim;
			else
				Debug.LogError($"[AI Setup] No Animator found on {Self.Value.name} or its children.");
		}

		return Status.Success;
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Recursive search to find a child by name deep within the hierarchy.
	/// </summary>
	private Transform FindChildRecursive(Transform parent, string name)
	{
		foreach (Transform child in parent)
		{
			if (child.name == name) return child;

			Transform result = FindChildRecursive(child, name);
			if (result != null) return result;
		}
		return null;
	}

	#endregion
}