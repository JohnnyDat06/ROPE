using DatScript;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class CrustaspikanRock : MonoBehaviour
{
	[Header("Rock Settings")]
	[Tooltip("Lực ném bay đi")]
	[SerializeField] private float _throwSpeed = 30f;
	[Tooltip("Lực xoay (để cục đá lộn nhào trên không cho tự nhiên)")]
	[SerializeField] private float _tumbleSpeed = 10f;
	[SerializeField] private float _damage = 40f;
	[SerializeField] private float _lifeTime = 5f; // Tự hủy sau 5s nếu bay mất

	[Header("Effects")]
	[SerializeField] private GameObject _rockVisuals;
	[SerializeField] private GameObject _explosionVFX; // Hiệu ứng vỡ đá

	private Rigidbody _rb;
	private bool _isLaunched = false;
	private bool _hasExploded = false;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		// Khi nằm trên tay Boss, nó chưa bị rớt và là vật thể Kinematic
		_rb.isKinematic = true;
		_rb.useGravity = false;
	}

	/// <summary>
	/// Hàm này được CrustaspikanCombat gọi khi đến Frame ném.
	/// </summary>
	public void Launch(Vector3 direction)
	{
		_isLaunched = true;

		// Bật vật lý
		_rb.isKinematic = false;
		_rb.useGravity = true;

		// Set vận tốc bay thẳng
		_rb.linearVelocity = direction * _throwSpeed; // Dùng velocity ở Unity 6+ (Hoặc _rb.velocity ở bản cũ)

		// Set một lực xoay ngẫu nhiên để cục đá lộn vòng vòng
		_rb.angularVelocity = Random.insideUnitSphere * _tumbleSpeed;

		Destroy(gameObject, _lifeTime);
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Nếu chưa được ném ra khỏi tay hoặc đã nổ rồi thì bỏ qua
		if (!_isLaunched || _hasExploded) return;

		// [CẬP NHẬT THEO ARATHROX] Bỏ qua va chạm với Enemy HOẶC các vùng Trigger vô hình
		if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.collider.isTrigger) return;

		// Xử lý trúng Player
		if (collision.gameObject.CompareTag("Player"))
		{
			// [CẬP NHẬT THEO ARATHROX] Gọi hàm TakeDamage qua PlayerHealth
			PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(_damage);
				Debug.Log($"[{name}] Rock hit Player! Dealt {_damage} damage.");
			}

			// Không return ở đây vì dù trúng người chơi, cục đá vẫn phải nát ra
		}

		// Trúng Player, trúng đất hay trúng tường đều nổ
		Explode();
	}

	public void Explode()
	{
		_hasExploded = true;

		// Tắt hình ảnh cục đá
		if (_rockVisuals != null) _rockVisuals.SetActive(false);

		// Bật hiệu ứng vỡ nát (VFX)
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		// Dừng di chuyển
		_rb.linearVelocity = Vector3.zero;
		_rb.isKinematic = true;
		GetComponent<Collider>().enabled = false;

		Destroy(gameObject, 2f); // Chờ VFX chạy xong rồi xóa hẳn
	}

	/// <summary>
	/// Ép cục đá nổ tung ngay lập tức (Dùng khi Boss bị choáng lúc đang cầm đá)
	/// </summary>
	public void ExplodeImmediate()
	{
		_hasExploded = true;

		// Tắt hình ảnh cục đá (Mesh)
		if (_rockVisuals != null) _rockVisuals.SetActive(false);

		// Bật Particle Effect bụi đá/vỡ vụn
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		// Hủy bỏ mọi tác động vật lý để nó không rớt xuống
		if (_rb != null)
		{
			_rb.linearVelocity = Vector3.zero;
			_rb.isKinematic = true;
		}

		Collider col = GetComponent<Collider>();
		if (col != null) col.enabled = false;

		// Xóa game object sau 2s để chờ VFX chạy xong
		Destroy(gameObject, 2f);
	}
}