using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Unity 6 replacement for the old Standard Assets AICharacterControl.cs
/// Works exactly as the tutorial expects — drag your Player into the Target slot,
/// and the enemy will chase them using NavMesh.
/// 
/// Also listens for audio via OnHearSound() — works with NoiseEmitter.cs
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AICharacterControl : MonoBehaviour
{
    [Header("Target (drag your Player here)")]
    public Transform target;

    [Header("Detection")]
    public float chaseRange     = 10f;   // starts chasing when player is within this range
    public float hearingRange   = 15f;   // reacts to sounds within this range
    public float stopDistance   = 1.5f;  // how close to get before stopping

    [Header("Speed")]
    public float chaseSpeed   = 5f;
    public float patrolSpeed  = 2f;

    // Private
    private NavMeshAgent _agent;
    private Animator     _animator;
    private bool         _heardSound;
    private Vector3      _soundPosition;

    // Animator hash
    private int _animSpeed;

    private void Start()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _animSpeed = Animator.StringToHash("Forward"); // matches old Standard Assets animator

        _agent.stoppingDistance = stopDistance;

        // Auto-find player by tag if not assigned
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    private void Update()
    {
        if (target == null) return;

        float distToPlayer = Vector3.Distance(transform.position, target.position);

        // --- Chase if player is close enough OR enemy heard a sound ---
        if (distToPlayer <= chaseRange || _heardSound)
        {
            _agent.speed = chaseSpeed;

            // If we heard a sound but haven't spotted player yet, go to sound position
            if (_heardSound && distToPlayer > chaseRange)
            {
                _agent.SetDestination(_soundPosition);

                // Arrived at sound — stop reacting
                if (_agent.remainingDistance < 0.5f)
                    _heardSound = false;
            }
            else
            {
                // Chase the player directly
                _agent.SetDestination(target.position);
                _heardSound = false;
            }
        }
        else
        {
            // Player out of range — stop
            _agent.SetDestination(transform.position);
        }

        // Update animator
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(_animSpeed, speed, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// Called by NoiseEmitter when a sound is made nearby.
    /// </summary>
    public void OnHearSound(Vector3 soundPosition, bool loud = false)
    {
        float radius = loud ? hearingRange * 2f : hearingRange;
        float dist   = Vector3.Distance(transform.position, soundPosition);

        if (dist > radius) return;

        _heardSound    = true;
        _soundPosition = soundPosition;
        Debug.Log($"[AICharacterControl] Heard sound at {soundPosition}");
    }

    // Draw ranges in editor
    private void OnDrawGizmosSelected()
    {
        // Chase range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Hearing range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}