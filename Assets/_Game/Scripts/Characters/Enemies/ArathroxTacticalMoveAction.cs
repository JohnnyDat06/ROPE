using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Arathrox Tactical Move", story: "Tactical move around [Player] for [Duration] seconds", category: "Arathrox", id: "arathrox-tactical-move")]
public class ArathroxTacticalMove : Action
{
	[SerializeReference] public BlackboardVariable<Transform> Player;
	[SerializeReference] public BlackboardVariable<float> IdealRange = new BlackboardVariable<float>(8f);
	[SerializeReference] public BlackboardVariable<float> SeparationDist = new BlackboardVariable<float>(2.5f);

	// [MỚI] Thời gian duy trì hành động này trước khi trả về Success
	[SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>(3f);

	// [MỚI] Khoảng cách tối đa chấp nhận được. Nếu xa hơn số này -> Trả về Failure để chuyển sang Chase
	[SerializeReference] public BlackboardVariable<float> MaxCombatRange = new BlackboardVariable<float>(15f);

	private ArathroxMovement _movement;
	private float _timer;

	protected override Status OnStart()
	{
		if (GameObject == null) return Status.Failure;
		_movement = GameObject.GetComponent<ArathroxMovement>();

		if (_movement == null || Player.Value == null)
		{
			return Status.Failure;
		}

		// Reset timer khi bắt đầu node
		_timer = 0f;
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Player.Value == null) return Status.Failure;

		float distToPlayer = Vector3.Distance(GameObject.transform.position, Player.Value.position);

		// 1. KIỂM TRA ĐIỀU KIỆN THOÁT KHẨN CẤP (QUÁ XA)
		// Nếu Player chạy quá xa tầm chiến đấu, ta không nên Strafe nữa mà phải trả về Failure
		// Để Behavior Graph (Selector) chuyển sang nhánh Chase (Dùng NavMesh chạy cho nhanh)
		if (distToPlayer > MaxCombatRange.Value)
		{
			return Status.Failure;
		}

		// 2. THỰC HIỆN LOGIC DI CHUYỂN
		_movement.HandleCombatMovement(Player.Value, IdealRange.Value, SeparationDist.Value);

		// 3. KIỂM TRA ĐIỀU KIỆN HOÀN THÀNH (TIMER)
		_timer += Time.deltaTime;
		if (_timer >= Duration.Value)
		{
			// Đã đi ngang đủ thời gian, trả về Success để node bên dưới (VD: Attack) được chạy
			return Status.Success;
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Dừng di chuyển khi node kết thúc (dù Success hay Failure)
		if (_movement != null) _movement.Stop();
	}
}