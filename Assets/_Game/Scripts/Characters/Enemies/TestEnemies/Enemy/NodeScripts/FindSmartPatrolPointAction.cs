using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find Smart Patrol Point",
				 story: "[Self] finds smart patrol point from [Waypoints] to [Target]",
				 category: "Custom",
				 id: "FindSmartPatrolPointAction")]
/// <summary>
/// Selects the next patrol destination from a list of waypoints using a "Smart Patrol" algorithm.
/// Prioritizes unvisited points to ensure map coverage, with a small chance to revisit old points.
/// </summary>
public partial class FindSmartPatrolPointAction : Action
{
	#region Blackboard Data

	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<List<GameObject>> Waypoints;
	[SerializeReference] public BlackboardVariable<GameObject> Target;

	#endregion

	#region Internal State

	// Tracks visited points to prevent repetitive looping.
	// Note: This list resets if the Node is re-instantiated.
	private List<GameObject> _visitedWaypoints = new List<GameObject>();

	#endregion

	#region Lifecycle Methods

	protected override Status OnStart()
	{
		// 1. Data Validation
		if (Self.Value == null || Waypoints.Value == null || Waypoints.Value.Count == 0)
		{
			return Status.Failure;
		}

		// 2. Execute Algorithm Immediately (No need to wait for Update)
		GameObject bestPoint = GetRandomWaypointWithLogic();

		if (bestPoint != null)
		{
			Target.Value = bestPoint;
			return Status.Success;
		}

		return Status.Failure;
	}

	protected override void OnEnd() { }

	#endregion

	#region Core Logic

	private GameObject GetRandomWaypointWithLogic()
	{
		Vector3 currentPos = Self.Value.transform.position;
		List<GameObject> allPoints = Waypoints.Value;

		// --- STEP 1: FILTERING (Lọc điểm) ---
		List<GameObject> candidates = new List<GameObject>();
		foreach (var wp in allPoints)
		{
			if (wp == null) continue;

			// Chỉ lấy những điểm cách xa hơn 2m (để tránh lấy trùng điểm đang đứng)
			if (Vector3.Distance(currentPos, wp.transform.position) > 2.0f)
			{
				candidates.Add(wp);
			}
		}

		// --- EDGE CASE: FALLBACK (Xử lý lỗi kẹt) ---
		// Nếu không có điểm nào thoả mãn (VD: map chỉ có 1 điểm và đang đứng ở đó)
		// Thì chấp nhận lấy đại 1 điểm xa nhất trong danh sách gốc.
		if (candidates.Count == 0)
		{
			GameObject furthest = null;
			float maxDist = -1f;
			foreach (var wp in allPoints)
			{
				float d = Vector3.Distance(currentPos, wp.transform.position);
				if (d > maxDist) { maxDist = d; furthest = wp; }
			}
			return furthest;
		}

		// --- STEP 2: CLASSIFICATION (Phân loại) ---
		List<GameObject> unvisited = new List<GameObject>();
		List<GameObject> visited = new List<GameObject>();

		foreach (var wp in candidates)
		{
			if (_visitedWaypoints.Contains(wp)) visited.Add(wp);
			else unvisited.Add(wp);
		}

		// --- STEP 3: RESET HISTORY (Làm mới) ---
		// Nếu đã đi hết tất cả các điểm -> Xóa lịch sử để đi lại từ đầu
		if (unvisited.Count == 0)
		{
			// Debug.Log("[SmartPatrol] All points visited. Resetting history...");
			_visitedWaypoints.Clear();
			unvisited.AddRange(candidates);
			visited.Clear();
		}

		// --- STEP 4: SELECTION (Chọn điểm) ---
		GameObject finalChoice = null;

		// 10% tỷ lệ quay lại điểm cũ (để hành vi tự nhiên hơn)
		bool tryVisitOld = Random.value < 0.1f;

		if (tryVisitOld && visited.Count > 0)
		{
			finalChoice = visited[Random.Range(0, visited.Count)];
		}
		else
		{
			// 90% ưu tiên chọn điểm mới (Unvisited)
			if (unvisited.Count > 0)
				finalChoice = unvisited[Random.Range(0, unvisited.Count)];
			else
				finalChoice = candidates[Random.Range(0, candidates.Count)];
		}

		// --- STEP 5: UPDATE STATE (Cập nhật lịch sử) ---
		if (finalChoice != null)
		{
			if (!_visitedWaypoints.Contains(finalChoice))
			{
				_visitedWaypoints.Add(finalChoice);
			}
			// Debug.DrawLine(Self.Value.transform.position, finalChoice.transform.position, Color.green, 2.0f);
		}

		return finalChoice;
	}

	#endregion
}