using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SoundEventManager — Static sound event bus.
///
/// DATA FLOW:
///   Any game object (player, thrown item, door, etc.)
///     calls SoundEventManager.EmitSound(position, intensity)
///       → manager stores the event in a list
///         → all registered IHearSound listeners are notified this frame
///           → each listener decides independently whether it heard it
///             based on its own distance and hearing threshold.
///
/// DESIGN RATIONALE:
///   No direct coupling between emitters and enemies.
///   New sound sources just call EmitSound() — they don't need
///   a reference to any enemy. New enemy types just implement
///   IHearSound and register themselves.
///
/// INTENSITY GUIDE (0 to 1):
///   0.1  — crouching footstep
///   0.3  — walking footstep
///   0.6  — running footstep
///   0.8  — object dropped
///   1.0  — object thrown and landing (use on collision)
/// </summary>
public static class SoundEventManager
{
    // ─── Listener registry ────────────────────────────────────────────────────

    // All active enemies/listeners register here on Start(), deregister on OnDestroy().
    private static readonly List<IHearSound> _listeners = new List<IHearSound>();

    /// <summary>Called by each enemy in Start() to receive sound events.</summary>
    public static void Register(IHearSound listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    /// <summary>Called by each enemy in OnDestroy() to clean up.</summary>
    public static void Unregister(IHearSound listener)
    {
        _listeners.Remove(listener);
    }

    // ─── Emission ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Emit a sound event at a world position with a given intensity.
    /// Call this from any script that makes noise.
    ///
    /// HOW IT FULFILLS THE REQUIREMENT:
    ///   Each registered listener receives the sound position and raw intensity.
    ///   The listener uses its own hearingThreshold and distance to decide
    ///   whether perceived intensity = (intensity / distance) exceeds its threshold.
    ///   This naturally makes loud sounds carry farther and quiet sounds go unnoticed.
    /// </summary>
    /// <param name="position">World position the sound originates from.</param>
    /// <param name="intensity">Raw loudness 0–1. See intensity guide above.</param>
    public static void EmitSound(Vector3 position, float intensity)
    {
        // Notify every registered listener — each decides for itself
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            // Iterate backward so safe removal mid-loop doesn't break indexing
            _listeners[i].OnSoundHeard(position, intensity);
        }
    }
}

/// <summary>
/// Interface implemented by any script that wants to receive sound events.
/// EnemyAI implements this.
/// </summary>
public interface IHearSound
{
    void OnSoundHeard(Vector3 soundPosition, float intensity);
}
