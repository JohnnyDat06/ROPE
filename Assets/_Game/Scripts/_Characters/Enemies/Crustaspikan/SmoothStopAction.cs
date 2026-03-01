using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(
	name: "Smooth Stop",
	story: "[Agent] smoothly decelerates to a complete stop",
	category: "Action/Movement",
	id: "crustaspikan-smooth-stop")]
public partial class SmoothStopAction : Action
{
	[Tooltip("The GameObject of the Boss that has the CrustaspikanMovement script.")]
	[SerializeReference] public BlackboardVariable<GameObject> Agent;

	private CrustaspikanMovement _movement;

	protected override Status OnStart()
	{
		// 1. Validate the Agent
		if (Agent.Value == null)
		{
			Debug.LogWarning("SmoothStopAction: Agent is null.");
			return Status.Failure;
		}

		// 2. Cache the movement component for performance
		_movement = Agent.Value.GetComponent<CrustaspikanMovement>();
		if (_movement == null)
		{
			Debug.LogWarning($"SmoothStopAction: CrustaspikanMovement not found on {Agent.Value.name}.");
			return Status.Failure;
		}

		// 3. Start the stopping process
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (_movement == null) return Status.Failure;

		// Execute the smooth deceleration math every frame.
		// Returns TRUE only when both Horizontal and Vertical momentum are near 0.
		bool isFullyStopped = _movement.ExecuteSmoothStop();

		if (isFullyStopped)
		{
			// The boss has firmly planted its feet. We can move on to the next node (e.g., Attack).
			return Status.Success;
		}

		// The boss is still sliding. Keep the node locked in the Running state.
		return Status.Running;
	}
}