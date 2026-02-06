using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase Target", story: "Chase [Target]", category: "Arathrox/Movement", id: "ChaseTargetAction")]
public partial class ChaseTargetAction : Action
{
	[Header("Inputs")]
	[Tooltip("Đối tượng cần truy đuổi (Lấy từ Blackboard)")]
	[SerializeReference] public BlackboardVariable<GameObject> Target;

	[Tooltip("Khoảng cách dừng lại khi đã áp sát (để chuyển sang Attack)")]
	[SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(1.5f);

	[Header("References")]
	[SerializeReference] public BlackboardVariable<ArathroxMovement> MovementComponent;

	private ArathroxMovement _movement;

	protected override Status OnStart()
	{
		// 1. Tìm Component ArathroxMovement
		if (MovementComponent.Value != null)
		{
			_movement = MovementComponent.Value;
		}
		else if (_movement == null)
		{
			_movement = GameObject.GetComponent<ArathroxMovement>();
		}

		if (_movement == null)
		{
			LogFailure("ArathroxMovement is missing!");
			return Status.Failure;
		}

		if (Target.Value == null)
		{
			LogFailure("Target is null!");
			return Status.Failure;
		}

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (_movement == null || Target.Value == null) return Status.Failure;

		// 2. Cập nhật vị trí liên tục mỗi frame
		// Vì Player luôn di chuyển, ta phải gọi MoveTo liên tục
		_movement.MoveTo(Target.Value.transform.position);

		// 3. (Tùy chọn) Kiểm tra khoảng cách để báo Success
		// Tuy nhiên, thường thì ta để Node Abort (Attack Condition) ngắt việc này.
		// Nhưng nếu muốn chắc chắn, có thể trả về Success khi đã áp sát.
		float dist = Vector3.Distance(GameObject.transform.position, Target.Value.transform.position);
		if (dist <= StopDistance.Value)
		{
			// Đã áp sát -> Trả về Success để Behavior Tree chuyển sang node Attack (nếu có sequence sau nó)
			return Status.Success;
		}

		// Vẫn đang đuổi -> Running
		return Status.Running;
	}

	protected override void OnEnd()
	{
		// 4. Khi bị ngắt (Mất dấu hoặc Chuyển sang Attack)
		// Dừng di chuyển ngay lập tức
		if (_movement != null)
		{
			_movement.Stop();
		}
	}
}