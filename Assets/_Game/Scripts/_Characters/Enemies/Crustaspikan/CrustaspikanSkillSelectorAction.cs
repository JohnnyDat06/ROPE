using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Crustaspikan Skill Selector", story: "Select and execute attack on [Target]", category: "Crustaspikan", id: "crustaspikan-skill-selector")]
public partial class CrustaspikanSkillSelectorAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private CrustaspikanCombat _combat;
    private bool _hasFiredSkill = false;
    private float _attackTimer = 0f;

    protected override Status OnStart()
    {
        if (Target.Value == null || GameObject == null) return Status.Failure;
        _combat = GameObject.GetComponent<CrustaspikanCombat>();
        if (_combat == null) return Status.Failure;

        _hasFiredSkill = false;
        _attackTimer = 0f;

        Transform targetTransform = Target.Value.transform;

        // [MỚI] Ưu tiên 0: Triệu hồi đệ tử nếu đạt ngưỡng máu (60% hoặc 40%)
        if (_combat.ExecuteSummon()) { _hasFiredSkill = true; }
        // Ưu tiên 1: Tát (Nếu không gọi đệ, sẽ đánh cận chiến)
        else if (_combat.ExecuteHandAttack(targetTransform)) { _hasFiredSkill = true; }
        // Ưu tiên 2: Ném đá (Nếu không đánh cận chiến, ném đá từ xa)
        else if (_combat.ExecuteThrowRock(targetTransform)) { _hasFiredSkill = true; }

        // Nếu gọi được 1 chiêu thành công -> Chuyển sang đợi đánh xong
        return _hasFiredSkill ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (!_hasFiredSkill || _combat == null) return Status.Failure;

        _attackTimer += Time.deltaTime;

        if (!_combat.IsAttacking) return Status.Success; // Đánh xong

        // Failsafe: Chống kẹt nếu AnimEvent không chạy (ví dụ Summon anim quá dài, có thể nới lỏng ra 6f - 8f tùy độ dài anim)
        if (_attackTimer > 6f) 
        {
            _combat.CancelPendingAttacks();
            return Status.Success;
        }

        return Status.Running; // Đang tung skill -> Khóa Node
    }
}