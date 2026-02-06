using UnityEngine;
using System;

/// <summary>
/// A static manager responsible for broadcasting noise events within the game world.
/// Acts as a central hub for the Hearing/Stealth system using the Observer pattern.
/// </summary>
public static class NoiseManager
{
	#region Events

	/// <summary>
	/// Event triggered whenever a noise is generated.
	/// Listeners (e.g., AI Agents) should subscribe to this to detect sounds.
	/// <br/> Payload: <b>Position</b> (Vector3), <b>Range/Loudness</b> (float).
	/// </summary>
	public static event Action<Vector3, float> OnNoiseGenerated;

	#endregion

	#region Public Methods

	/// <summary>
	/// Broadcasts a noise event to all active listeners.
	/// Call this method when an action produces sound (e.g., Footsteps, Gunshots, Object Impacts).
	/// </summary>
	/// <param name="position">The world position where the noise originated.</param>
	/// <param name="range">The audible radius or intensity of the noise. AI within this range may detect it.</param>
	public static void MakeNoise(Vector3 position, float range)
	{
		// Notify all subscribers (AI agents) that a noise has occurred
		// Using ?.Invoke to prevent errors if there are no listeners
		OnNoiseGenerated?.Invoke(position, range);

#if UNITY_EDITOR
		// Visual debugging: Draws a vertical yellow ray at the noise source to visualize the event in the Scene view.
		Debug.DrawRay(position, Vector3.up * 5, Color.yellow, 2f);
#endif
	}

	#endregion
}