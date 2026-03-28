using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI — Replaces enemyAIPatrol.cs. Handles two states: Patrol and Investigate.
/// Chase/attack state is stubbed and can be implemented later.
///
/// ─── STATE MACHINE OVERVIEW ──────────────────────────────────────────────────
///
///   ┌─────────┐   sound heard within     ┌─────────────┐
///   │  PATROL │ ────── threshold ──────▶  │ INVESTIGATE │
///   └─────────┘                           └─────────────┘
///        ▲                                      │
///        └──────── 5s elapsed, no catch ────────┘
///
/// PATROL:
///   Picks a valid random NavMesh point within `patrolRange`.
///   Walks there. When arrived, picks a new point. Loops forever.
///   FIX over old script: uses NavMesh.SamplePosition() instead of
///   a ground raycast, which is why the old enemy never moved.
///
/// INVESTIGATE:
///   Walks to the sound position.
///   Waits `investigateDuration` seconds (default 5).
///   Returns to PATROL from current spot.
///   If another sound fires during investigation, updates destination
///   to the new sound (louder/more urgent sounds override).
///
/// ─── SOUND DETECTION ─────────────────────────────────────────────────────────
///
///   Implements IHearSound — registered with SoundEventManager.
///   When OnSoundHeard() fires:
///     perceivedIntensity = rawIntensity / distanceToSound
///     if perceivedIntensity >= hearingThreshold → switch to INVESTIGATE
///   This means loud sounds carry farther; quiet sounds need proximity.
///
/// ─── REQUIREMENTS FULFILLED ──────────────────────────────────────────────────
///   ✔ Enemy patrols randomly when no sound is heard
///   ✔ Sound within detection threshold triggers investigation
///   ✔ Enemy walks to sound origin and stops
///   ✔ Waits 5 seconds, then resumes patrol from that spot
///   ✔ Catch logic is stubbed — add player proximity check in InvestigateState()
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
    [SerializeField] float patrolArrivalThreshold = 1.2f;

    [Header("Investigation Settings")]
    [Tooltip("How many seconds to wait at a sound origin before resuming patrol.")]
    [SerializeField] float investigateDuration = 7f;

    [Header("Hearing Settings")]
    [Tooltip("Minimum perceived intensity required to trigger investigation.\n" +
             "perceivedIntensity = rawIntensity / distance.\n" +
             "Lower = more sensitive. Recommended range: 0.05 – 0.3")]
    [SerializeField] float hearingThreshold = 0.3f;

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

    private enum AIState { Patrol, Investigate /*, Chase — add later */ }
    private AIState _state = AIState.Patrol;

    // Patrol
    private Vector3 _patrolDest;
    private bool    _patrolDestSet;

    // Investigate
    private Vector3 _soundOrigin;
    private float   _investigateTimer;
    private bool    _arrivedAtSound;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();

        // Register with the sound bus so we receive EmitSound() events
        SoundEventManager.Register(this);
    }

    void OnDestroy()
    {
        // Clean up registration so the static list doesn't hold a dead reference
        SoundEventManager.Unregister(this);
    }

    void Update()
    {
        // Route to the correct state handler each frame
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

    /// ENSURES INFINITE PATROLLING
    /// TODO: keep patrolling if sound not heard. else, approach sound heard and stay there for a bit.
    void PatrolState()
    {
        _agent.speed = patrolSpeed;

        if (!_patrolDestSet)
            FindPatrolPoint();

        if (_patrolDestSet)
        {
            _agent.SetDestination(_patrolDest);
            Debug.Log("[EnemyAI] Patrolling to " + _patrolDest);
        }
            
        // Arrived check — uses NavMeshAgent's built-in remaining distance
        if (_patrolDestSet && !_agent.pathPending && _agent.remainingDistance < patrolArrivalThreshold)
            _patrolDestSet = false; // triggers a new point search next frame
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
    /// 
    /// CHECKS AND FINDS VALID POSITION FOR PATROLLING
    void FindPatrolPoint()
    {
        // Random point in a circle around current position
        Vector3 randomOffset = new Vector3(
            Random.Range(-patrolRange, patrolRange),
            0f,
            Random.Range(-patrolRange, patrolRange)
        );

        Vector3 candidate = transform.position + randomOffset;

        // Validate against baked NavMesh
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
        {
            _patrolDest    = hit.position; // snapped to NavMesh surface
            _patrolDestSet = true;
        }
        // If SamplePosition fails, _patrolDestSet stays false → retried next frame
    }



    // ACTION: TRIGGER PLAYER GETTING CAUGHT
    void TriggerCaught()
    {
        Debug.Log("[EnemyAI] Player caught!");
        _agent.ResetPath();

        // TODO:Add caught logic here:
        // GameManager.Instance.OnPlayerCaught();
        // SceneManager.LoadScene("GameOver");

        ReturnToPatrol();
    }


    // ─── State: Investigate ───────────────────────────────────────────────────

    /// <summary>
    /// INVESTIGATE STATE — called every frame while _state == Investigate.
    ///
    /// Flow:
    ///   1. Move toward _soundOrigin at investigate speed.
    ///   2. Once arrived (_arrivedAtSound), start counting down investigateTimer.
    ///   3. When timer expires → return to Patrol from current position.
    /// </summary>
    void InvestigateState()
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
        _agent.speed = investigateSpeed;
        
        if (!_arrivedAtSound)
        {
            _agent.SetDestination(_soundOrigin);

            // Check if we've reached the sound origin
            if (!_agent.pathPending && _agent.remainingDistance < patrolArrivalThreshold)
            {
                _arrivedAtSound   = true;
                _investigateTimer = investigateDuration; // start countdown
                _agent.ResetPath(); // stop moving — stand and wait
            }
        }
        else
        {
            // ── Waiting at sound origin ────────────────────────────────────
            _investigateTimer -= Time.deltaTime;

            Debug.Log("[EnemyAI] Investigating... " + _investigateTimer.ToString("F1") + "s left");
            if (_investigateTimer <= 0f)
            {
                // Timer expired — no catch — resume patrol from here
                Debug.Log("[EnemyAI] Investigation ended. Returning to patrol.");
                ReturnToPatrol();
            }
        }
    }

    /// <summary>
    /// Resets all investigation state and switches back to Patrol.
    /// Called when investigation timer expires.
    /// </summary>
    void ReturnToPatrol()
    {
        _arrivedAtSound = false;
        _patrolDestSet  = false; // force a new patrol point from current position
        _state          = AIState.Patrol;
    }

    // ─── IHearSound implementation ────────────────────────────────────────────

    /// <summary>
    /// Called by SoundEventManager whenever any sound is emitted in the scene.
    ///
    /// Flow:
    ///   1. Calculate distance from this enemy to the sound.
    ///   2. Compute perceivedIntensity = rawIntensity / distance.
    ///      (Louder sounds carry farther; quiet ones require proximity.)
    ///   3. If perceivedIntensity >= hearingThreshold, switch to Investigate.
    ///
    /// This is called for EVERY sound emitted — the threshold check is what
    /// filters out irrelevant sounds (e.g. crouching far away).
    /// </summary>
    public void OnSoundHeard(Vector3 soundPosition, float rawIntensity)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);

        // Avoid divide-by-zero for sounds emitted at the enemy's exact position
        if (distance < 0.1f) distance = 0.1f;

        float perceivedIntensity = rawIntensity / distance;

        if (perceivedIntensity >= hearingThreshold)
        {
            // Sound is loud enough — investigate it
            _soundOrigin    = soundPosition;
            _arrivedAtSound = false;            // reset so we walk to new position
            _state          = AIState.Investigate;

            Debug.Log($"[EnemyAI] Sound heard at {soundPosition} " +
                      $"| raw={rawIntensity:F2} dist={distance:F1} " +
                      $"perceived={perceivedIntensity:F3}");
        }
    }

    // ─── Editor visualization ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
        // Patrol range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRange);

        // Show current patrol destination
        if (_patrolDestSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_patrolDest, 0.3f);
        }

        // Show investigation target
        if (_state == AIState.Investigate)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_soundOrigin, 0.4f);
            Gizmos.DrawLine(transform.position, _soundOrigin);
        }
    }
}
