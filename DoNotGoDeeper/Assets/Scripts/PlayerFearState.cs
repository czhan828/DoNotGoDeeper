using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// PlayerFearState — Central hub for all player-side horror feedback.
///
/// Tracks two values every frame:
///   FearLevel  (0–1): how close the Proctor is. Drives breathing, heartbeat, camera tremor, vignette.
///   NoiseLevel (0–1): how loud the player's recent actions were. Drives the caution meter.
///
/// Both are exposed as public properties so FlickerLight and other scripts can read them.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   REQUIRED:
///     proctorTransform     — drag the Proctor (EnemyAI) GameObject
///     cameraPivot          — drag the cameraPivot child of the player (same ref as PlayerMovement)
///
///   SOUND (add AudioSource components to the player, assign here):
///     breathingAudioSource — looping breathing clip, Spatial Blend 0 (2D)
///     heartbeatAudioSource — single heartbeat thud, Spatial Blend 0 (2D)
///     breathingClip        — slow-to-fast breathing loop
///     heartbeatClip        — single heartbeat thud SFX
///
///   UI:
///     cautionMeterFill     — the Image component of the caution bar (Image Type: Filled)
///     cautionMeterColor    — gradient from green (safe) to red (danger), set via inspector colours
///
///   POST-PROCESSING:
///     postProcessVolume    — the Global Volume in your scene (needs Vignette override added)
///
/// ─── POST-PROCESSING SETUP ───────────────────────────────────────────────────
///   1. In Unity: Window → Package Manager → install "Universal RP" or ensure URP is active
///   2. In Hierarchy: right-click → Volume → Global Volume
///   3. In the Volume's Profile, click Add Override → Post-processing → Vignette
///   4. Drag that Volume GameObject into the postProcessVolume field here
/// </summary>
public class PlayerFearState : MonoBehaviour
{
    public static PlayerFearState instance;

    // ─── Inspector: References ────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Drag the Proctor (EnemyAI) GameObject here.")]
    [SerializeField] Transform proctorTransform;

    [Tooltip("The cameraPivot child of the player — same object assigned in PlayerMovement.")]
    [SerializeField] Transform cameraPivot;

    // ─── Inspector: Fear (Proctor Proximity) ─────────────────────────────────

    [Header("Fear — Proctor Proximity")]
    [Tooltip("Distance at which fear begins (fully calm beyond this).")]
    [SerializeField] float fearStartDistance = 20f;

    [Tooltip("Distance at which fear is maxed (fully terrified at this range).")]
    [SerializeField] float fearMaxDistance = 4f;

    // ─── Inspector: Noise / Caution Meter ────────────────────────────────────

    [Header("Noise — Caution Meter")]
    [Tooltip("Image component of the caution bar fill. Set Image Type to Filled in Inspector.")]
    [SerializeField] Image cautionMeterFill;

    [Tooltip("Colour of the bar when noise is low (safe).")]
    [SerializeField] Color cautionColorSafe = Color.green;

    [Tooltip("Colour of the bar when noise is high (danger).")]
    [SerializeField] Color cautionColorDanger = Color.red;

    [Tooltip("How quickly the noise level decays back to zero after the player stops moving.")]
    [SerializeField] float noiseDecayRate = 0.8f;

    [Tooltip("How long the noise level holds at its peak before decaying (seconds).")]
    [SerializeField] float noiseHoldDuration = 0.6f;

    [Tooltip("How much the caution bar shakes when noise is high. 0 = no shake.")]
    [SerializeField] float cautionShakeIntensity = 5f;

    // ─── Inspector: Breathing ─────────────────────────────────────────────────

    [Header("Breathing")]
    [SerializeField] AudioSource breathingAudioSource;
    [SerializeField] AudioClip   breathingClip;

    [Tooltip("Volume when calm (fear = 0).")]
    [SerializeField] float breathVolumeCalm = 0.1f;

    [Tooltip("Volume when terrified (fear = 1).")]
    [SerializeField] float breathVolumeMax  = 0.8f;

    [Tooltip("Pitch when calm.")]
    [SerializeField] float breathPitchCalm  = 0.9f;

    [Tooltip("Pitch when terrified (faster breathing).")]
    [SerializeField] float breathPitchMax   = 1.4f;

    // ─── Inspector: Heartbeat ─────────────────────────────────────────────────

    [Header("Heartbeat")]
    [SerializeField] AudioSource heartbeatAudioSource;
    [SerializeField] AudioClip   heartbeatClip;

    [Tooltip("Fear level at which the heartbeat starts (0–1).")]
    [SerializeField] float heartbeatStartFear = 0.4f;

    [Tooltip("Seconds between heartbeat thuds when at max fear.")]
    [SerializeField] float heartbeatIntervalMin = 0.4f;

    [Tooltip("Seconds between heartbeat thuds when fear is at heartbeatStartFear.")]
    [SerializeField] float heartbeatIntervalMax = 1.5f;

    // ─── Inspector: Camera Tremor ─────────────────────────────────────────────

    [Header("Camera Tremor")]
    [Tooltip("Maximum rotation offset (degrees) applied via Perlin noise at full fear.")]
    [SerializeField] float tremorMaxAngle = 0.4f;

    [Tooltip("Speed of the Perlin noise scroll. Higher = faster tremor.")]
    [SerializeField] float tremorSpeed    = 3f;

    // ─── Inspector: Vignette ──────────────────────────────────────────────────

    [Header("Post-Processing Vignette")]
    [Tooltip("The Global Volume GameObject in your scene. Needs a Vignette override.")]
    [SerializeField] Volume postProcessVolume;

    [Tooltip("Vignette intensity when calm (fear = 0).")]
    [SerializeField] float vignetteMin = 0.2f;

    [Tooltip("Vignette intensity when terrified (fear = 1).")]
    [SerializeField] float vignetteMax = 0.65f;

    // ─── Public properties (read by FlickerLight, etc.) ──────────────────────

    /// <summary>0 = calm, 1 = Proctor is right next to you.</summary>
    public float FearLevel  { get; private set; }

    /// <summary>0 = silent, 1 = running/throwing. Drives the caution meter.</summary>
    public float NoiseLevel { get; private set; }

    // ─── Private state ────────────────────────────────────────────────────────

    private Vignette _vignette;
    private float    _noiseDecayTimer;
    private float    _heartbeatTimer;
    private float    _tremorSeedX;
    private float    _tremorSeedY;
    private Vector3  _cautionMeterBasePos;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        SoundEventManager.OnSoundEmitted += HandleSoundEmitted;
    }

    void OnDisable()
    {
        SoundEventManager.OnSoundEmitted -= HandleSoundEmitted;
    }

    void Start()
    {
        // Cache initial caution meter position for shake effect
        if (cautionMeterFill != null)
            _cautionMeterBasePos = cautionMeterFill.rectTransform.anchoredPosition;

        // Set up breathing
        if (breathingAudioSource != null && breathingClip != null)
        {
            breathingAudioSource.clip   = breathingClip;
            breathingAudioSource.loop   = true;
            breathingAudioSource.volume = breathVolumeCalm;
            breathingAudioSource.pitch  = breathPitchCalm;
            breathingAudioSource.Play();
        }

        // Try to get Vignette from post-process volume
        if (postProcessVolume != null)
            postProcessVolume.profile.TryGet(out _vignette);

        // Randomise Perlin seeds so two players don't tremble identically
        _tremorSeedX = Random.Range(0f, 100f);
        _tremorSeedY = Random.Range(0f, 100f);
    }

    void Update()
    {
        UpdateFearLevel();
        UpdateNoiseLevel();
        UpdateBreathing();
        UpdateHeartbeat();
        UpdateCameraTremor();
        UpdateVignette();
        UpdateCautionMeter();
    }

    // ─── Fear level ───────────────────────────────────────────────────────────

    void UpdateFearLevel()
    {
        if (proctorTransform == null) { FearLevel = 0f; return; }

        float dist = Vector3.Distance(transform.position, proctorTransform.position);
        // InverseLerp: 0 when dist >= fearStartDistance, 1 when dist <= fearMaxDistance
        FearLevel = Mathf.Clamp01(
            Mathf.InverseLerp(fearStartDistance, fearMaxDistance, dist)
        );
    }

    // ─── Noise level ──────────────────────────────────────────────────────────

    void HandleSoundEmitted(Vector3 pos, float intensity)
    {
        // Only react to sounds that originate from the player (within 2 units)
        if (Vector3.Distance(transform.position, pos) > 2f) return;

        // Peak-hold: take the loudest recent value
        NoiseLevel = Mathf.Max(NoiseLevel, intensity);
        _noiseDecayTimer = noiseHoldDuration;
    }

    void UpdateNoiseLevel()
    {
        if (_noiseDecayTimer > 0f)
        {
            _noiseDecayTimer -= Time.deltaTime;
        }
        else
        {
            NoiseLevel = Mathf.MoveTowards(NoiseLevel, 0f, Time.deltaTime * noiseDecayRate);
        }
    }

    // ─── Breathing ───────────────────────────────────────────────────────────

    void UpdateBreathing()
    {
        if (breathingAudioSource == null) return;

        breathingAudioSource.volume = Mathf.Lerp(breathVolumeCalm, breathVolumeMax, FearLevel);
        breathingAudioSource.pitch  = Mathf.Lerp(breathPitchCalm,  breathPitchMax,  FearLevel);
    }

    // ─── Heartbeat ───────────────────────────────────────────────────────────

    void UpdateHeartbeat()
    {
        if (heartbeatAudioSource == null || heartbeatClip == null) return;
        if (FearLevel < heartbeatStartFear) { _heartbeatTimer = heartbeatIntervalMax; return; }

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer <= 0f)
        {
            float t        = Mathf.InverseLerp(heartbeatStartFear, 1f, FearLevel);
            float interval = Mathf.Lerp(heartbeatIntervalMax, heartbeatIntervalMin, t);
            float volume   = Mathf.Lerp(0.2f, 0.9f, t);

            heartbeatAudioSource.PlayOneShot(heartbeatClip, volume);
            _heartbeatTimer = interval;
        }
    }

    // ─── Camera tremor ───────────────────────────────────────────────────────

    void UpdateCameraTremor()
    {
        if (cameraPivot == null || FearLevel < 0.3f) return;

        float t      = Mathf.InverseLerp(0.3f, 1f, FearLevel);
        float angle  = tremorMaxAngle * t;
        float time   = Time.time * tremorSpeed;

        // Perlin noise offset — centred at 0 by subtracting 0.5
        float offsetX = (Mathf.PerlinNoise(time,              _tremorSeedX) - 0.5f) * 2f * angle;
        float offsetY = (Mathf.PerlinNoise(_tremorSeedY, time             ) - 0.5f) * 2f * angle;

        cameraPivot.localRotation = Quaternion.Euler(
            cameraPivot.localEulerAngles.x + offsetX,
            offsetY,
            0f
        );
    }

    // ─── Vignette ─────────────────────────────────────────────────────────────

    void UpdateVignette()
    {
        if (_vignette == null) return;
        _vignette.intensity.value = Mathf.Lerp(vignetteMin, vignetteMax, FearLevel);
    }

    // ─── Caution meter ───────────────────────────────────────────────────────

    void UpdateCautionMeter()
    {
        if (cautionMeterFill == null) return;

        cautionMeterFill.fillAmount = NoiseLevel;
        cautionMeterFill.color      = Color.Lerp(cautionColorSafe, cautionColorDanger, NoiseLevel);

        // Shake the bar when noise is dangerously high
        if (NoiseLevel > 0.55f && cautionShakeIntensity > 0f)
        {
            Vector2 shake = Random.insideUnitCircle * cautionShakeIntensity * (NoiseLevel - 0.55f);
            cautionMeterFill.rectTransform.anchoredPosition = _cautionMeterBasePos + (Vector3)shake;
        }
        else
        {
            cautionMeterFill.rectTransform.anchoredPosition = _cautionMeterBasePos;
        }
    }
}
