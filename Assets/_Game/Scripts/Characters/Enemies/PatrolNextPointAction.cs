using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

// [QUAN TRỌNG] Phải có dòng này để Unity nhận diện đây là Action Node
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Spider Patrol Next", story: "Move Spider to next waypoint", category: "Spider AI", id: "spider-patrol-next")]
public class SpiderPatrolNextAction : Action
{
	// Không cần biến input nào vì ta lấy trực tiếp từ component
	private SpiderAgent _agent;
	private PatrolPathManager _pathManager;
	private bool _hasRequestedMove;

	protected override Status OnStart()
	{
		if (GameObject == null)
		{
			return Status.Failure;
		}

		// Lấy các component cần thiết
		_agent = GameObject.GetComponent<SpiderAgent>();
		_pathManager = GameObject.GetComponent<PatrolPathManager>();

		if (_agent == null || _pathManager == null)
		{
			// Log lỗi để biết nếu quên gắn script
			Debug.LogError($"[SpiderPatrolNextAction] GameObject '{GameObject.name}' thiếu SpiderAgent hoặc PatrolPathManager!");
			return Status.Failure;
		}

		// Thực hiện hành động
		Vector3 nextPoint = _pathManager.GetNextWaypoint();
		_agent.MoveTo(nextPoint);
		_hasRequestedMove = true;

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		// Nếu component bị mất giữa chừng (hiếm khi)
		if (_agent == null) return Status.Failure;

		if (_hasRequestedMove)
		{
			// Kiểm tra biến IsMoving từ script SpiderAgent của bạn
			// Nếu IsMoving = false (đã đến nơi hoặc bị kẹt) -> Trả về Success
			if (!_agent.IsMoving)
			{
				return Status.Success;
			}
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Tùy chọn: Dừng agent khi node kết thúc
		// if (_agent != null) _agent.Stop(); 
	}
}