using UnityEngine;

/// <summary>
/// NoiseObject — Furniture or prop that emits a sound event when the player collides with it.
///
/// The emitted sound is picked up by EnemyAI via SoundEventManager, causing the Proctor
/// to investigate the collision point. This punishes careless movement and rewards
/// players who navigate carefully around obstacles.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - Add a non-trigger Collider to the GameObject (blocks movement physically)
///   - Add an AudioSource component (Play On Awake: off)
///   - Attach this script
///   - Assign noiseSFX (scraping chair, falling book, clattering object, etc.)
///   - Tune noiseIntensity: 0.6 = moderate, 0.9 = very loud
///   - Set oneTimeOnly = true for objects that only make noise once (a tipped chair)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class NoiseObject : MonoBehaviour
{
    [Tooltip("Sound that plays when the player bumps into this object.")]
    [SerializeField] AudioClip noiseSFX;

    [Tooltip(
        "Intensity of the emitted sound event (0–1). Higher = Proctor hears from farther away.\n" +
        "0.5 = nearby furniture scrape\n" +
        "0.8 = knocked-over chair\n" +
        "1.0 = crashing bookshelf")]
    [SerializeField] float noiseIntensity = 0.7f;

    [Tooltip("If true, only makes noise on the first collision. Good for tipped chairs, etc.")]
    [SerializeField] bool oneTimeOnly = true;

    [Tooltip("Optional: minimum seconds between repeated noise triggers. Ignored if oneTimeOnly is true.")]
    [SerializeField] float repeatCooldown = 3f;

    private AudioSource _audioSource;
    private bool        _spent = false;
    private float       _cooldownTimer = 0f;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (oneTimeOnly && _spent) return;
        if (_cooldownTimer > 0f) return;

        MakeNoise();
    }

    void MakeNoise()
    {
        if (noiseSFX != null)
            _audioSource.PlayOneShot(noiseSFX);

        SoundEventManager.EmitSound(transform.position, noiseIntensity);

        if (oneTimeOnly)
        {
            _spent = true;
        }
        else
        {
            _cooldownTimer = repeatCooldown;
        }

        Debug.Log($"[NoiseObject] {gameObject.name} bumped — emitting sound at intensity {noiseIntensity}");
    }

    void OnDrawGizmosSelected()
    {
        // Visualise how far the Proctor can hear this object
        // At intensity I, heard within I / hearingThreshold units.
        // Default threshold is 0.05, so intensity 0.7 → heard within 14 units.
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, noiseIntensity / 0.05f);
    }
}
