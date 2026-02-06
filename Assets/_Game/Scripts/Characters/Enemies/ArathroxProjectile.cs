using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ArathroxProjectile : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Tốc độ bay của đạn")]
	[SerializeField] private float _speed = 20f;

	[Tooltip("Sát thương (để dùng sau)")]
	[SerializeField] private float _damage = 25f;

	[Tooltip("Thời gian tự hủy nếu không trúng gì (để tránh rác bộ nhớ)")]
	[SerializeField] private float _maxLifetime = 5f;

	[Header("References")]
	[Tooltip("Kéo object chứa Mesh/Hạt của viên đạn vào đây để tắt khi nổ")]
	[SerializeField] private GameObject _projectileVisuals;

	[Tooltip("Kéo object hiệu ứng nổ (đang Deactive) vào đây")]
	[SerializeField] private GameObject _explosionVFX;

	private bool _hasHit = false;
	private Rigidbody _rb;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_rb.useGravity = false; // Đạn độc bay thẳng, không rơi
		_rb.isKinematic = true; // Dùng Trigger để check va chạm
	}

	private void Start()
	{
		// Tự hủy sau 5s nếu bắn lên trời
		Destroy(gameObject, _maxLifetime);
	}

	private void Update()
	{
		// Nếu đã trúng đích (đang nổ) thì không bay nữa
		if (_hasHit) return;

		// Lao thẳng về phía trước
		transform.Translate(Vector3.forward * _speed * Time.deltaTime);
	}

	private void OnTriggerEnter(Collider other)
	{
		// 1. Nếu đã nổ rồi thì không xử lý thêm
		if (_hasHit) return;

		// 2. Bỏ qua nếu trúng đồng bọn (Layer Enemy) hoặc trúng Trigger khác (như vùng check tầm nhìn)
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.isTrigger) return;

		// 3. Xử lý trúng Player
		if (other.CompareTag("Player"))
		{
			Debug.Log($"[{name}] Hit Player! Dealing {_damage} damage.");

			// TODO: Gọi hàm TakeDamage của Player tại đây
			// var damageable = other.GetComponent<IDamageable>();
			// if (damageable != null) damageable.TakeDamage(_damage);

			// Trúng người thì hủy ngay lập tức (hoặc nổ tùy bạn, ở đây làm theo yêu cầu hủy ngay)
			Destroy(gameObject);
			return;
		}

		// 4. Xử lý trúng Môi trường (Tường, Đất, Vật cản...)
		// Yêu cầu: Active hiệu ứng nổ, tắt visual đạn, hủy sau 1s
		HandleEnvironmentHit();
	}

	private void HandleEnvironmentHit()
	{
		_hasHit = true; // Đánh dấu để ngừng di chuyển trong Update

		// Tắt hình ảnh viên đạn
		if (_projectileVisuals != null) _projectileVisuals.SetActive(false);

		// Bật hiệu ứng nổ
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		// Hủy object sau 1 giây (để VFX nổ chạy hết)
		Destroy(gameObject, 1f);
	}
}