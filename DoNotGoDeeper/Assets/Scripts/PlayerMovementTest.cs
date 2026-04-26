using UnityEngine;

public class Player : MonoBehaviour
{
    // Camera Rotation
    public float mouseSensitivity = 150f;
    private float verticalRotation = 0f;
    private Transform cameraTransform;

    private float mouseX;
    private float mouseY;

    // Movement
    private Rigidbody rb;
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float moveHorizontal;
    private float moveForward;
    private bool isRunning = false;

    // Jumping
    public float jumpForce = 10f;
    public float fallMultiplier = 2.5f;
    public float ascendMultiplier = 2f;
    private bool isGrounded = true;
    public LayerMask groundLayer;

    private float groundCheckTimer = 0f;
    private float groundCheckDelay = 0.2f;

    private float playerHeight;
    private float raycastDistance;

    // Crouch
    private CapsuleCollider capsuleCollider;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 2f;
    private bool isCrouching = false;

    // Footstep Sounds
    public AudioSource footstepAudioSource;
    public AudioClip footstepSound;
    public float walkStepInterval = 0.5f;
    public float crouchStepInterval = 0.7f;
    public float walkVolume = 1f;
    public float crouchVolume = 0.2f;
    private float footstepTimer = 0f;

    // Sound Emission (for Proctor detection)
    [SerializeField] float crouchSoundIntensity = 0.1f;
    [SerializeField] float walkSoundIntensity = 0.3f;
    [SerializeField] float runSoundIntensity = 0.6f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraTransform = Camera.main.transform;

        playerHeight = capsuleCollider.height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");

        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        // Crouch
        if (Input.GetKey(KeyCode.R))
        {
            capsuleCollider.height = crouchHeight;
            isCrouching = true;
        }
        else
        {
            capsuleCollider.height = defaultHeight;
            isCrouching = false;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        HandleGroundCheck();
        HandleFootsteps();
    }

    void FixedUpdate()
    {
        RotateCamera();
        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        float currentSpeed = isCrouching ? crouchSpeed : isRunning ? runSpeed : moveSpeed;

        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * currentSpeed;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        rb.linearVelocity = velocity;

        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void RotateCamera()
    {
        transform.Rotate(0, mouseX, 0);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Jump()
    {
        isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * ascendMultiplier * Time.fixedDeltaTime;
        }
    }

    void HandleGroundCheck()
    {
        if (!isGrounded && groundCheckTimer <= 0f)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer);
        }
        else
        {
            groundCheckTimer -= Time.deltaTime;
        }
    }

    void HandleFootsteps()
    {
        bool isMoving = Mathf.Abs(moveHorizontal) > 0.1f || Mathf.Abs(moveForward) > 0.1f;

        if (isMoving && isGrounded)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                if (footstepAudioSource != null && footstepSound != null)
                {
                    footstepAudioSource.Stop();
                    footstepAudioSource.volume = isCrouching ? crouchVolume : walkVolume;
                    footstepAudioSource.clip = footstepSound;
                    footstepAudioSource.Play();

                    float intensity =
                        isCrouching ? crouchSoundIntensity :
                        isRunning ? runSoundIntensity :
                        walkSoundIntensity;

                    SoundEventManager.EmitSound(transform.position, intensity);
                }

                footstepTimer = isCrouching ? crouchStepInterval : walkStepInterval;
            }
        }
        else
        {
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
                footstepAudioSource.Stop();

            footstepTimer = 0f;
        }
    }
}