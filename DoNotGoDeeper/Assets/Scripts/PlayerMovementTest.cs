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
    private float moveHorizontal;
    private float moveForward;

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


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // ✅ smooth movement

        cameraTransform = Camera.main.transform;

        playerHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
{
    moveHorizontal = Input.GetAxisRaw("Horizontal");
    moveForward = Input.GetAxisRaw("Vertical");

    // ✅ mouse input (NO deltaTime)
    mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
    mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

    if (Input.GetButtonDown("Jump") && isGrounded)
    {
        Jump();
    }

    HandleGroundCheck();
}

    void FixedUpdate()
    {
        RotateCamera();   // ✅ moved here to match physics timing
        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * moveSpeed;

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
}