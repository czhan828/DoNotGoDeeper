using System.Collections;
using UnityEngine;

/// <summary>
/// FlickerLight — Configurable flickering for any scene light.
///
/// Three independent modes (mix and match via Inspector checkboxes):
///
///   AMBIENT FLICKER
///     Random sporadic flicker on a timer. Always-on subtle dread.
///     Tune minAmbientInterval / maxAmbientInterval for frequency.
///
///   PROCTOR-PROXIMITY FLICKER
///     Reads FearLevel from PlayerFearState. As the Proctor closes in,
///     flicker rate increases. At max fear, the light becomes unstable.
///     Requires PlayerFearState to exist in the scene.
///
///   SOUND-REACTIVE FLICKER
///     Subscribes to SoundEventManager.OnSoundEmitted. When a loud sound
///     occurs within soundReactRadius, the light briefly flickers.
///     Good for: a thrown object making nearby lights react.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - Attach to any GameObject that has a Light component
///   - Enable the modes you want with the three booleans
///   - For proximity flicker: PlayerFearState must be in the scene
///   - For sound-reactive: set soundReactRadius and soundReactThreshold
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    // ─── Ambient Flicker ──────────────────────────────────────────────────────

    [Header("Ambient Flicker")]
    [Tooltip("Enable random ambient flickering.")]
    [SerializeField] bool ambientFlicker = true;

    [Tooltip("Minimum seconds between ambient flicker events.")]
    [SerializeField] float minAmbientInterval = 8f;

    [Tooltip("Maximum seconds between ambient flicker events.")]
    [SerializeField] float maxAmbientInterval = 30f;

    [Tooltip("Number of rapid on/off cycles per flicker event.")]
    [SerializeField] int ambientFlickerCount = 2;

    [Tooltip("Duration the light is off during each cycle (seconds).")]
    [SerializeField] float ambientOffDuration = 0.05f;

    // ─── Proctor-Proximity Flicker ────────────────────────────────────────────

    [Header("Proctor-Proximity Flicker")]
    [Tooltip("Enable proximity-driven flicker (reads FearLevel from PlayerFearState).")]
    [SerializeField] bool proximityFlicker = true;

    [Tooltip("Fear level at which proximity flickering begins (0–1).")]
    [SerializeField] float proximityFlickerStartFear = 0.5f;

    [Tooltip("Minimum seconds between proximity flicker events at max fear.")]
    [SerializeField] float proximityIntervalMin = 0.5f;

    [Tooltip("Maximum seconds between proximity flicker events at fear = proximityFlickerStartFear.")]
    [SerializeField] float proximityIntervalMax = 5f;

    // ─── Sound-Reactive Flicker ───────────────────────────────────────────────

    [Header("Sound-Reactive Flicker")]
    [Tooltip("Enable flickering when a loud sound occurs nearby.")]
    [SerializeField] bool soundReactive = true;

    [Tooltip("Maximum distance from this light at which sounds trigger a flicker.")]
    [SerializeField] float soundReactRadius = 12f;

    [Tooltip("Minimum sound intensity required to trigger a flicker (0–1). Recommend 0.6+.")]
    [SerializeField] float soundReactThreshold = 0.65f;

    [Tooltip("Number of flicker cycles triggered by a loud sound.")]
    [SerializeField] int soundReactFlickerCount = 3;

    // ─── Private state ────────────────────────────────────────────────────────

    private Light  _light;
    private bool   _isFlickering = false;
    private float  _ambientTimer;
    private float  _proximityTimer;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _light = GetComponent<Light>();
    }

    void OnEnable()
    {
        if (soundReactive)
            SoundEventManager.OnSoundEmitted += OnSoundEmitted;

        ResetAmbientTimer();
        ResetProximityTimer(1f); // start at calm interval
    }

    void OnDisable()
    {
        SoundEventManager.OnSoundEmitted -= OnSoundEmitted;
        StopAllCoroutines();
        if (_light != null) _light.enabled = true; // restore on disable
    }

    void Update()
    {
        if (_isFlickering) return; // don't stack flicker coroutines

        if (ambientFlicker)
            TickAmbientFlicker();

        if (proximityFlicker)
            TickProximityFlicker();
    }

    // ─── Ambient Flicker ──────────────────────────────────────────────────────

    void TickAmbientFlicker()
    {
        _ambientTimer -= Time.deltaTime;
        if (_ambientTimer <= 0f)
        {
            StartCoroutine(DoFlicker(ambientFlickerCount, ambientOffDuration));
            ResetAmbientTimer();
        }
    }

    void ResetAmbientTimer()
    {
        _ambientTimer = Random.Range(minAmbientInterval, maxAmbientInterval);
    }

    // ─── Proximity Flicker ────────────────────────────────────────────────────

    void TickProximityFlicker()
    {
        if (PlayerFearState.instance == null) return;

        float fear = PlayerFearState.instance.FearLevel;
        if (fear < proximityFlickerStartFear) return;

        _proximityTimer -= Time.deltaTime;
        if (_proximityTimer <= 0f)
        {
            StartCoroutine(DoFlicker(Random.Range(1, 4), 0.04f));

            float t = Mathf.InverseLerp(proximityFlickerStartFear, 1f, fear);
            ResetProximityTimer(t);
        }
    }

    /// <param name="fearT">0 = calm (slow), 1 = max fear (fast).</param>
    void ResetProximityTimer(float fearT)
    {
        _proximityTimer = Mathf.Lerp(proximityIntervalMax, proximityIntervalMin, fearT);
    }

    // ─── Sound-Reactive Flicker ───────────────────────────────────────────────

    void OnSoundEmitted(Vector3 pos, float intensity)
    {
        if (!soundReactive) return;
        if (intensity < soundReactThreshold) return;
        if (Vector3.Distance(transform.position, pos) > soundReactRadius) return;
        if (_isFlickering) return;

        StartCoroutine(DoFlicker(soundReactFlickerCount, 0.03f));
    }

    // ─── Core Flicker Coroutine ───────────────────────────────────────────────

    /// <summary>
    /// Turns the light off and on <paramref name="count"/> times,
    /// with <paramref name="offDuration"/> seconds of darkness each cycle.
    /// </summary>
    IEnumerator DoFlicker(int count, float offDuration)
    {
        _isFlickering = true;

        for (int i = 0; i < count; i++)
        {
            _light.enabled = false;
            yield return new WaitForSeconds(offDuration);
            _light.enabled = true;
            yield return new WaitForSeconds(offDuration * 0.5f);
        }

        _isFlickering = false;
    }
}
