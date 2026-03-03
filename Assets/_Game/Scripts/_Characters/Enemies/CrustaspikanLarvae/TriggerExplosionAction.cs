using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
	name: "Trigger Explosion",
	story: "[Agent] detonates itself immediately",
	category: "Enemy AI",
	id: "CrustaspikanLarvaeTriggerExplosion")]
public partial class TriggerExplosionAction : Action
{
	[Tooltip("GameObject của con quái mang bom (Thường là Self).")]
	[SerializeReference] public BlackboardVariable<GameObject> Agent;

	protected override Status OnStart()
	{
		// 1. Kiểm tra tính hợp lệ của Agent
		if (Agent == null || Agent.Value == null)
		{
			Debug.LogWarning("TriggerExplosionAction: Agent bị trống!");
			return Status.Failure;
		}

		// 2. Lấy script Combat của con Larvae
		CrustaspikanLarvaeCombat combatScript = Agent.Value.GetComponent<CrustaspikanLarvaeCombat>();

		if (combatScript != null)
		{
			// 3. Kích nổ ngay lập tức
			combatScript.TriggerExplosion();

			// Vì object đã bị Destroy ngay trong hàm trên, luồng Graph ở đây thực chất đã bị cắt đứt.
			// Nhưng ta vẫn trả về Success đúng chuẩn cấu trúc Behavior Tree.
			return Status.Success;
		}
		else
		{
			Debug.LogError($"TriggerExplosionAction: Không tìm thấy script CrustaspikanLarvaeCombat trên {Agent.Value.name}");
			return Status.Failure;
		}
	}
}