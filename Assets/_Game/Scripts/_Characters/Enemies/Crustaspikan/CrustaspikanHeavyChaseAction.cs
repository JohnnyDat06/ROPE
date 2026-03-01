using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
// [CẬP NHẬT] Đổi category thành "Enemy AI" để nó nằm chung chỗ với Node Chase cũ của bạn
[NodeDescription(
	name: "Crustaspikan Heavy Chase",
	story: "Heavy chase [Target] stopping at [IdealRange]",
	category: "Enemy AI",
	id: "CrustaspikanHeavyChaseAction")]
public partial class CrustaspikanHeavyChaseAction : Action
{
	[Header("Inputs")]
	[Tooltip("The target GameObject to chase (retrieved from the Blackboard).")]
	[SerializeReference] public BlackboardVariable<GameObject> Target;

	[Tooltip("The optimal distance the boss wants to maintain before attacking.")]
	[SerializeReference] public BlackboardVariable<float> IdealRange = new BlackboardVariable<float>(3f);

	// Cached reference to the boss's specific movement component
	private CrustaspikanMovement _movement;

	protected override Status OnStart()
	{
		if (Target == null || Target.Value == null)
		{
			Debug.LogWarning("CrustaspikanHeavyChase: Target is null or unassigned!");
			return Status.Failure;
		}

		if (GameObject != null)
		{
			_movement = GameObject.GetComponent<CrustaspikanMovement>();
		}

		if (_movement == null)
		{
			Debug.LogError($"CrustaspikanHeavyChase: CrustaspikanMovement missing on {GameObject.name}!");
			return Status.Failure;
		}

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (_movement == null || Target == null || Target.Value == null)
			return Status.Failure;

		// Bơm gia tốc tiến/lùi nặng nề cho Boss
		_movement.HandleHeavyCombatMovement(Target.Value.transform, IdealRange.Value);

		// Kiểm tra khoảng cách
		float distanceToTarget = Vector3.Distance(GameObject.transform.position, Target.Value.transform.position);

		if (distanceToTarget <= IdealRange.Value)
		{
			return Status.Success; // Đến tầm -> Báo Success để chuyển sang Smooth Stop
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Khi Node kết thúc, gọi Stop() để giữ lại quán tính cho Node Smooth Stop kế tiếp
		if (_movement != null)
		{
			_movement.Stop();
		}
	}
}