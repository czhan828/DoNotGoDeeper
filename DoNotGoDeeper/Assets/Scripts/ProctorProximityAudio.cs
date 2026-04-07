using UnityEngine;

/// <summary>
/// ProctorProximityAudio — Non-diegetic tension drone that intensifies as the Proctor closes in.
///
/// Attach to the player. As the Proctor gets closer, the drone volume and pitch rise,
/// creating a subconscious sense of dread. The player won't notice it consciously at
/// first — they'll just feel more tense.
///
/// NON-DIEGETIC: this sound is only heard by the real player, not the in-game Proctor.
/// Do NOT call SoundEventManager.EmitSound from this script.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - Attach to the Player GameObject
///   - Add an AudioSource component (Loop: on, Play On Awake: off, Spatial Blend: 0 for 2D)
///   - Assign droneClip (a low ambient hum, rumble, or tonal drone)
///   - Drag the Proctor (EnemyAI GameObject) into proctorTransform
///   - Set maxHearDistance (how close before drone starts — recommend 25–35 units)
///   - Set minVolume (0 = silent at range) and maxVolume (recommend 0.5–0.8)
///   - Set minPitch and maxPitch (recommend 0.8 and 1.2)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProctorProximityAudio : MonoBehaviour
{
    [Tooltip("Ambient tension drone clip. Should loop seamlessly.")]
    [SerializeField] AudioClip droneClip;

    [Tooltip("Drag the Proctor (EnemyAI) GameObject here.")]
    [SerializeField] Transform proctorTransform;

    [Tooltip("Distance at which the drone begins (fully silent beyond this).")]
    [SerializeField] float maxHearDistance = 30f;

    [Tooltip("Distance at which the drone reaches full volume and pitch.")]
    [SerializeField] float minHearDistance = 4f;

    [Tooltip("Volume at maximum distance (outer edge of range).")]
    [SerializeField] float minVolume = 0f;

    [Tooltip("Volume when Proctor is at minimum distance (very close).")]
    [SerializeField] float maxVolume = 0.75f;

    [Tooltip("Pitch at maximum distance.")]
    [SerializeField] float minPitch = 0.85f;

    [Tooltip("Pitch when Proctor is at minimum distance.")]
    [SerializeField] float maxPitch = 1.15f;

    [Tooltip("How fast the drone volume/pitch responds to distance changes.")]
    [SerializeField] float smoothSpeed = 2f;

    private AudioSource _audioSource;
    private float       _targetVolume;
    private float       _targetPitch;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip   = droneClip;
        _audioSource.loop   = true;
        _audioSource.volume = 0f;
        _audioSource.pitch  = minPitch;

        if (droneClip != null)
            _audioSource.Play();
    }

    void Update()
    {
        if (proctorTransform == null) return;

        float dist = Vector3.Distance(transform.position, proctorTransform.position);

        // Normalize distance: 0 = at minHearDistance (close), 1 = at maxHearDistance (far)
        float t = Mathf.InverseLerp(minHearDistance, maxHearDistance, dist);
        // t = 0 → close → full volume/pitch; t = 1 → far → quiet/low

        _targetVolume = Mathf.Lerp(maxVolume, minVolume, t);
        _targetPitch  = Mathf.Lerp(maxPitch,  minPitch,  t);

        _audioSource.volume = Mathf.Lerp(_audioSource.volume, _targetVolume, Time.deltaTime * smoothSpeed);
        _audioSource.pitch  = Mathf.Lerp(_audioSource.pitch,  _targetPitch,  Time.deltaTime * smoothSpeed);
    }
}
