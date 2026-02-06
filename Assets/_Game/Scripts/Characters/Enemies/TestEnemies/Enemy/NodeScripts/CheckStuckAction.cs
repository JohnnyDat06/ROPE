using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Watchdog: Check Stuck & Recover",
				 story: "Monitor [Agent]. If stuck: Set [IsStuck] True, reset [HasInvestigateTarget], and blind for [BlindDuration]s",
				 category: "MutantAI",
				 id: "CheckStuck")]
/// <summary>
/// A "Self-Healing" Watchdog node.
/// It runs in parallel to monitor if the Agent is physically stuck.
/// If stuck, it triggers a recovery sequence: Flags the stuck state, forces the Agent to abandon the target, 
/// and temporarily "blinds" the Agent to allow a reset via the Patrol branch.
/// </summary>
public partial class CheckStuckAction : Action
{
	#region Input / Output Variables

	[SerializeReference]
	public BlackboardVariable<GameObject> Agent;

	[Tooltip("Flag to signal the main tree that the agent is stuck.")]
	[SerializeReference]
	public BlackboardVariable<bool> IsStuck;

	[Tooltip("Flag indicating if the agent has an investigation target. Will be forced to False when stuck.")]
	[SerializeReference]
	public BlackboardVariable<bool> HasInvestigateTarget;

	[Header("Vision Control (For Blinding)")]
	[Tooltip("Reference to the Agent's View Distance variable.")]
	[SerializeReference]
	public BlackboardVariable<float> VisionDistance;

	[Tooltip("Reference to the Agent's Detection Range variable.")]
	[SerializeReference]
	public BlackboardVariable<float> DetectRange;

	#endregion

	#region Configuration

	[Header("Stuck Detection Settings")]
	[Tooltip("The movement threshold radius. If the agent stays within this radius, it is considered stationary.")]
	[SerializeReference]
	public BlackboardVariable<float> Radius = new BlackboardVariable<float>(1.0f);

	[Tooltip("Time limit (in seconds) to stay within the Radius before triggering recovery.")]
	[SerializeReference]
	public BlackboardVariable<float> TimeLimit = new BlackboardVariable<float>(15.0f);

	[Header("Recovery Settings")]
	[Tooltip("Duration (in seconds) to blind the agent, allowing it to transition to Patrol without re-detecting the player immediately.")]
	[SerializeReference]
	public BlackboardVariable<float> BlindDuration = new BlackboardVariable<float>(5.0f);

	[Tooltip("The original Vision Distance to restore after recovery.")]
	[SerializeReference]
	public BlackboardVariable<float> NormalVisionDist = new BlackboardVariable<float>(14f);

	[Tooltip("The original Detection Range to restore after recovery.")]
	[SerializeReference]
	public BlackboardVariable<float> NormalDetectRange = new BlackboardVariable<float>(2f);

	#endregion

	#region Internal State

	private Vector3 _anchorPosition;
	private float _timerStuck;    // Timer for detection phase
	private float _timerRecover;  // Timer for recovery phase
	private bool _isRecovering;   // State machine flag

	#endregion

	#region Lifecycle Methods

	protected override Status OnStart()
	{
		if (Agent.Value == null) return Status.Failure;

		ResetWatchdog(); // Khởi tạo trạng thái ban đầu
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Agent.Value == null) return Status.Failure;

		if (_isRecovering)
		{
			// --- PHA 2: ĐANG HỒI PHỤC (Bị mù) ---
			HandleRecoveryPhase();
		}
		else
		{
			// --- PHA 1: ĐANG CANH GÁC (Bình thường) ---
			HandleMonitoringPhase();
		}

		return Status.Running; // Chạy ngầm vĩnh viễn (Background Service)
	}

	protected override void OnEnd()
	{
		// Đảm bảo khi tắt game hoặc stop node thì trả lại thông số, tránh bị mù vĩnh viễn
		if (VisionDistance != null) VisionDistance.Value = NormalVisionDist.Value;
		if (DetectRange != null) DetectRange.Value = NormalDetectRange.Value;
	}

	#endregion

	#region Core Logic

	/// <summary>
	/// Giám sát vị trí của Agent. Nếu đứng yên quá lâu trong 1 vùng -> Kích hoạt Recovery.
	/// </summary>
	private void HandleMonitoringPhase()
	{
		float distance = Vector3.Distance(Agent.Value.transform.position, _anchorPosition);

		if (distance > Radius.Value)
		{
			// Đã di chuyển ra khỏi vùng kẹt -> Reset bộ đếm
			_anchorPosition = Agent.Value.transform.position;
			_timerStuck = 0f;
		}
		else
		{
			// Vẫn ở trong vòng tròn -> Đếm giờ
			_timerStuck += Time.deltaTime;

			if (_timerStuck >= TimeLimit.Value)
			{
				StartRecovery(); // Kẹt quá lâu -> Chuyển sang chế độ cứu hộ
			}
		}
	}

	/// <summary>
	/// Kích hoạt chế độ "Tự chữa lành": Báo kẹt, Hủy mục tiêu và Làm mù.
	/// </summary>
	private void StartRecovery()
	{
		Debug.LogWarning($"⚠️ [Watchdog] Mutant stuck for {TimeLimit.Value}s! Initiating Recovery Protocol.");

		_isRecovering = true;
		_timerRecover = 0f;

		// 1. Báo cáo kẹt (để Graph chính Abort nhánh Investigate/Chase)
		if (IsStuck != null) IsStuck.Value = true;

		// 2. Hủy mục tiêu điều tra (Force Quit Investigate logic)
		if (HasInvestigateTarget != null) HasInvestigateTarget.Value = false;

		// 3. Làm mù (để nhánh Patrol không bị ngắt quãng bởi Chase)
		if (VisionDistance != null) VisionDistance.Value = 0f;
		if (DetectRange != null) DetectRange.Value = 0f;
	}

	/// <summary>
	/// Đếm ngược thời gian bị mù, sau đó khôi phục lại trạng thái bình thường.
	/// </summary>
	private void HandleRecoveryPhase()
	{
		_timerRecover += Time.deltaTime;

		// Đợi hết thời gian mù
		if (_timerRecover >= BlindDuration.Value)
		{
			FinishRecovery();
		}
	}

	private void FinishRecovery()
	{
		// Debug.Log("✅ [Watchdog] Mutant recovered! Vision restored.");

		// 1. Tắt cờ kẹt
		if (IsStuck != null) IsStuck.Value = false;

		// 2. Trả lại thị lực gốc
		if (VisionDistance != null) VisionDistance.Value = NormalVisionDist.Value;
		if (DetectRange != null) DetectRange.Value = NormalDetectRange.Value;

		// 3. Reset lại watchdog để canh gác tiếp
		ResetWatchdog();
	}

	private void ResetWatchdog()
	{
		_isRecovering = false;
		_timerStuck = 0f;
		_timerRecover = 0f;
		if (Agent.Value != null) _anchorPosition = Agent.Value.transform.position;
	}

	#endregion
}