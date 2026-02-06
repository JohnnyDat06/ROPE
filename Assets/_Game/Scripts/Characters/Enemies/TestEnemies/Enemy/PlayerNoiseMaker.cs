using UnityEngine;

public class PlayerNoiseMaker : MonoBehaviour
{
	#region Configuration

	[Header("Noise Configuration")]
	[Tooltip("The noise radius generated during a walking cycle.")]
	public float walkNoiseRange = 3f;

	[Tooltip("The noise radius generated during a running cycle.")]
	public float runNoiseRange = 6f;

	[Tooltip("The noise radius generated when jumping.")]
	public float jumpNoiseRange = 8f;

	[Tooltip("Default noise radius for special actions (open door, impact, etc.) if no override is provided.")]
	public float defaultSpecialNoiseRange = 30f;

	#endregion

	#region Animation Event Handlers

	/// <summary>
	/// Triggered by Animation Event at the exact frame the foot touches the ground during a Walk cycle.
	/// </summary>
	public void OnFootstepWalk()
	{
		GenerateNoise(walkNoiseRange, "Walk");
	}

	/// <summary>
	/// Triggered by Animation Event at the exact frame the foot touches the ground during a Run cycle.
	/// </summary>
	public void OnFootstepRun()
	{
		GenerateNoise(runNoiseRange, "Run");
	}

	/// <summary>
	/// Triggered by Animation Event during the Jump action (lift-off or landing).
	/// </summary>
	public void OnJumpNoise()
	{
		GenerateNoise(jumpNoiseRange, "Jump");
	}

	/// <summary>
	/// Triggered by Animation Event for miscellaneous actions (falling, shouting, object interaction).
	/// Allows overriding the range via the Float parameter in the Animation Event.
	/// </summary>
	/// <param name="rangeOverride">Custom noise radius. Pass 0 to use the default special range.</param>
	public void OnSpecialActionNoise(float rangeOverride)
	{
		float range = rangeOverride > 0 ? rangeOverride : defaultSpecialNoiseRange;
		GenerateNoise(range, "Special Action");
	}

	#endregion

	#region Core Logic

	/// <summary>
	/// Broadcasts the noise to the NoiseManager and handles debug visualization.
	/// </summary>
	/// <param name="range">The radius of the noise.</param>
	/// <param name="actionName">Name of the action for debugging purposes.</param>
	private void GenerateNoise(float range, string actionName)
	{
		// Notify the global NoiseManager
		NoiseManager.MakeNoise(transform.position, range);

#if UNITY_EDITOR
		// Visual debugging in Scene view
		Debug.DrawRay(transform.position, Vector3.up * 2, Color.yellow, 0.5f);
#endif

		// Debug.Log($"🔊 [NoiseMaker] Generated '{actionName}' noise with range: {range}");
	}

	#endregion
}