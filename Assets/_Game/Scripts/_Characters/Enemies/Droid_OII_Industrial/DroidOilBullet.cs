using DatScript;
using UnityEngine;

public class DroidOilBullet : MonoBehaviour
{
	[Header("Bullet Settings")]
	[SerializeField] private float _speed = 15f;
	[SerializeField] private float _lifetime = 5f; // Tự hủy sau 5s nếu bay trượt ra ngoài map

	[Tooltip("Độ lệch hướng bay của đạn (tạo độ tản mát ngẫu nhiên)")]
	[SerializeField] private float _spreadAngle = 2.5f; // Thay đổi góc xoay (độ) để tạo bán kính trượt

	[Header("Damage Settings")]
	[Tooltip("Gây sát thương tức thời (Chỉ 1 lần)")]
	[SerializeField] private int _instantDamage = 10;

	[Header("VFX")]
	[Tooltip("Kéo Model/Mesh của viên đạn vào đây để tắt nó đi khi va chạm")]
	[SerializeField] private GameObject _bulletVisuals;

	[Tooltip("Hiệu ứng nổ bắn tung tóe khi đạn chạm mục tiêu")]
	[SerializeField] private GameObject _impactVfxPrefab;

	private bool _hasHit = false; // Cờ khóa để đảm bảo chỉ xử lý va chạm đúng 1 lần
	private SphereCollider _collider;

	private void Start()
	{

		// Hủy an toàn nếu đạn bay mất hút
		Destroy(gameObject, _lifetime);
	}

	private void Update()
	{
		// Nếu đã chạm mục tiêu thì ngừng bay (đứng yên chờ tự hủy)
		if (_hasHit) return;

		transform.Translate(Vector3.forward * _speed * Time.deltaTime);
	}

	private void OnTriggerEnter(Collider other)
	{
		// Bỏ qua nếu đã va chạm trước đó, hoặc chạm vào Enemy, hoặc chạm vào vùng Trigger tàng hình khác
		if (_hasHit || other.CompareTag("Enemy") || other.isTrigger) return;

		// Khóa lại để đạn không xuyên qua gây sát thương 2 lần
		_hasHit = true;

		// 1. Gây sát thương nếu trúng Player
		if (other.CompareTag("Player"))
		{
			PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(_instantDamage);
			}
		}

		// 2. Bật VFX va chạm
		if (_impactVfxPrefab != null)
		{
			Instantiate(_impactVfxPrefab, transform.position, Quaternion.identity);
		}

		// 3. Tắt hình ảnh và khả năng va chạm của viên đạn
		if (_bulletVisuals != null)
		{
			_bulletVisuals.SetActive(false);
		}
		_collider.enabled = false;

		// 4. Đợi 0.5s rồi mới hủy GameObject hoàn toàn
		// Mẹo: Đợi 1 lúc giúp các hiệu ứng đuôi đạn (Trail/Particle) có thời gian mờ dần cho tự nhiên
		Destroy(gameObject, 0.5f);
	}
}