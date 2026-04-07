using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// EnemyAI — Handles two states: Patrol and Investigate.
///
/// ─── STATE MACHINE OVERVIEW ──────────────────────────────────────────────────
///
///   ┌─────────┐   sound heard within     ┌─────────────┐
///   │  PATROL │ ────── threshold ──────▶  │ INVESTIGATE │
///   └─────────┘                           └─────────────┘
///        ▲                                      │
///        └──────── timer expires, no catch ─────┘
///
/// PATROL:
///   Picks a valid random NavMesh point within patrolRange.
///   Walks there. When arrived, picks a new point. Loops forever.
///
/// INVESTIGATE:
///   Walks to the sound position.
///   Waits investigateDuration seconds (default 7).
///   Returns to PATROL from current spot.
///   If another sound fires during investigation, updates destination
///   to the new sound position.
///
/// ─── SOUND DETECTION ─────────────────────────────────────────────────────────
///
///   Implements IHearSound — registered with SoundEventManager.
///   When OnSoundHeard() fires:
///     perceivedIntensity = rawIntensity / distanceToSound
///     if perceivedIntensity >= hearingThreshold → switch to INVESTIGATE
///
/// ─── FIXES APPLIED ───────────────────────────────────────────────────────────
///   ✔ hearingThreshold lowered to 0.05 (was 0.3 — effectively deaf)
///   ✔ Per-frame Debug.Log in PatrolState removed (was spamming console)
///   ✔ Missing </summary> closing tag on PatrolState fixed
///   ✔ patrolArrivalThreshold raised to 1.5 for more reliable arrival detection
///   ✔ Added fallback: if path becomes invalid, clear dest and repick
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IHearSound
{
    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Patrol Settings")]
    [Tooltip("How far from current position to pick a random patrol point.")]
    [SerializeField] float patrolRange = 25f;

    [Tooltip("NavMesh sample radius when finding a valid patrol point.")]
    [SerializeField] float navSampleRadius = 18f;

    [Tooltip("How close the enemy must get before considering a patrol point reached.")]
    [SerializeField] float patrolArrivalThreshold = 1.5f;

    [Header("Investigation Settings")]
    [Tooltip("How many seconds to wait at a sound origin before resuming patrol.")]
    [SerializeField] float investigateDuration = 7f;

    [Header("Hearing Settings")]
    [Tooltip(
        "Minimum perceived intensity required to trigger investigation.\n" +
        "perceivedIntensity = rawIntensity / distance.\n" +
        "Lower = more sensitive.\n\n" +
        "At 0.05: walks heard ~6 units away, runs ~12 units away.\n" +
        "At 0.10: walks heard ~3 units away, runs ~6 units away.\n" +
        "Recommended: 0.05 for easy detection, 0.10 for tighter range.")]
    [SerializeField] float hearingThreshold = 0.05f;   // FIX: was 0.3 — almost completely deaf

    [Header("Speed")]
    [SerializeField] float patrolSpeed = 10f;
    [SerializeField] float investigateSpeed = 15f;

    [Header("Catch Settings")]
    [Tooltip("The player Transform. Drag the player GameObject here in the Inspector.")]
    [SerializeField] Transform player;

    [Tooltip("How close the player must be (in metres) for the enemy to catch them.")]
    [SerializeField] float catchRadius = 2f;

    // ─── Private state ────────────────────────────────────────────────────────

    private NavMeshAgent _agent;

    private enum AIState { Patrol, Investigate }
    private AIState _state = AIState.Patrol;

    // Patrol
    private Vector3 _patrolDest;
    private bool    _patrolDestSet;

    // Investigate
    private Vector3 _soundOrigin;
    private float   _investigateTimer;
    private bool    _arrivedAtSound;
    private bool    _playerCaught;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        SoundEventManager.Register(this);
    }

    void OnDestroy()
    {
        SoundEventManager.Unregister(this);
    }

    void Update()
    {
        switch (_state)
        {
            case AIState.Patrol:      PatrolState();      break;
            case AIState.Investigate: InvestigateState(); break;
        }
    }

    // ─── State: Patrol ────────────────────────────────────────────────────────

    /// <summary>
    /// PATROL STATE — called every frame while _state == Patrol.
    ///
    /// Flow:
    ///   1. If no destination is set, call FindPatrolPoint() to pick one.
    ///   2. Once a valid destination exists, give it to the NavMeshAgent.
    ///   3. When the agent is close enough, clear the destination so a new
    ///      one is picked next frame. This creates a continuous wander loop.
    ///   4. If the path becomes invalid (e.g. agent clipped through geometry),
    ///      reset and repick to avoid getting stuck forever.
    /// </summary>
    void PatrolState()
    {
        _agent.speed = patrolSpeed;
        if (_playerCaught) return;  // Don't move if player is already caught

        AttemptCatchPlayer();
        if (!_patrolDestSet)
            FindPatrolPoint();

        if (_patrolDestSet)
        {
            _agent.SetDestination(_patrolDest);

            // FIX: guard against invalid/incomplete paths causing infinite stuck loops
            if (!_agent.pathPending)
            {
                if (_agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    // Path couldn't be resolved — pick a new point
                    _patrolDestSet = false;
                    return;
                }

                if (_agent.remainingDistance < patrolArrivalThreshold)
                {
                    // Arrived — queue up a new destination next frame
                    _patrolDestSet = false;
                }
            }
        }
    }

    /// <summary>
    /// check if Player is within killing radius of the Proctor
    /// </summary>
    void AttemptCatchPlayer()
    {
        if (player != null)
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= catchRadius)
        {
            TriggerCaught();
            return;
        }
    }
        
    }



    /// <summary>
    /// Picks a valid random NavMesh point within patrolRange of current position.
    ///
    /// Flow:
    ///   1. Generate a random offset within patrolRange.
    ///   2. Use NavMesh.SamplePosition() to snap it to the nearest NavMesh surface
    ///      within navSampleRadius.
    ///   3. Only set _patrolDestSet = true if SamplePosition finds a valid hit.
    ///      If it doesn't, we try again next frame — no freeze, just retry.
    /// </summary>
    void FindPatrolPoint()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-patrolRange, patrolRange),
            0f,
            Random.Range(-patrolRange, patrolRange)
        );

        Vector3 candidate = transform.position + randomOffset;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
        {
            _patrolDest    = hit.position;
            _patrolDestSet = true;
            Debug.Log("[EnemyAI] New patrol destination: " + _patrolDest);
        }
    }

    // ─── State: Investigate ───────────────────────────────────────────────────

    /// <summary>
    /// INVESTIGATE STATE — called every frame while _state == Investigate.
    ///
    /// Flow:
    ///   1. Check catch radius — if player is within catchRadius, trigger caught.
    ///   2. Move toward _soundOrigin at investigate speed.
    ///   3. Once arrived, start counting down investigateTimer.
    ///   4. When timer expires → return to Patrol from current position.
    /// </summary>
    void InvestigateState()
    {
        AttemptCatchPlayer();
        if (_playerCaught) return;

        _agent.speed = investigateSpeed;

        if (!_arrivedAtSound)
        {
            _agent.SetDestination(_soundOrigin);

            if (!_agent.pathPending && _agent.remainingDistance < patrolArrivalThreshold)
            {
                _arrivedAtSound   = true;
                _investigateTimer = investigateDuration;
                _agent.ResetPath();
                Debug.Log("[EnemyAI] Arrived at sound origin. Waiting " + investigateDuration + "s.");
            }
        }
        else
        {
            _investigateTimer -= Time.deltaTime;
            Debug.Log("[EnemyAI] Investigating... " + _investigateTimer.ToString("F1") + "s left");

            if (_investigateTimer <= 0f)
            {
                Debug.Log("[EnemyAI] Investigation ended. Returning to patrol.");
                ReturnToPatrol();
            }
        }
    }

    /// <summary>
    /// Resets all investigation state and switches back to Patrol.
    /// </summary>
    void ReturnToPatrol()
    {
        _arrivedAtSound = false;
        _patrolDestSet  = false;
        _state          = AIState.Patrol;
    }

    /// <summary>
    /// Called when player enters catchRadius during investigation.
    /// </summary>
    void TriggerCaught()
    {
        Debug.Log("[EnemyAI] Player caught!");
        _playerCaught = true;
        _agent.ResetPath();
        SceneManager.LoadSceneAsync(3);

        // ReturnToPatrol(); only reactivate if we have health bar
    }

    // ─── IHearSound implementation ────────────────────────────────────────────

    /// <summary>
    /// Called by SoundEventManager whenever any sound is emitted in the scene.
    ///
    /// perceivedIntensity = rawIntensity / distance
    /// If perceivedIntensity >= hearingThreshold, switch to Investigate.
    ///
    /// Detection ranges at hearingThreshold = 0.05:
    ///   Crouch (0.1 intensity) → heard within ~2 units
    ///   Walk   (0.3 intensity) → heard within ~6 units
    ///   Run    (0.6 intensity) → heard within ~12 units
    /// </summary>
    public void OnSoundHeard(Vector3 soundPosition, float rawIntensity)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        if (distance < 0.1f) distance = 0.1f;

        float perceivedIntensity = rawIntensity / distance;

        if (perceivedIntensity >= hearingThreshold)
        {
            _soundOrigin    = soundPosition;
            _arrivedAtSound = false;
            _state          = AIState.Investigate;

            Debug.Log($"Proctor heard a noise at pos={soundPosition} " +
                      $"raw={rawIntensity:F2} dist={distance:F1} " +
                      $"perceived={perceivedIntensity:F3} (threshold={hearingThreshold})");
        }
        InvestigateState();  // Immediately react to sound without waiting for next frame
        // might be iffy or buggy but test for now
        // might need to adjust priority sounds
    }

    // ─── Editor visualization ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Catch radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        // Patrol range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRange);

        // Current patrol destination
        if (_patrolDestSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_patrolDest, 0.3f);
            Gizmos.DrawLine(transform.position, _patrolDest);
        }

        // Investigation target
        if (_state == AIState.Investigate)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_soundOrigin, 0.4f);
            Gizmos.DrawLine(transform.position, _soundOrigin);
        }
    }
}