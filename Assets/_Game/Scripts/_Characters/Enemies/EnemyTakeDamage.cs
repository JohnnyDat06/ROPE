using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

public class EnemyTestTakeDamage : MonoBehaviour
{
	public EnemyHealth enemyHealth;

	[Header("References")]
	[SerializeField] private Animator animator;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private BehaviorGraphAgent behaviorAgent;
	[SerializeField] private VisionSensor visionSensor;

	[Header("Hit Reactions")]
	[Tooltip("Thời gian hồi chiêu giữa 2 lần bị choáng (Giây)")]
	[SerializeField] private float getHitCooldown = 3f;
	private float lastGetHitTime = -999f; // Số âm để lần đầu bị bắn sẽ luôn choáng

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
		// 1. LẬP TỨC BÁO ĐỘNG (Phát hiện người chơi)
		AlertEnemy();

		// 2. XỬ LÝ GET HIT KÈM COOLDOWN
		if (Time.time >= lastGetHitTime + getHitCooldown)
		{
			if (animator != null)
			{
				animator.SetTrigger("GetHit");
			}

			lastGetHitTime = Time.time;
			StartCoroutine(ClearTriggerNextFrame("GetHit"));
		}
	}

	/// <summary>
	/// Kích hoạt trạng thái phát hiện người chơi vào thẳng "Não" của quái
	/// </summary>
	private void AlertEnemy()
	{
		//if (behaviorAgent != null && behaviorAgent.BlackboardReference != null)
		//{
		//	// Dùng hàm SetVariableValue của Unity Behavior để gán thẳng giá trị
		//	// Đảm bảo chữ "IsDetected" khớp 100% với tên biến trong Blackboard của bạn
		//	behaviorAgent.BlackboardReference.SetVariableValue("IsDetected", true);
		//}

		if (visionSensor != null)
		{
			// Triggers the alert for 5 seconds (giving the boss plenty of time to turn around)
			visionSensor.TriggerAlert(5f);
		}
	}

	private System.Collections.IEnumerator ClearTriggerNextFrame(string triggerName)
	{
		yield return new WaitForEndOfFrame();
		if (animator != null) animator.ResetTrigger(triggerName);
	}

	private void Death_OnDeath(Vector3 position)
	{
		if (animator != null) animator.SetTrigger("Dead");
		if (agent != null) agent.enabled = false;
		if (behaviorAgent != null) behaviorAgent.enabled = false;
	}
}