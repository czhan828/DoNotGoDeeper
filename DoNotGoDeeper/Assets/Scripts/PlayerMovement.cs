using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public Transform cameraPivot;

    [Header("Movement")]
    public float normalWalkSpeed = 6f;
    public float normalRunSpeed = 9f;
    public float crouchSpeed = 3f;
    public float jumpPower = 7f;
    public float gravity = 20f;

    private float walkSpeed;
    private float runSpeed;

    [Header("Mouse Look")]
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    [Header("Crouch")]
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;
    public AudioClip footstepSound;
    public float walkStepInterval = 0.5f;
    public float crouchStepInterval = 0.7f;
    public float walkVolume = 1f;
    public float crouchVolume = 0.2f;

    [SerializeField] float crouchSoundIntensity = 0.1f;
    [SerializeField] float walkSoundIntensity = 0.3f;
    [SerializeField] float runSoundIntensity = 0.6f;

    private float footstepTimer = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    private CharacterController characterController;

    private bool canMove = true;
    private bool isCrouching = false;
    private bool isRunning = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        walkSpeed = normalWalkSpeed;
        runSpeed = normalRunSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ---------------- MOVEMENT INPUT ----------------
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        float inputX = Input.GetAxis("Vertical");
        float inputY = Input.GetAxis("Horizontal");

        Vector3 move = (forward * inputX + right * inputY) * currentSpeed;

        // ---------------- GROUND CHECK ----------------
        if (characterController.isGrounded)
        {
            if (moveDirection.y < 0)
                moveDirection.y = -2f; // keeps player grounded

            if (Input.GetButtonDown("Jump") && canMove)
            {
                moveDirection.y = jumpPower;
            }
        }

        // ---------------- GRAVITY ----------------
        moveDirection.y -= gravity * Time.deltaTime;

        // combine movement
        Vector3 finalMove = move + new Vector3(0, moveDirection.y, 0);

        // ---------------- CROUCH ----------------
        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            characterController.center = new Vector3(0, crouchHeight / 2f, 0);

            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;

            isCrouching = true;
        }
        else
        {
            characterController.height = defaultHeight;
            characterController.center = new Vector3(0, defaultHeight / 2f, 0);

            walkSpeed = normalWalkSpeed;
            runSpeed = normalRunSpeed;

            isCrouching = false;
        }

        // ---------------- MOVE PLAYER ----------------
        characterController.Move(finalMove * Time.deltaTime);

        // ---------------- CAMERA LOOK ----------------
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            cameraPivot.localRotation = Quaternion.Euler(rotationX, 0, 0);

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            transform.Rotate(0, mouseX, 0);
        }

        HandleFootsteps(inputX, inputY);
    }

    void HandleFootsteps(float speedX, float speedY)
    {
        bool isMoving = Mathf.Abs(speedX) > 0.1f || Mathf.Abs(speedY) > 0.1f;

        if (isMoving && characterController.isGrounded)
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
            {
                footstepAudioSource.Stop();
            }

            footstepTimer = 0f;
        }
    }
}