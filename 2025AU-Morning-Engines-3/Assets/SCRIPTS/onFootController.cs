using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OnFootPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float backwardWalkSpeed = 2.5f;
    public float backwardRunSpeed = 4.5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("Animator Reference")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameter Names")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string aimBoolParam = "IsAiming";
    [SerializeField] private string shootTriggerParam = "Shoot";

    [Header("State")]
    public bool isActive = true; // turn off when in vehicle etc.

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isAiming;
    private float xRotation = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isActive)
            return;

        HandleLook();
        HandleMovement();
        HandleJump();
        HandleAimAndShoot();
    }

    // ------------ LOOK ------------
    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // ------------ MOVEMENT + ANIMATOR SPEED ------------
    private void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // tiny downward force to stay grounded
        }

        // WASD input
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Decide speed based on forward/back and running
        float moveSpeed = 0f;

        if (v > 0f) // forward
        {
            moveSpeed = wantsRun ? runSpeed : walkSpeed;
        }
        else if (v < 0f) // backward
        {
            moveSpeed = wantsRun ? backwardRunSpeed : backwardWalkSpeed;
        }
        else // standing still (only strafing)
        {
            moveSpeed = walkSpeed; // or 0 if you want strictly no movement
        }

        // Combine forward/back and strafe
        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f)
            move.Normalize();

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity (vertical movement)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator parameters for locomotion
        if (animator != null)
        {
            float animSpeed = 0f;
            if (v > 0f) animSpeed = 1f;
            else if (v < 0f) animSpeed = -1f;

            animator.SetFloat(speedParam, animSpeed);
            animator.SetBool(isRunningParam, wantsRun && v > 0f);
        }
    }

    // ------------ JUMP ------------
    private void HandleJump()
    {
        if (!isGrounded) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null && !string.IsNullOrEmpty(jumpTriggerParam))
            {
                animator.SetTrigger(jumpTriggerParam);
            }
        }
    }

    // ------------ AIM + SHOOT ------------
    // Right mouse (hold) = aim
    // Left mouse (while aiming) = shoot
    private void HandleAimAndShoot()
    {
        if (animator == null) return;

        // Hold right mouse to aim
        bool rightHeld = Input.GetMouseButton(1);

        if (rightHeld != isAiming)
        {
            isAiming = rightHeld;
            animator.SetBool(aimBoolParam, isAiming);
            // Debug.Log(isAiming ? "AIM ON" : "AIM OFF");
        }

        // Shoot only while aiming
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            // Debug.Log("SHOOT pressed while aiming");
            animator.ResetTrigger(shootTriggerParam);
            animator.SetTrigger(shootTriggerParam);
        }
    }
}
