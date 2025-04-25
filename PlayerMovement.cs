using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    public float walkSpeed = 12f;
    public float sprintSpeed = 20f; // Kecepatan saat sprint
    public float crouchSpeed = 6f;  // Kecepatan saat crouch
    public float gravity = -9.81f * 2f;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // Variabel untuk crouch
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    private Vector3 standingCenter;
    private Vector3 crouchingCenter;
    public float crouchTransitionSpeed = 10f;
    private bool isCrouching = false;

    // Variabel untuk sprint
    private bool isSprinting = false;
    private float currentSpeed;

    Vector3 velocity;
    bool isGrounded;
    bool isMoving;

    private Vector3 lastPosition = new Vector3(0f, 0f, 0f);
   
    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
        
        // Setup untuk crouch
        standingCenter = controller.center;
        crouchingCenter = new Vector3(standingCenter.x, standingCenter.y / 2, standingCenter.z);
         
        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0, -controller.height / 2 + 0.1f, 0);
        }
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        //reset velocity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Handle Crouch
        HandleCrouch();
        
        // Handle Sprint
        HandleSprint();

        //input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //Movement
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        //Jumping (tidak bisa lompat saat crouch)
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        
        //execute the jump
        controller.Move(velocity * Time.deltaTime);

        if (lastPosition != gameObject.transform.position && isGrounded == true)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        lastPosition = gameObject.transform.position;
    }

    void HandleCrouch()
    {
        // Toggle crouch dengan tombol C
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }

        // Atur tinggi controller dan kecepatan berdasarkan status crouch
        if (isCrouching)
        {
            // Transisi ke posisi crouch
            controller.height = Mathf.Lerp(controller.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.center = Vector3.Lerp(controller.center, crouchingCenter, crouchTransitionSpeed * Time.deltaTime);
            currentSpeed = isSprinting ? walkSpeed : crouchSpeed; // Tidak bisa sprint saat crouch
            isSprinting = false;
        }
        else
        {
            // Cek apakah ada ruang untuk berdiri
            if (!Physics.Raycast(transform.position, Vector3.up, standingHeight))
            {
                // Transisi ke posisi berdiri
                controller.height = Mathf.Lerp(controller.height, standingHeight, crouchTransitionSpeed * Time.deltaTime);
                controller.center = Vector3.Lerp(controller.center, standingCenter, crouchTransitionSpeed * Time.deltaTime);
                currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            }
        }

        // Update groundCheck position based on the current height
        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0, -controller.height / 2 + 0.1f, 0);
        }
    }

    void HandleSprint()
    {
        // Sprint dengan Left Shift
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isCrouching)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) || isCrouching)
        {
            isSprinting = false;
            currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}