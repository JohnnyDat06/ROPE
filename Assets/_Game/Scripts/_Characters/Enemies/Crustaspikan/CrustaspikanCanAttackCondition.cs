using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Crustaspikan Attack", story: "Can execute any skill on [Target]", category: "Crustaspikan", id: "crustaspikan-can-attack-cond")]
public partial class CrustaspikanCanAttackCondition : Condition
{
	[SerializeReference] public BlackboardVariable<GameObject> Target;
	private CrustaspikanCombat _combat;

	// [ĐÃ SỬA] Đổi protected thành public để khớp với lớp cơ sở Condition
	public override void OnStart()
	{
		if (GameObject != null)
		{
			_combat = GameObject.GetComponent<CrustaspikanCombat>();
		}
	}

	// [ĐĐÃ SỬA] Đổi protected thành public
	public override bool IsTrue()
	{
		if (_combat == null || Target.Value == null) return false;

		// Trả về True nếu Boss đang sẵn sàng và Player nằm trong vùng đánh/ném
		return _combat.IsAnySkillReady(Target.Value.transform);
	}
}