using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ClickToMove : MonoBehaviour
{
	// Các biến thành viên để tham chiếu component và trạng thái
	private NavMeshAgent _agent;
	private Animator _animator;
	private bool _isOffMesh;

	// Tự động gán tham chiếu trong Editor để tránh lỗi Runtime
	// [00:05:58]
	private void OnValidate()
	{
		if (_agent == null) _agent = GetComponent<NavMeshAgent>();
		if (_animator == null) _animator = GetComponent<Animator>();
	}

	private void Update()
	{
		// 1. Xử lý Input: Bắn tia từ chuột để xác định điểm đến
		// [00:06:25]
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				_agent.SetDestination(hit.point);
			}
		}

		// 2. Xử lý Off-Mesh Link (Nhảy/Leo)
		// [00:16:43]
		if (_agent.isOnOffMeshLink)
		{
			if (!_isOffMesh)
			{
				_isOffMesh = true;
				// Bắt đầu Coroutine xử lý nhảy thủ công
				StartCoroutine(DoOffMeshLink(_agent.currentOffMeshLinkData));
			}
			return; // Ngừng xử lý di chuyển thông thường
		}

		// 3. Xử lý Di chuyển thông thường trên NavMesh
		// [00:07:59]
		if (_agent.hasPath)
		{
			// Kiểm tra khoảng cách để dừng, tránh rung lắc
			// [00:10:05]
			if (Vector3.Distance(transform.position, _agent.destination) < _agent.radius)
			{
				_agent.ResetPath();
			}

			// Tính toán hướng di chuyển trong không gian thế giới và cục bộ
			// [00:08:04]
			Vector3 worldDir = (_agent.steeringTarget - transform.position).normalized;
			Vector3 localDir = transform.InverseTransformDirection(worldDir);

			// Xoay nhân vật mượt mà về hướng di chuyển
			// [00:11:13]
			Quaternion goalRot = Quaternion.LookRotation(worldDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRot, 180f * Time.deltaTime);

			// Kiểm tra xem nhân vật đã quay mặt về hướng đi chưa (Dot Product)
			// [00:12:43]
			float dot = Vector3.Dot(transform.forward, worldDir);
			bool isFacing = dot > 0.5f; // Ngưỡng 0.5 tương đương 60 độ

			// Cập nhật tham số Animator với Dampening để chuyển động mượt
			// [00:11:42]
			_animator.SetFloat("Horizontal", isFacing ? localDir.x : 0, 0.5f, Time.deltaTime);
			_animator.SetFloat("Vertical", isFacing ? localDir.z : 0, 0.5f, Time.deltaTime);
		}
		else
		{
			// Khi không có đường đi, giảm dần tốc độ về Idle
			_animator.SetFloat("Horizontal", 0, 0.25f, Time.deltaTime);
			_animator.SetFloat("Vertical", 0, 0.25f, Time.deltaTime);
		}
	}

	// Coroutine xử lý logic nhảy qua liên kết
	// [00:17:29]
	private IEnumerator DoOffMeshLink(OffMeshLinkData data)
	{
		// Giai đoạn 1: Xoay người hướng về điểm đáp
		// [00:20:05]
		Vector3 jumpDir = (data.endPos - data.startPos).normalized;
		while (Vector3.Dot(transform.forward, jumpDir) < 0.99f)
		{
			Quaternion goalRot = Quaternion.LookRotation(jumpDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRot, 180f * Time.deltaTime);
			yield return null;
		}

		// Giai đoạn 2: Kích hoạt Animation
		_animator.CrossFade("Jump", 0.2f);

		// Giai đoạn 3: Di chuyển vị trí (Lerp)
		float totalTime = 0.7f; // Thời gian nhảy giả định
		float currentTime = totalTime;

		while (currentTime > 0)
		{
			currentTime -= Time.deltaTime;
			float t = 1 - (currentTime / totalTime);

			Vector3 goalPos = Vector3.Lerp(data.startPos, data.endPos, t);
			float elapsed = totalTime - currentTime;

			// Blend vị trí hiện tại vào quỹ đạo trong 0.3s đầu
			// [00:21:56]
			if (elapsed < 0.3f)
			{
				transform.position = Vector3.Lerp(transform.position, goalPos, elapsed / 0.3f);
			}
			else
			{
				transform.position = goalPos;
			}

			yield return null;
		}

		// Kết thúc: Gán vị trí cuối và báo cáo hoàn tất
		transform.position = data.endPos;
		_agent.CompleteOffMeshLink();
		_isOffMesh = false;
	}

	// Vẽ đường đi trong Scene view để debug
	// [00:07:04]
	private void OnDrawGizmos()
	{
		if (_agent == null | _agent.path == null) return;

		Gizmos.color = Color.red;
		for (int i = 0; i < _agent.path.corners.Length - 1; i++)
		{
			Gizmos.DrawLine(_agent.path.corners[i], _agent.path.corners[i + 1]);
		}
	}
}