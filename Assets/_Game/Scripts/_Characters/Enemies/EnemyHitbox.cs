using UnityEngine;
using UnityEngine.Events;

public class EnemyHitbox : MonoBehaviour, IDamageable
{
	public enum HitboxType { Normal, Critical, WeakPoint }

	[Header("Hitbox Setup")]
	public HitboxType Type = HitboxType.Normal;
	[Tooltip("Kéo script EnemyHealth ở object cha vào đây")]
	public EnemyHealth MainHealth;

	[Header("Critical Settings (Gấp n lần)")]
	public float DamageMultiplier = 2f;

	[Header("Weak Point Settings (Điểm yếu)")]
	public int WeakPointMaxHealth = 50;
	[SerializeField] private int _currentWeakPointHealth;
	public bool IsBroken { get; private set; }

	[SerializeField] private GameObject WeakPoint;

	// Đã xóa mảng ObjectsToDisableOnBreak vì không cần thiết nữa
	public UnityEvent OnWeakPointBroken;

	// ========================================================
	// CÁC THÀNH PHẦN BẮT BUỘC CỦA IDAMAGEABLE (CS0535 FIX)
	// ========================================================
	public int curentHealth
	{
		get => MainHealth != null ? MainHealth.curentHealth : 0;
		set { if (MainHealth != null) MainHealth.curentHealth = value; }
	}

	public int maxHealth
	{
		get => MainHealth != null ? MainHealth.maxHealth : 0;
		set { if (MainHealth != null) MainHealth.maxHealth = value; }
	}

	public event IDamageable.TakeDamageEvent OnTakeDamage;
	public event IDamageable.DeathEvent OnDeath;
	// ========================================================

	private void Start()
	{
		_currentWeakPointHealth = WeakPointMaxHealth;
	}

	public void TakeDamage(int damage)
	{
		if (MainHealth == null) return;

		int finalDamage = damage;

		switch (Type)
		{
			case HitboxType.Normal:
				finalDamage = damage;
				break;

			case HitboxType.Critical:
				finalDamage = Mathf.RoundToInt(damage * DamageMultiplier);
				break;

			case HitboxType.WeakPoint:
				if (IsBroken)
				{
					finalDamage = damage;
					break;
				}

				_currentWeakPointHealth -= damage;
				finalDamage = damage;

				if (_currentWeakPointHealth <= 0)
				{
					BreakWeakPoint();
				}
				break;
		}

		MainHealth.TakeDamage(finalDamage);
	}

	private void BreakWeakPoint()
	{
		IsBroken = true; // Biến cờ điểm yếu đã bị phá vỡ

		// Gọi Event TRƯỚC khi tắt GameObject để các lệnh khác (phát âm thanh, nổ particle) kịp thực thi
		OnWeakPointBroken?.Invoke();
		Debug.Log($"[{gameObject.name}] Điểm yếu đã bị phá hủy và tự ẩn đi!");

		WeakPoint.SetActive(false);
		// Tự tắt chính nó (ẩn Mesh và vô hiệu hóa luôn Collider)
		gameObject.SetActive(false);
	}
}