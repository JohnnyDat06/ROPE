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
	[SerializeField] private float _lifeTime = 5f; // Tự hủy sau 5s nếu bay mất

	[Header("Damage Settings")]
	[Tooltip("Sát thương LỚN khi cục đá đập trúng trực tiếp vào người")]
	[SerializeField] private float _directDamage = 40f;
	[Tooltip("Sát thương NHỎ khi đứng gần vụ nổ")]
	[SerializeField] private float _aoeDamage = 15f;
	[Tooltip("Bán kính của vụ nổ")]
	[SerializeField] private float _aoeRadius = 3f;

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

		// Tắt sẵn hiệu ứng nổ để tránh lỗi tự phát khi vừa sinh ra
		if (_explosionVFX != null) _explosionVFX.SetActive(false);
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

		if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.collider.isTrigger) return;

		Collider directHitCollider = null;

		// 1. XỬ LÝ TRÚNG TRỰC TIẾP (DIRECT HIT)
		if (collision.gameObject.CompareTag("Player"))
		{
			// Ghi nhớ lại Collider của Player này để không tính sát thương nổ lan cho nó nữa
			directHitCollider = collision.collider;

			PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(_directDamage);
				Debug.Log($"[{name}] Rock DIRECT hit Player! Dealt {_directDamage} damage.");
			}
		}

		// 2. PHÁT NỔ VÀ TÍNH SÁT THƯƠNG LAN (AOE)
		Explode(directHitCollider);
	}

	/// <summary>
	/// Kích hoạt nổ. Truyền vào excludeCollider nếu muốn một đối tượng không bị ăn sát thương nổ 2 lần.
	/// </summary>
	public void Explode(Collider excludeCollider = null)
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

		// --- TÍNH TOÁN SÁT THƯƠNG LAN (AOE) ---
		Collider[] hits = Physics.OverlapSphere(transform.position, _aoeRadius);
		foreach (var hit in hits)
		{
			// Bỏ qua đối tượng đã ăn sát thương trực tiếp (hoặc các trigger vô hình)
			if (hit == excludeCollider || hit.isTrigger) continue;

			if (hit.CompareTag("Player"))
			{
				PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
				if (playerHealth != null)
				{
					playerHealth.TakeDamage(_aoeDamage);
					Debug.Log($"[{name}] Rock AOE hit Player! Dealt {_aoeDamage} splash damage.");
				}
			}
		}

		Destroy(gameObject, 3.5f); // Chờ VFX chạy xong rồi xóa hẳn
	}

	/// <summary>
	/// Ép cục đá nổ tung ngay lập tức (Dùng khi Boss bị choáng lúc đang cầm đá)
	/// Không gây sát thương để tránh vô tình giết Player khi Boss bị stun
	/// </summary>
	public void ExplodeImmediate()
	{
		_hasExploded = true;

		if (_rockVisuals != null) _rockVisuals.SetActive(false);
		if (_explosionVFX != null) _explosionVFX.SetActive(true);

		if (_rb != null)
		{
			_rb.linearVelocity = Vector3.zero;
			_rb.isKinematic = true;
		}

		Collider col = GetComponent<Collider>();
		if (col != null) col.enabled = false;

		Destroy(gameObject, 2f);
	}

	// Vẽ hình cầu màu đỏ trên Scene để bạn dễ dàng căn chỉnh bán kính nổ
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1, 0, 0, 0.4f);
		Gizmos.DrawWireSphere(transform.position, _aoeRadius);
	}
}