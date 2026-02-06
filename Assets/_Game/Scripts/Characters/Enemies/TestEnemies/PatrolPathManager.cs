using UnityEngine;
using System.Collections.Generic;

public class PatrolPathManager : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Kéo các GameObject Waypoint vào đây")]
	[SerializeField] private List<Transform> _waypoints = new List<Transform>();

	[Tooltip("Thứ tự đi tuần: True = Đi vòng tròn (1->2->3->1), False = Đi đến cuối rồi đứng lại")]
	[SerializeField] private bool _loop = true;

	[Header("Debug")]
	[SerializeField] private Color _pathColor = Color.cyan;

	// Biến lưu trạng thái nội bộ
	private int _currentIndex = -1; // Bắt đầu chưa đi điểm nào

	/// <summary>
	/// Lấy tọa độ của điểm tuần tra tiếp theo.
	/// Hàm này tự động tăng index lên.
	/// </summary>
	public Vector3 GetNextWaypoint()
	{
		if (_waypoints.Count == 0)
		{
			Debug.LogWarning($"[PatrolPathManager] {name} chưa có Waypoint nào!");
			return transform.position; // Trả về vị trí hiện tại để không lỗi
		}

		// Tăng index
		_currentIndex++;

		// Xử lý vòng lặp
		if (_currentIndex >= _waypoints.Count)
		{
			if (_loop)
			{
				_currentIndex = 0; // Quay về đầu
			}
			else
			{
				_currentIndex = _waypoints.Count - 1; // Giữ nguyên ở cuối
			}
		}

		return _waypoints[_currentIndex].position;
	}

	/// <summary>
	/// Kiểm tra xem còn điểm nào để đi tiếp không (Dùng cho trường hợp không Loop)
	/// </summary>
	public bool HasNextPoint()
	{
		if (_loop) return true; // Loop thì lúc nào cũng có điểm tiếp
		return _currentIndex < _waypoints.Count - 1;
	}

	// --- Vẽ đường đi trong Editor để dễ nhìn ---
	private void OnDrawGizmos()
	{
		// 1. Kiểm tra list có bị null không (Lỗi UnassignedReferenceException thường do cái này)
		if (_waypoints == null) return;

		// 2. Kiểm tra list có rỗng không
		if (_waypoints.Count == 0) return;

		Gizmos.color = _pathColor;

		// 3. Duyệt qua từng điểm
		for (int i = 0; i < _waypoints.Count; i++)
		{
			// [QUAN TRỌNG] Kiểm tra từng phần tử con có bị null không
			// (Phòng trường hợp bạn tăng Size list lên 5 nhưng chưa kéo object vào, nó sẽ là null)
			if (_waypoints[i] == null) continue;

			// Vẽ cục tròn tại điểm
			Gizmos.DrawSphere(_waypoints[i].position, 0.3f);

			// Vẽ đường nối đến điểm tiếp theo
			if (i < _waypoints.Count - 1)
			{
				// Kiểm tra điểm tiếp theo có tồn tại không trước khi vẽ đường nối
				if (_waypoints[i + 1] != null)
				{
					Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
				}
			}
			// Vẽ đường nối từ cuối về đầu (nếu Loop)
			else if (_loop && _waypoints.Count > 1)
			{
				// Kiểm tra điểm đầu tiên có tồn tại không
				if (_waypoints[0] != null)
				{
					Gizmos.DrawLine(_waypoints[i].position, _waypoints[0].position);
				}
			}
		}
	}
}