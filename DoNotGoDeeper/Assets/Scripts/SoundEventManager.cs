using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SoundEventManager — Static sound event bus.
///
/// DATA FLOW:
///   Any game object (player, thrown item, door, etc.)
///     calls SoundEventManager.EmitSound(position, intensity)
///       → all registered IHearSound listeners are notified immediately
///         → each listener decides independently whether it heard it
///           based on its own distance and hearing threshold.
///
/// INTENSITY GUIDE (0 to 1):
///   0.1  — crouching footstep
///   0.3  — walking footstep
///   0.6  — running footstep
///   0.8  — object dropped
///   1.0  — object thrown / landing impact
///
/// ─── FIX: STATIC LIST PERSISTENCE ───────────────────────────────────────────
///   The static _listeners list persists across editor Play sessions.
///   Enemies re-register each Start(), but if the list isn't cleared between
///   sessions, you can get duplicate registrations or stale dead references.
///   RuntimeInitializeOnLoadMethod ensures the list is wiped each Play.
/// </summary>
public static class SoundEventManager
{
    private static readonly List<IHearSound> _listeners = new List<IHearSound>();

    // FIX: clear the static list at the start of every Play session
    // so stale references from the previous session don't cause NullReferenceExceptions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        _listeners.Clear();
    }

    /// <summary>Called by each enemy in Start() to receive sound events.</summary>
    public static void Register(IHearSound listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
            Debug.Log($"[SoundEventManager] Registered listener. Total: {_listeners.Count}");
        }
    }

    /// <summary>Called by each enemy in OnDestroy() to clean up.</summary>
    public static void Unregister(IHearSound listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// Emit a sound event at a world position with a given intensity.
    /// Call this from any script that makes noise.
    /// </summary>
    /// <param name="position">World position the sound originates from.</param>
    /// <param name="intensity">Raw loudness 0–1. See intensity guide above.</param>
    public static void EmitSound(Vector3 position, float intensity)
    {
        Debug.Log($"[SoundEventManager] EmitSound pos={position} intensity={intensity:F2} listeners={_listeners.Count}");

        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnSoundHeard(position, intensity);
        }
    }
}

/// <summary>
/// Interface implemented by any script that wants to receive sound events.
/// </summary>
public interface IHearSound
{
    void OnSoundHeard(Vector3 soundPosition, float intensity);
}