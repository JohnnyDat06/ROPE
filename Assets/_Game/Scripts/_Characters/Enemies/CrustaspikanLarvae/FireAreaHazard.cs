using DatScript;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FireAreaHazard : MonoBehaviour
{
	[Header("Fire Settings")]
	[Tooltip("Thời gian tồn tại của bãi lửa (giây)")]
	[SerializeField] private float _duration = 5f;

	[Tooltip("Sát thương đốt mỗi giây")]
	[SerializeField] private int _burnDamagePerTick = 10;

	[Tooltip("Khoảng cách giữa các lần đốt (1 = mỗi giây đốt 1 lần)")]
	[SerializeField] private float _tickInterval = 1f;

	// Danh sách các đối tượng đang đứng trong lửa
	private List<GameObject> _targetsInFire = new List<GameObject>();

	private void Start()
	{
		// Đảm bảo Collider là dạng Trigger để mọi người đi xuyên qua được
		GetComponent<BoxCollider>().isTrigger = true;

		// Bắt đầu vòng lặp đốt máu
		StartCoroutine(BurnRoutine());

		// Tự hủy bãi lửa sau 5 giây
		Destroy(gameObject, _duration);
	}

	private void OnTriggerEnter(Collider other)
	{
		// Nếu là Player hoặc Quái đi vào bãi lửa thì thêm vào danh sách nướng
		if (other.CompareTag("Player") || other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
		{
			if (!_targetsInFire.Contains(other.gameObject))
			{
				_targetsInFire.Add(other.gameObject);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		// Nếu chạy thoát khỏi bãi lửa thì xóa khỏi danh sách
		if (_targetsInFire.Contains(other.gameObject))
		{
			_targetsInFire.Remove(other.gameObject);
		}
	}

	private IEnumerator BurnRoutine()
	{
		while (true)
		{
			// Đợi đủ thời gian Tick (ví dụ: 1 giây)
			yield return new WaitForSeconds(_tickInterval);

			// Lọc lại danh sách, xóa những mục tiêu đã chết (bị Destroy) để tránh lỗi NullReference
			_targetsInFire.RemoveAll(target => target == null);

			// Gây sát thương cho tất cả những ai còn đang đứng trong lửa
			foreach (GameObject target in _targetsInFire)
			{
				if (target.CompareTag("Player"))
				{
					PlayerHealth ph = target.GetComponent<PlayerHealth>();
					if (ph != null) ph.TakeDamage(_burnDamagePerTick);
				}
				else
				{
					EnemyHealth eh = target.GetComponent<EnemyHealth>();
					if (eh != null) eh.TakeDamage(_burnDamagePerTick);
				}
			}
		}
	}
}