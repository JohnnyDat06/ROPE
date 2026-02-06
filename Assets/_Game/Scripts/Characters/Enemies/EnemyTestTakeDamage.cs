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
	}

	private void Death_OnDeath(Vector3 position)
	{
		// 1. Animation Chết
		if (animator != null)
		{
			animator.SetBool("IsDead", true);
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