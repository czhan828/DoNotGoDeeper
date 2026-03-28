using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public Transform cameraPivot;

    [Header("Movement")]
    public float normalWalkSpeed = 60f;
    public float normalRunSpeed = 75f;
    public float walkSpeed;
    public float runSpeed;
    public float jumpPower = 7f;
    public float gravity = 10f;

    [Header("Mouse Look")]
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    [Header("Crouch")]
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

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

        // initialize speeds
        walkSpeed = normalWalkSpeed;
        runSpeed = normalRunSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        float curSpeedX = canMove ? currentSpeed * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? currentSpeed * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Crouch with R key
        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
            isCrouching = true;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = normalWalkSpeed;
            runSpeed = normalRunSpeed;
            isCrouching = false;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        HandleFootsteps(curSpeedX, curSpeedY);

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            cameraPivot.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    void HandleFootsteps(float speedX, float speedY)
    {
        bool isMoving = Mathf.Abs(speedX) > 0.1f || Mathf.Abs(speedY) > 0.1f;
        bool isGrounded = characterController.isGrounded;

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
            {
                footstepAudioSource.Stop();
            }

            footstepTimer = 0f;
        }
    }
}