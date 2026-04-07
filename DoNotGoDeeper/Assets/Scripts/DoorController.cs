using UnityEngine;

/// <summary>
/// DoorController — A door that opens and closes via E-key interaction.
/// Plays a creak sound and emits a sound event so the Proctor investigates.
///
/// Implements IInteractable (defined in Interactor.cs) so it plugs into
/// the existing raycast interaction system automatically.
///
/// ─── INSPECTOR SETUP ─────────────────────────────────────────────────────────
///   - The door should be structured as:
///       DoorPivot (this script) ← empty GameObject at the hinge edge
///         └─ DoorMesh            ← the visible door model, child of pivot
///   - openAngle: how far the door swings (90 = standard door swing)
///   - openSpeed: lerp speed (2–4 feels natural)
///   - openDirection: set to -1 to swing the other way if needed
///   - soundIntensity: recommend 0.4 (creaky door, Proctor may hear from ~8 units)
///
/// ─── PROCTOR RESPONSE ────────────────────────────────────────────────────────
///   The creak emits to SoundEventManager at soundIntensity.
///   At intensity 0.4 and hearingThreshold 0.05, Proctor hears from ~8 units.
///   Players should use doors sparingly — each open/close risks detection.
/// </summary>
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Movement")]
    [Tooltip("Degrees the door rotates when opened (around Y axis of the pivot).")]
    [SerializeField] float openAngle = 90f;

    [Tooltip("Speed of the door swing lerp. 2–4 feels natural.")]
    [SerializeField] float openSpeed = 2.5f;

    [Tooltip("Set to -1 to reverse the swing direction.")]
    [SerializeField] float openDirection = 1f;

    [Header("Sound")]
    [Tooltip("Sound when the door opens.")]
    [SerializeField] AudioClip creakOpen;

    [Tooltip("Sound when the door closes.")]
    [SerializeField] AudioClip creakClose;

    [Tooltip("Intensity of the sound event emitted to SoundEventManager (0–1).")]
    [SerializeField] float soundIntensity = 0.4f;

    [Header("Interaction Hint")]
    [Tooltip("Optional: UI hint GameObject shown when the player looks at this door.")]
    [SerializeField] GameObject interactHint;

    private AudioSource _audioSource;
    private bool        _isOpen = false;
    private Quaternion  _closedRotation;
    private Quaternion  _openRotation;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _closedRotation = transform.localRotation;
        _openRotation   = Quaternion.Euler(
            transform.localEulerAngles + new Vector3(0f, openAngle * openDirection, 0f)
        );
    }

    void Update()
    {
        Quaternion target = _isOpen ? _openRotation : _closedRotation;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, target, Time.deltaTime * openSpeed
        );
    }

    /// <summary>
    /// Called by Interactor.cs when the player presses E while looking at this door.
    /// Toggles the door state, plays the appropriate creak, and emits a sound event.
    /// </summary>
    public void Interact()
    {
        _isOpen = !_isOpen;

        AudioClip clip = _isOpen ? creakOpen : creakClose;
        if (clip != null)
            _audioSource.PlayOneShot(clip);

        SoundEventManager.EmitSound(transform.position, soundIntensity);

        Debug.Log($"[DoorController] {gameObject.name} is now {(_isOpen ? "open" : "closed")}.");
    }
}
