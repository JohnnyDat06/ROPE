using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get Next Waypoint", story: "Get next waypoint from [Manager] and save to [TargetLocation]", category: "Arathrox/Patrol", id: "GetWaypointAction")]
public partial class GetWaypointAction : Action
{
	[Header("References")]
	[Tooltip("Biến Blackboard để lưu vị trí điểm đến")]
	[SerializeReference] public BlackboardVariable<Vector3> TargetLocation;

	[Tooltip("Nếu để trống, sẽ tự tìm WaypointManager trên GameObject hiện tại")]
	[SerializeReference] public BlackboardVariable<WaypointManager> Manager;

	[Header("Settings")]
	[Tooltip("Bán kính lệch ngẫu nhiên xung quanh Waypoint (Giúp di chuyển tự nhiên hơn)")]
	[SerializeReference] public BlackboardVariable<float> RandomOffset = new BlackboardVariable<float>(0.5f);

	// Cache component để tối ưu hiệu năng
	private WaypointManager _cachedManager;

	protected override Status OnStart()
	{
		// 1. Tìm WaypointManager
		if (Manager.Value != null)
		{
			_cachedManager = Manager.Value;
		}
		else if (_cachedManager == null)
		{
			// Tự tìm trên chính Agent
			_cachedManager = GameObject.GetComponent<WaypointManager>();
			if (_cachedManager == null)
			{
				LogFailure("Không tìm thấy WaypointManager trên GameObject này!");
				return Status.Failure;
			}
		}

		// 2. Lấy điểm đến tiếp theo từ Manager
		Vector3 nextPoint = _cachedManager.GetNextWaypoint();

		// 3. Xử lý độ lệch ngẫu nhiên (Random Offset)
		// Đây là tính năng "khoảng cách lệch cho phép" giúp quái di chuyển tự nhiên
		if (RandomOffset.Value > 0)
		{
			Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * RandomOffset.Value;
			nextPoint += new Vector3(randomCircle.x, 0, randomCircle.y);
		}

		// 4. Lưu vào Blackboard để Node Move sử dụng
		TargetLocation.Value = nextPoint;

		// Hành động chỉ lấy dữ liệu nên xong ngay lập tức -> Success
		return Status.Success;
	}
}