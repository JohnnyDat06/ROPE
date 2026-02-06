using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Hearing Sensor",
				 story: "[Self] listens for noise and updates [LastKnownPos]",
				 category: "MutantAI",
				 id: "HearingSensorAction")]
/// <summary>
/// A continuous Action node that acts as the AI's "Ears".
/// It subscribes to the NoiseManager events to detect sounds within range, 
/// filters them based on significance, and updates the AI's target location.
/// </summary>
public partial class HearingSensorAction : Action
{
	#region Input / Output Variables

	[SerializeReference]
	public BlackboardVariable<GameObject> Self;

	[SerializeReference]
	public BlackboardVariable<Vector3> LastKnownPos;

	[SerializeReference]
	public BlackboardVariable<bool> HasInvestigateTarget;

	[Tooltip("Trigger flag to signal the Behavior Graph that a significant new noise was detected (useful for restarting branches).")]
	[SerializeReference]
	public BlackboardVariable<bool> IsNewNoiseDetected;

	#endregion

	#region Configuration

	[Header("Sensor Settings")]
	[Tooltip("The base hearing radius of the Agent.")]
	[SerializeReference]
	public BlackboardVariable<float> HearingRange = new BlackboardVariable<float>(20f);

	[Tooltip("Minimum distance required between the old target and the new noise to register an update. Prevents the AI from stuttering/freezing due to minor noise position changes.")]
	[SerializeReference]
	public BlackboardVariable<float> UpdateThreshold = new BlackboardVariable<float>(3.0f);

	#endregion

	#region Internal State

	private bool _noiseHeardThisFrame = false;
	private Vector3 _noisePosition;

	#endregion

	#region Lifecycle Methods

	protected override Status OnStart()
	{
		// Subscribe to the global noise event
		NoiseManager.OnNoiseGenerated += OnNoiseHeard;
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		// Process noise detection on the main thread
		if (_noiseHeardThisFrame)
		{
			if (LastKnownPos != null)
			{
				// --- Anti-Freeze / Jitter Filter Logic ---

				// Calculate distance between the current target and the new noise
				float distFromOldTarget = Vector3.Distance(LastKnownPos.Value, _noisePosition);

				// Condition 1: Is the new sound significantly far from the old one?
				bool isNewLocationSignificant = distFromOldTarget > UpdateThreshold.Value;

				// Condition 2: Is this the very first target we've found?
				bool isFirstTarget = HasInvestigateTarget != null && HasInvestigateTarget.Value == false;

				if (isNewLocationSignificant || isFirstTarget)
				{
					// Update the target position
					LastKnownPos.Value = _noisePosition;

					// Mark that we have a valid target to investigate
					if (HasInvestigateTarget != null) HasInvestigateTarget.Value = true;

					// Trigger the "Restart" signal for the behavior tree
					if (IsNewNoiseDetected != null) IsNewNoiseDetected.Value = true;
				}
			}

			// Reset the flag for the next frame
			_noiseHeardThisFrame = false;
		}

		// Keep running to listen continuously
		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Always unsubscribe to prevent memory leaks
		NoiseManager.OnNoiseGenerated -= OnNoiseHeard;
	}

	#endregion

	#region Event Callbacks

	/// <summary>
	/// Callback triggered by NoiseManager.
	/// Calculates if the sound is audible based on distance and the sound's loudness.
	/// </summary>
	/// <param name="pos">World position of the noise.</param>
	/// <param name="range">Loudness/Radius of the noise.</param>
	private void OnNoiseHeard(Vector3 pos, float range)
	{
		if (Self.Value == null) return;

		// Check distance: Distance <= Agent's Hearing Ability + Sound's Loudness
		float dist = Vector3.Distance(Self.Value.transform.position, pos);

		if (dist <= HearingRange.Value + range)
		{
			// Cache the data to be processed in OnUpdate (Main Thread)
			_noiseHeardThisFrame = true;
			_noisePosition = pos;
		}
	}

	#endregion
}