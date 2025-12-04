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

    // NEW: for left/right walk
    [SerializeField] private string strafeParam = "Strafe"; // float: -1 left, 0 idle, 1 right

    [Header("State")]
    public bool isActive = true; // can turn off when in vehicle, cutscene, etc.

    private CharacterController controller;
    private Vector3 verticalVelocity; // only Y is used
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

        // Safety: if the Inspector has an empty string, force "Strafe"
        if (string.IsNullOrWhiteSpace(strafeParam))
        {
            strafeParam = "Strafe";
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isActive)
            return;

        HandleLook();
        GroundCheck();
        HandleMovement();     // horizontal movement + walk/run anims
        HandleJump();         // instant jump + animation
        ApplyGravity();       // vertical movement
        HandleAimAndShoot();  // right mouse aim, left mouse shoot
    }

    // ------------ LOOK (mouse) ------------
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

    // ------------ GROUND CHECK ------------
    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;

        // tiny downward force to keep us grounded
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
    }

    // ------------ MOVEMENT + RUN FORWARD/BACKWARD ------------
    private void HandleMovement()
    {
        // WASD input
        float h = Input.GetAxisRaw("Horizontal"); // A / D
        float v = Input.GetAxisRaw("Vertical");   // W / S

        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Decide speed based on forward/back & run
        float moveSpeed = 0f;

        if (v > 0f) // forward
        {
            moveSpeed = wantsRun ? runSpeed : walkSpeed;
        }
        else if (v < 0f) // backward
        {
            moveSpeed = wantsRun ? backwardRunSpeed : backwardWalkSpeed;
        }
        else if (Mathf.Abs(h) > 0.01f) // strafing only
        {
            moveSpeed = walkSpeed;
        }

        // Combine directions
        Vector3 moveDirection = (transform.right * h + transform.forward * v);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 horizontalMove = moveDirection * moveSpeed * Time.deltaTime;
        controller.Move(horizontalMove);

        // ----- Animator: Speed, IsRunning, Strafe -----
        if (animator != null)
        {
            // Forward/back speed param (same as before)
            float animSpeed = 0f;
            if (v > 0f) animSpeed = 1f;
            else if (v < 0f) animSpeed = -1f;
            animator.SetFloat(speedParam, animSpeed);

            bool isMovingForwardOrBack = Mathf.Abs(v) > 0.1f;
            animator.SetBool(isRunningParam, wantsRun && isMovingForwardOrBack);

            // NEW: Strafe param for left/right walk
            float strafe = 0f;
            if (h < -0.1f)      // A key
                strafe = -1f;   // walk left
            else if (h > 0.1f)  // D key
                strafe = 1f;    // walk right

            animator.SetFloat(strafeParam, strafe);

            // DEBUG: see what we're sending to the Animator
            // Comment this out later if it spams too much
            if (Mathf.Abs(strafe) > 0.1f || Mathf.Abs(animSpeed) > 0.1f)
            {
                Debug.Log($"OnFootPlayerController - v:{v} h:{h}  Speed:{animSpeed}  Strafe:{strafe}");
            }
        }
    }

    // ------------ JUMP (instant animation + physics) ------------
    private void HandleJump()
    {
        // Only jump when grounded & space pressed this frame
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            // 1) Trigger jump animation immediately
            if (animator != null && !string.IsNullOrEmpty(jumpTriggerParam))
            {
                animator.SetTrigger(jumpTriggerParam);
            }

            // 2) Apply jump velocity same frame
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // ------------ GRAVITY / VERTICAL MOVEMENT ------------
    private void ApplyGravity()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // ------------ AIM + SHOOT (upper body) ------------
    // Right mouse (hold) = aim (IsAiming true)
    // Left mouse (while aiming) = shoot (Shoot trigger)
    private void HandleAimAndShoot()
    {
        if (animator == null) return;

        // RMB hold → aiming
        bool rightHeld = Input.GetMouseButton(1);

        if (rightHeld != isAiming)
        {
            isAiming = rightHeld;
            animator.SetBool(aimBoolParam, isAiming);
        }

        // LMB click while aiming → shoot
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            animator.ResetTrigger(shootTriggerParam);
            animator.SetTrigger(shootTriggerParam);
        }
    }

    // 👇 This is the property the camera script will read
    public bool IsAiming => isAiming;
}
