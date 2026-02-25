using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyVisualDebugger : MonoBehaviour
{
	[Header("--- CÀI ĐẶT ĐƯỜNG ĐI (PATH) ---")]
	public bool ShowPath = true;
	public float LineWidth = 0.15f;

	[Header("--- CÀI ĐẶT CẢM BIẾN (SENSES DEBUG) ---")]
	[Tooltip("Hãy điền giống thông số trong Behavior Graph")]
	public float VisionDistance = 15f;
	[Range(0, 360)] public float VisionAngle = 90f;
	public float ChaseRadius = 10f;
	public float AttackRange = 1.5f;

	[Header("--- MÀU SẮC DEBUG ---")]
	public Color VisionColor = new Color(0, 1, 1, 0.2f); // Cyan mờ
	public Color ChaseColor = new Color(1, 0, 0, 0.1f);  // Đỏ nhạt
	public Color AttackColor = new Color(1, 0, 0, 0.6f); // Đỏ đậm

	private NavMeshAgent _agent;
	private LineRenderer _lineRenderer;

	private void Awake()
	{
		_agent = GetComponent<NavMeshAgent>();
		_lineRenderer = GetComponent<LineRenderer>();

		// Setup LineRenderer mặc định
		_lineRenderer.startWidth = LineWidth;
		_lineRenderer.endWidth = LineWidth;
		_lineRenderer.positionCount = 0;
		_lineRenderer.useWorldSpace = true;

		// Gán material mặc định nếu chưa có
		if (_lineRenderer.material == null)
		{
			_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		}
	}

	private void Update()
	{
		HandlePathDrawing();
	}

	// --- 1. LOGIC VẼ ĐƯỜNG ĐI (GAME VIEW) ---
	private void HandlePathDrawing()
	{
		if (!ShowPath || _agent == null || _lineRenderer == null)
		{
			_lineRenderer.enabled = false;
			return;
		}

		if (_agent.hasPath)
		{
			_lineRenderer.enabled = true;

			// Đổi màu: Chạy nhanh (Đỏ) - Đi chậm (Vàng)
			Color pathColor = _agent.speed > 2.5f ? Color.red : Color.yellow;
			_lineRenderer.startColor = pathColor;
			_lineRenderer.endColor = pathColor;

			var path = _agent.path;
			if (path.corners.Length > 0)
			{
				_lineRenderer.positionCount = path.corners.Length;
				_lineRenderer.SetPositions(path.corners);
			}
		}
		else
		{
			_lineRenderer.enabled = false;
		}
	}

	// --- 2. LOGIC VẼ VÒNG TRÒN DEBUG (SCENE VIEW) ---
	private void OnDrawGizmosSelected()
	{
		// Vẽ Vòng đuổi (Chase Radius)
		Gizmos.color = ChaseColor;
		Gizmos.DrawSphere(transform.position, ChaseRadius);
		Gizmos.DrawWireSphere(transform.position, ChaseRadius);

		// Vẽ Tầm đánh (Attack Range)
		Gizmos.color = AttackColor;
		Gizmos.DrawWireSphere(transform.position, AttackRange);

		// Vẽ Tầm nhìn Hình Quạt (Vision Cone)
		DrawVisionCone();
	}

	private void DrawVisionCone()
	{
		Gizmos.color = VisionColor;
		Vector3 pos = transform.position;
		Vector3 forward = transform.forward;

		// Cạnh trái & phải của hình quạt
		Vector3 leftDir = Quaternion.Euler(0, -VisionAngle / 2, 0) * forward;
		Vector3 rightDir = Quaternion.Euler(0, VisionAngle / 2, 0) * forward;

		Gizmos.DrawLine(pos, pos + leftDir * VisionDistance);
		Gizmos.DrawLine(pos, pos + rightDir * VisionDistance);

		// Vẽ đường cung ảo nối 2 đầu (nối thẳng)
		Gizmos.DrawLine(pos + leftDir * VisionDistance, pos + rightDir * VisionDistance);
	}
}