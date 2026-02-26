using UnityEngine;
using DatScript; // Gọi namespace chứa PlayerHealth của bạn kia

public class FireDamageTrigger : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    [Tooltip("Lượng máu mất mỗi lần giật")]
    [SerializeField] private float damagePerHit = 10f;

    [Tooltip("Khoảng thời gian (giây) giữa 2 lần giật máu")]
    [SerializeField] private float damageCooldown = 0.5f;

    private float nextDamageTime = 0f;

    // Chạy liên tục khi Player còn đứng trong vùng lửa (Box Collider IsTrigger)
    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextDamageTime) return;

        if (other.CompareTag("Player"))
        {
            // Tìm component PlayerHealth của bạn kia một cách an toàn
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                // Trừ máu theo đúng code gốc
                playerHealth.TakeDamage(damagePerHit);
                nextDamageTime = Time.time + damageCooldown;

                Debug.Log("<color=red>Player dính sát thương lửa: -" + damagePerHit + " máu.</color>");
            }
        }
    }
}