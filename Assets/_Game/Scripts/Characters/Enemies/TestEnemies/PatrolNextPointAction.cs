using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Spider Patrol Next", story: "Move Spider to next waypoint", category: "Spider AI", id: "spider-patrol-next")]
public class SpiderPatrolNextAction : Action
{
	private SpiderAgent _agent;
	private PatrolPathManager _pathManager;

	// Lưu vị trí đích để tự kiểm tra
	private Vector3 _targetPos;
	// Ngưỡng khoảng cách để coi là "đã đến nơi" (vd: 1 mét)
	private float _stopDistance = 1.0f;

	protected override Status OnStart()
	{
		if (GameObject == null) return Status.Failure;

		_agent = GameObject.GetComponent<SpiderAgent>();
		_pathManager = GameObject.GetComponent<PatrolPathManager>();

		if (_agent == null || _pathManager == null) return Status.Failure;

		// 1. Lấy điểm đến
		_targetPos = _pathManager.GetNextWaypoint();

		// 2. Ra lệnh di chuyển
		_agent.MoveTo(_targetPos);

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (_agent == null) return Status.Failure;

		// --- LOGIC SỬA LỖI ---

		// Tự tính khoảng cách từ Quái đến Điểm Đích (bỏ qua trục Y để chính xác hơn)
		float distance = Vector3.Distance(
			new Vector3(GameObject.transform.position.x, 0, GameObject.transform.position.z),
			new Vector3(_targetPos.x, 0, _targetPos.z)
		);

		// Chỉ trả về Success khi thực sự đã đến rất gần điểm đích
		if (distance <= _stopDistance)
		{
			// Đảm bảo dừng hẳn lại
			_agent.Stop();
			return Status.Success;
		}

		// Nếu chưa đến nơi -> Tiếp tục gửi lệnh Move (đề phòng quái bị đẩy lệch hướng)
		// Dòng này giúp quái kiên quyết quay lại điểm đó nếu lỡ đi lố
		_agent.MoveTo(_targetPos);

		return Status.Running;
	}
}