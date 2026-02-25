using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
// Cập nhật tên, story, category và id cho phù hợp với Boss mới
[NodeDescription(
	name: "Crustaspikan Heavy Move",
	story: "Heavy tactical move around [Player] for [Duration] seconds",
	category: "Crustaspikan",
	id: "crustaspikan-heavy-move")]
public class CrustaspikanHeavyMoveAction : Action
{
	[SerializeReference] public BlackboardVariable<Transform> Player;

	// Khảng cách Boss muốn giữ để chuẩn bị ra đòn (ví dụ: đấm đập đất cần đứng gần hơn bắn độc)
	[SerializeReference] public BlackboardVariable<float> IdealRange = new BlackboardVariable<float>(4f);

	// [ĐÃ XÓA] SeparationDist vì Boss khổng lồ không thèm né đường cho lính lác

	[SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>(3f);
	[SerializeReference] public BlackboardVariable<float> MaxCombatRange = new BlackboardVariable<float>(15f);

	// [CẬP NHẬT] Đổi tham chiếu sang script di chuyển của Boss
	private CrustaspikanMovement _movement;
	private float _timer;

	protected override Status OnStart()
	{
		if (GameObject == null) return Status.Failure;

		// Lấy component CrustaspikanMovement thay vì ArathroxMovement
		_movement = GameObject.GetComponent<CrustaspikanMovement>();

		if (_movement == null || Player.Value == null)
		{
			return Status.Failure;
		}

		_timer = 0f;
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Player.Value == null) return Status.Failure;

		float distToPlayer = Vector3.Distance(GameObject.transform.position, Player.Value.position);

		// 1. Kiểm tra thoát khẩn cấp nếu Player chạy quá xa
		if (distToPlayer > MaxCombatRange.Value)
		{
			return Status.Failure;
		}

		// 2. [CẬP NHẬT] Gọi hàm di chuyển hạng nặng (Heavy Combat)
		// Boss sẽ lùi/tiến từ từ với quán tính lớn, không có Strafe
		_movement.HandleHeavyCombatMovement(Player.Value, IdealRange.Value);

		// 3. Kiểm tra hoàn thành theo thời gian
		_timer += Time.deltaTime;
		if (_timer >= Duration.Value)
		{
			// Hết thời gian "gườm" nhau, trả về Success để chuyển sang Node tấn công
			return Status.Success;
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Dừng di chuyển khi node kết thúc để đảm bảo Boss khóa cứng vị trí khi bắt đầu tung đòn
		if (_movement != null) _movement.Stop();
	}
}