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
///   Walks there at a slightly randomised speed (±15%) so movement feels alive.
///   When arrived, picks a new point. Loops forever.
///
/// INVESTIGATE:
///   Walks to the sound position at investigateSpeed.
///   Waits investigateDuration seconds.
///   Returns to PATROL from current spot.
///   If another sound fires during investigation, updates destination.
///
/// ─── SOUND DETECTION ─────────────────────────────────────────────────────────
///   perceivedIntensity = rawIntensity / distance
///   if perceivedIntensity >= hearingThreshold → switch to INVESTIGATE
///
/// ─── PROXIMITY SENSING ───────────────────────────────────────────────────────
///   If the player is within sensingRadius AND no wall is between them,
///   the Proctor "feels" the presence and investigates their position.
///   This is not sound-based — it creates paranoia at very close range.
///
/// ─── VOCALIZATIONS ───────────────────────────────────────────────────────────
///   The Proctor emits random audio clips while patrolling and investigating.
///   Patrol clips: shuffling, slow breathing (diegetic — player can locate him).
///   Investigate clips: heavier breathing, urgent sounds.
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
        "At 0.10: walks heard ~3 units away, runs ~6 units away.")]
    [SerializeField] float hearingThreshold = 0.05f;

    [Header("Speed")]
    [SerializeField] float patrolSpeed = 10f;
    [SerializeField] float investigateSpeed = 15f;

    [Header("Catch Settings")]
    [Tooltip("The player Transform. Drag the player GameObject here in the Inspector.")]
    [SerializeField] Transform player;

    [Tooltip("How close the player must be (in metres) for the enemy to catch them.")]
    [SerializeField] float catchRadius = 2f;

    [Header("Proximity Sensing")]
    [Tooltip("If the player is within this radius with no wall between them, the Proctor senses them.")]
    [SerializeField] float sensingRadius = 3.5f;

    [Tooltip("Layer mask for walls — used in the line-of-sight check for sensing.")]
    [SerializeField] LayerMask wallLayers;

    [Header("Vocalizations")]
    [Tooltip("Sounds the Proctor makes while patrolling (shuffling, slow breathing, muttering).")]
    [SerializeField] AudioClip[] patrolVocalizations;

    [Tooltip("Sounds the Proctor makes while investigating (heavier breathing, urgent sounds).")]
    [SerializeField] AudioClip[] investigateVocalizations;

    [Tooltip("Minimum seconds between vocalizations.")]
    [SerializeField] float minVocalizationInterval = 8f;

    [Tooltip("Maximum seconds between vocalizations.")]
    [SerializeField] float maxVocalizationInterval = 20f;

    // ─── Private state ────────────────────────────────────────────────────────

    private NavMeshAgent _agent;
    private AudioSource  _audioSource;

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

    // Vocalizations
    private float _vocalizationTimer;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        _agent       = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        SoundEventManager.Register(this);
        _state = AIState.Patrol;
        ResetVocalizationTimer();
    }

    void OnDestroy()
    {
        SoundEventManager.Unregister(this);
    }

    void Update()
    {
        CheckProximitySense();
        TickVocalizations();

        switch (_state)
        {
            case AIState.Patrol:      PatrolState();      break;
            case AIState.Investigate: InvestigateState(); break;
        }
    }

    // ─── State: Patrol ────────────────────────────────────────────────────────

    void PatrolState()
    {
        if (_playerCaught) return;

        AttemptCatchPlayer();

        if (!_patrolDestSet)
            FindPatrolPoint();

        if (_patrolDestSet)
        {
            _agent.SetDestination(_patrolDest);

            if (!_agent.pathPending)
            {
                if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    _patrolDestSet = false;
                    return;
                }

                if (_agent.remainingDistance < patrolArrivalThreshold)
                    _patrolDestSet = false;
            }
        }
    }

    void AttemptCatchPlayer()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= catchRadius)
            TriggerCaught();
            return;
    }

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

            float variance = Random.Range(-patrolSpeed * 0.15f, patrolSpeed * 0.15f);
            _agent.speed   = patrolSpeed + variance;

            Debug.Log("[EnemyAI] New patrol destination: " + _patrolDest);
        }
    }

    // ─── State: Investigate ───────────────────────────────────────────────────

    void InvestigateState()
    {
        _agent.speed = investigateSpeed;

        AttemptCatchPlayer();
        if (_playerCaught) return;

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

            if (_investigateTimer <= 0f)
            {
                Debug.Log("[EnemyAI] Investigation ended. Returning to patrol.");
                ReturnToPatrol();
            }
        }
    }

    void ReturnToPatrol()
    {
        _arrivedAtSound = false;
        _patrolDestSet  = false;
        _state          = AIState.Patrol;
    }

    // ─── Proximity Sensing ────────────────────────────────────────────────────

    void CheckProximitySense()
    {
        if (player == null || _playerCaught) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > sensingRadius) return;

        Vector3 dir = (player.position - transform.position).normalized;

        if (!Physics.Raycast(transform.position, dir, dist, wallLayers))
        {
            if (_state != AIState.Investigate || Vector3.Distance(_soundOrigin, player.position) > 1f)
            {
                _soundOrigin    = player.position;
                _arrivedAtSound = false;
                _state          = AIState.Investigate;
                Debug.Log("[EnemyAI] Proximity sense triggered — investigating player position.");
            }
        }
    }

    // ─── Vocalizations ───────────────────────────────────────────────────────

    void TickVocalizations()
    {
        _vocalizationTimer -= Time.deltaTime;
        if (_vocalizationTimer > 0f) return;

        AudioClip[] clips = (_state == AIState.Investigate)
            ? investigateVocalizations
            : patrolVocalizations;

        if (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) _audioSource.PlayOneShot(clip);
        }

        ResetVocalizationTimer();
    }

    void ResetVocalizationTimer()
    {
        _vocalizationTimer = Random.Range(minVocalizationInterval, maxVocalizationInterval);
    }

    // ─── Catch ────────────────────────────────────────────────────────────────

    void TriggerCaught()
    {
        if (_playerCaught) return; // prevent double-triggering

        Debug.Log("[EnemyAI] Player caught!");
        _playerCaught    = true;
        _agent.ResetPath();
        _agent.isStopped = true;
        _agent.enabled   = false;

        if (GameOver.Instance == null)
            {
                GameOver.Instance = FindFirstObjectByType<GameOver>();
            }

            if (GameOver.Instance != null)
            {
                // Get the current scene so we know where to respawn
                int currentLevel = SceneManager.GetActiveScene().buildIndex;
                GameOver.Instance.EndGame(currentLevel);
            }
            else
            {
                Debug.LogWarning("[EnemyAI] No GameOver found in scene! Loading Scene 3 anyway.");
            }

                //Debug.Log("[EnemyAI] Loading Game Over scene...");
            SceneManager.LoadScene(3);
                //Debug.Log("Finished loading Game Over scene.");

                // ReturnToPatrol(); only reactivate if we have health bar
        }



    // ─── IHearSound implementation ────────────────────────────────────────────

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
    }

    // ─── Editor visualization ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, sensingRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRange);

        if (_patrolDestSet)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_patrolDest, 0.3f);
            Gizmos.DrawLine(transform.position, _patrolDest);
        }

        if (_state == AIState.Investigate)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_soundOrigin, 0.4f);
            Gizmos.DrawLine(transform.position, _soundOrigin);
        }
    }
}
