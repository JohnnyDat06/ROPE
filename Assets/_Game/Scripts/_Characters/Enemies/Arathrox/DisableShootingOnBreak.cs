using UnityEngine;
using Unity.Behavior;

public class DisableShootingOnBreak : MonoBehaviour
{
	[Header("References")]
	[Tooltip("Kéo object chứa script EnemyHitbox (cục điểm yếu) vào đây")]
	[SerializeField] private EnemyHitbox _weakPointHitbox;

	[Tooltip("Kéo object chứa BehaviorGraphAgent (não của quái) vào đây")]
	[SerializeField] private BehaviorGraphAgent _behaviorAgent;

	[Header("Blackboard Setup")]
	[Tooltip("Tên biến trong Blackboard cần ép về false")]
	[SerializeField] private string _canShootVariableName = "CanShoot";

	private void OnEnable()
	{
		// Đăng ký lắng nghe sự kiện từ EnemyHitbox
		if (_weakPointHitbox != null)
		{
			_weakPointHitbox.OnWeakPointBroken.AddListener(OnWeakPointDestroyed);
		}
	}

	private void OnDisable()
	{
		// Hủy lắng nghe khi script tắt để tránh lỗi Memory Leak
		if (_weakPointHitbox != null)
		{
			_weakPointHitbox.OnWeakPointBroken.RemoveListener(OnWeakPointDestroyed);
		}
	}

	/// <summary>
	/// Hàm này sẽ tự động chạy khi điểm yếu bị phá vỡ
	/// </summary>
	private void OnWeakPointDestroyed()
	{
		if (_behaviorAgent != null && _behaviorAgent.BlackboardReference != null)
		{
			// Ép biến CanShoot thành false
			_behaviorAgent.BlackboardReference.SetVariableValue<bool>(_canShootVariableName, false);
			Debug.Log($"[{gameObject.name}] Điểm yếu nổ! Đã khóa biến {_canShootVariableName} thành false.");
		}
		else
		{
			Debug.LogWarning($"[{gameObject.name}] Chưa gán Behavior Agent hoặc Blackboard bị null!");
		}
	}
}