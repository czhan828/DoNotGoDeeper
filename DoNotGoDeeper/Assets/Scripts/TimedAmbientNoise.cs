using UnityEngine;

/// <summary>
/// TimedAmbientNoise — Plays a sound on a randomised timer to create ambient dread.
///
/// Creates the feeling that the building is alive — objects falling, pipes rattling,
/// lockers banging in the dark. The player cannot tell if sounds are scripted or
/// the Proctor, which builds paranoia.
///
/// If emitToSoundSystem is true, the Proctor will investigate the source of the sound.
/// Use this sparingly — it can feel unfair if the Proctor always chases scripted noises.
/// Better for "atmosphere only" sounds to leave emitToSoundSystem off.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - Attach to any GameObject in the scene (empty, or the noisy prop itself)
///   - Add an AudioSource (Play On Awake: off, Spatial Blend: 1 for 3D)
///   - Assign at least one AudioClip in soundClips
///   - Set fireOnStart = false for sounds that should wait for the timer first
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TimedAmbientNoise : MonoBehaviour
{
    [Tooltip("Pool of clips to randomly select from each trigger.")]
    [SerializeField] AudioClip[] soundClips;

    [Tooltip("Minimum seconds between triggers.")]
    [SerializeField] float minInterval = 15f;

    [Tooltip("Maximum seconds between triggers.")]
    [SerializeField] float maxInterval = 45f;

    [Tooltip("If true, this sound is emitted to the SoundEventManager and the Proctor will investigate it.")]
    [SerializeField] bool emitToSoundSystem = false;

    [Tooltip("Sound intensity if emitToSoundSystem is true (0–1).")]
    [SerializeField] float emitIntensity = 0.6f;

    [Tooltip("If true, plays a sound immediately when the scene starts.")]
    [SerializeField] bool fireOnStart = false;

    [Tooltip("If true, only fires once and then stops.")]
    [SerializeField] bool oneTimeOnly = false;

    private AudioSource _audioSource;
    private float       _timer;
    private bool        _done = false;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        if (fireOnStart)
            Trigger();
        else
            ResetTimer();
    }

    void Update()
    {
        if (_done) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            Trigger();
    }

    void Trigger()
    {
        if (soundClips == null || soundClips.Length == 0) return;

        AudioClip clip = soundClips[Random.Range(0, soundClips.Length)];
        if (clip != null)
            _audioSource.PlayOneShot(clip);

        if (emitToSoundSystem)
            SoundEventManager.EmitSound(transform.position, emitIntensity);

        if (oneTimeOnly)
        {
            _done = true;
        }
        else
        {
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        _timer = Random.Range(minInterval, maxInterval);
    }
}
