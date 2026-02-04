using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Arathrox Tactical Move", story: "Tactical move around [Player]", category: "Arathrox AI", id: "arathrox-tactical-move")]
public class ArathroxTacticalMove : Action
{
	[SerializeReference] public BlackboardVariable<Transform> Player;
	[SerializeReference] public BlackboardVariable<float> IdealRange = new BlackboardVariable<float>(10f);
	[SerializeReference] public BlackboardVariable<float> SeparationDist = new BlackboardVariable<float>(2.5f);

	private ArathroxMovement _movement;

	protected override Status OnStart()
	{
		if (GameObject == null) return Status.Failure;
		_movement = GameObject.GetComponent<ArathroxMovement>();

		if (_movement == null || Player.Value == null)
		{
			return Status.Failure;
		}
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Player.Value == null) return Status.Failure;

		// GỌI HÀM LOGIC CHIẾN THUẬT MỖI FRAME
		_movement.HandleCombatMovement(Player.Value, IdealRange.Value, SeparationDist.Value);

		// Luôn trả về Running để duy trì hành vi (cho đến khi Player chết hoặc chuyển State khác)
		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Khi thoát khỏi node này (VD: Player đi quá xa), cho dừng lại
		if (_movement != null) _movement.Stop();
	}
}