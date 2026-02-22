using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior; // [QUAN TRỌNG] Cần thư viện này để truy cập BehaviorGraphAgent

public class EnemyTestTakeDamage : MonoBehaviour
{
	public EnemyHealth enemyHealth;

	[Header("References")]
	[SerializeField] private Animator animator;
	[SerializeField] private NavMeshAgent agent;

	// [MỚI] Tham chiếu đến bộ não Behavior Graph
	[SerializeField] private BehaviorGraphAgent behaviorAgent;

	private void Start()
	{
		if (enemyHealth != null)
		{
			enemyHealth.OnTakeDamage += EnemyHealth_OnTakeDamage;
			enemyHealth.OnDeath += Death_OnDeath;
		}
	}

	private void OnDestroy()
	{
		if (enemyHealth != null)
		{
			enemyHealth.OnTakeDamage -= EnemyHealth_OnTakeDamage;
			enemyHealth.OnDeath -= Death_OnDeath;
		}
	}

	private void EnemyHealth_OnTakeDamage(int damage)
	{
		if (animator != null)
		{
			animator.SetTrigger("GetHit");
		}

		// Gọi Coroutine để dọn dẹp Trigger thừa
		StartCoroutine(ClearTriggerNextFrame("GetHit"));
	}

	private System.Collections.IEnumerator ClearTriggerNextFrame(string triggerName)
	{
		// Đợi đến cuối frame hiện tại
		yield return new WaitForEndOfFrame();

		// Hủy bỏ (Tắt) Trigger đi. 
		// Nếu Animator đã kịp dùng Trigger này để chuyển sang Hit rồi thì lệnh này vô hại.
		// Nếu Animator chưa kịp dùng (bị kẹt ở State khác) thì lệnh này sẽ xóa trí nhớ của nó.
		animator.ResetTrigger(triggerName);
	}

	private void Death_OnDeath(Vector3 position)
	{
		// 1. Animation Chết
		if (animator != null)
		{
			animator.SetTrigger("Dead");
		}

		// 2. Tắt NavMeshAgent (Ngưng di chuyển vật lý/đẩy nhau)
		if (agent != null)
		{
			agent.enabled = false;
		}

		// 3. [MỚI] Tắt Não (Ngưng suy nghĩ/ra lệnh)
		// Nếu không tắt, Graph vẫn sẽ cố gọi lệnh Move/Attack gây lỗi hoặc hành vi lạ
		if (behaviorAgent != null)
		{
			behaviorAgent.enabled = false;
		}
	}
}