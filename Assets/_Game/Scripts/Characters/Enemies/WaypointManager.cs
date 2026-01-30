using UnityEngine;
using System.Collections.Generic;

public class WaypointManager : MonoBehaviour
{
	[Header("Patrol Points")]
	[Tooltip("Kéo thả các điểm đi tuần vào đây")]
	public List<Transform> waypoints = new List<Transform>();

	[Header("Debug")]
	public Color gizmoColor = Color.yellow;
	public float radius = 0.3f;

	// Hàm tiện ích để lấy một điểm ngẫu nhiên
	public Vector3 GetRandomWaypoint()
	{
		if (waypoints == null || waypoints.Count == 0) return Vector3.zero;
		return waypoints[Random.Range(0, waypoints.Count)].position;
	}

	// Vẽ trong Scene để dễ nhìn
	private void OnDrawGizmos()
	{
		Gizmos.color = gizmoColor;
		foreach (Transform t in waypoints)
		{
			if (t != null)
			{
				Gizmos.DrawSphere(t.position, radius);
				// Vẽ đường nối các điểm nếu muốn
			}
		}
	}
}