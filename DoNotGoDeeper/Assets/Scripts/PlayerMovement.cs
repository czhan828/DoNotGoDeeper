using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;
    public AudioClip footstepSound;
    public float walkStepInterval = 0.5f;   // time between footstep sounds while walking
    public float crouchStepInterval = 0.7f; // time between footstep sounds while sneaking
    public float walkVolume = 1f;
    public float crouchVolume = 0.2f;       // quiet while sneaking

    private float footstepTimer = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;
    private bool canMove = true;
    private bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
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

        // Crouch / sneak with R key
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
            walkSpeed = 6f;
            runSpeed = 12f;
            isCrouching = false;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // Handle footstep sounds
        HandleFootsteps(curSpeedX, curSpeedY);

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
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
                }
                footstepTimer = isCrouching ? crouchStepInterval : walkStepInterval;
            }
        }
        else
        {
            // Player stopped or in air — stop sound immediately
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
            footstepTimer = 0f;
        }
    }
}