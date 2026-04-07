using System.Collections;
using UnityEngine;

/// <summary>
/// WhisperAmbience — Plays random whisper clips from shifting positions around the player.
///
/// Pure psychological horror — no game mechanic attached. The whispers have no source
/// and no meaning. They exist to destabilise the player and make the environment feel
/// inhabited by something wrong.
///
/// The AudioSource drifts to a random position near the player before each whisper plays,
/// giving a faint sense of directionality that the player will try (and fail) to locate.
///
/// NON-DIEGETIC: does not call SoundEventManager — the Proctor cannot hear these.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - Attach to the Player GameObject (or a child empty)
///   - Add an AudioSource (Play On Awake: off, Spatial Blend: 0.5 for subtle 3D, Volume: 0.3–0.5)
///   - Populate whisperClips with short, quiet whisper SFX
///   - Set spawnRadius: how far from the player the "source" drifts (recommend 3–6)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WhisperAmbience : MonoBehaviour
{
    [Tooltip("Pool of whisper clips to randomly select from.")]
    [SerializeField] AudioClip[] whisperClips;

    [Tooltip("Minimum seconds between whispers.")]
    [SerializeField] float minInterval = 20f;

    [Tooltip("Maximum seconds between whispers.")]
    [SerializeField] float maxInterval = 60f;

    [Tooltip("How far from the player the whisper 'source' is positioned before playing.")]
    [SerializeField] float spawnRadius = 4f;

    [Tooltip("If true, only whispers when the player has been stationary for stationaryThreshold seconds.")]
    [SerializeField] bool onlyWhenStill = false;

    [Tooltip("Seconds of stillness required before a whisper can trigger.")]
    [SerializeField] float stationaryThreshold = 3f;

    private AudioSource _audioSource;
    private float       _timer;
    private Vector3     _lastPosition;
    private float       _stillTimer;

    void Start()
    {
        _audioSource  = GetComponent<AudioSource>();
        _lastPosition = transform.position;
        ResetTimer();
    }

    void Update()
    {
        // Track stillness
        if (onlyWhenStill)
        {
            float moved = Vector3.Distance(transform.position, _lastPosition);
            if (moved > 0.1f)
            {
                _stillTimer   = 0f;
                _lastPosition = transform.position;
            }
            else
            {
                _stillTimer += Time.deltaTime;
            }
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            bool canPlay = !onlyWhenStill || _stillTimer >= stationaryThreshold;
            if (canPlay)
                StartCoroutine(PlayWhisper());
            else
                ResetTimer(); // Not still yet — try again after next interval
        }
    }

    IEnumerator PlayWhisper()
    {
        if (whisperClips == null || whisperClips.Length == 0)
        {
            ResetTimer();
            yield break;
        }

        // Drift the audio source to a random nearby position
        Vector3 offset = Random.insideUnitSphere * spawnRadius;
        offset.y = Mathf.Clamp(offset.y, -0.5f, 1.5f); // keep it at head height
        _audioSource.transform.localPosition = offset;

        AudioClip clip = whisperClips[Random.Range(0, whisperClips.Length)];
        if (clip != null)
            _audioSource.PlayOneShot(clip);

        yield return new WaitForSeconds(clip != null ? clip.length : 1f);

        // Reset source position
        _audioSource.transform.localPosition = Vector3.zero;
        ResetTimer();
    }

    void ResetTimer()
    {
        _timer = Random.Range(minInterval, maxInterval);
    }
}
