using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
// [CẬP NHẬT] Đổi Category thành "Enemy AI" để dùng chung cho mọi quái
[NodeDescription(name: "Move To Position", story: "Move Agent to [TargetPosition]", category: "Enemy AI", id: "MoveToPositionAction")]
public partial class MoveToPositionAction : Action
{
	[Header("Inputs")]
	[Tooltip("Vị trí cần đến (Lấy từ Blackboard)")]
	[SerializeReference] public BlackboardVariable<Vector3> TargetPosition;

	[Tooltip("Khoảng cách chấp nhận đã đến đích (Nên khớp hoặc lớn hơn EnemyMovement settings một chút)")]
	[SerializeReference] public BlackboardVariable<float> StoppingDistance = new BlackboardVariable<float>(0.6f);

	[Header("References")]
	[Tooltip("Component di chuyển (Tự động tìm nếu để trống)")]
	// [CẬP NHẬT] Thay BlackboardVariable thành lớp cha EnemyMovement
	[SerializeReference] public BlackboardVariable<EnemyMovement> MovementComponent;

	// [CẬP NHẬT] Cache biến lớp cha
	private EnemyMovement _movement;
	private NavMeshAgent _agent;

	protected override Status OnStart()
	{
		// 1. Tìm Component EnemyMovement
		// Nhờ tính đa hình, nó sẽ lấy được ArathroxMovement, CrustaspikanMovement, v.v.
		if (MovementComponent != null && MovementComponent.Value != null)
		{
			_movement = MovementComponent.Value;
		}

		if (_movement == null && GameObject != null)
		{
			_movement = GameObject.GetComponent<EnemyMovement>();
		}

		if (_movement == null)
		{
			LogFailure("Không tìm thấy component kế thừa EnemyMovement trên GameObject!");
			return Status.Failure;
		}

		// Lấy NavMeshAgent từ GameObject
		if (_agent == null && GameObject != null)
		{
			_agent = GameObject.GetComponent<NavMeshAgent>();
		}

		// 2. Ra lệnh di chuyển 
		// (Sẽ gọi hàm MoveTo ảo, lớp con nào ghi đè thì chạy logic lớp con đó)
		_movement.MoveTo(TargetPosition.Value);

		// Trả về Running để giữ node này hoạt động trong các frame tiếp theo
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (_movement == null || _agent == null) return Status.Failure;

		// 3. Kiểm tra xem đã đến nơi chưa
		// Lưu ý: Chúng ta check pathPending để tránh trường hợp Agent chưa kịp tính toán đường đi
		if (!_agent.pathPending)
		{
			if (_agent.remainingDistance <= StoppingDistance.Value)
			{
				// Đã đến nơi
				return Status.Success;
			}
		}

		// Vẫn đang đi -> Tiếp tục giữ trạng thái Running
		return Status.Running;
	}

	protected override void OnEnd()
	{
		// 4. Xử lý khi Node kết thúc (Hoặc bị ABORT)
		// Đây là điểm quan trọng cho cơ chế Abort:
		// Nếu nhánh Patrol đang chạy node này mà bị nhánh Chase ngắt ngang,
		// OnEnd sẽ được gọi. Ta cần Stop ngay để quái không bị trôi.

		if (_movement != null)
		{
			// Chỉ gọi Stop nếu thực sự cần thiết (tránh conflict nếu Success tự nhiên)
			// Tuy nhiên, EnemyMovement.Stop() khá an toàn để gọi nhiều lần.
			_movement.Stop();
		}
	}
}