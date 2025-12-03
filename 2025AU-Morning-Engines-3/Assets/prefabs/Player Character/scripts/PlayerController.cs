using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float backwardWalkSpeed = 2.5f;
    public float backwardRunSpeed = 4.5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("References")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // we cache aiming state so shoot can check it
    private bool isAiming;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAimAndShoot();
    }

    void HandleMovement()
    {
        // ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float vertical = 0f;

        // W / S for forward/backward
        if (Input.GetKey(KeyCode.W))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S))
            vertical = -1f;

        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float moveSpeed = 0f;
        if (vertical > 0f)
            moveSpeed = isRunning ? runSpeed : walkSpeed;
        else if (vertical < 0f)
            moveSpeed = isRunning ? backwardRunSpeed : backwardWalkSpeed;

        // move along local forward axis
        Vector3 move = transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // animator params for locomotion
        float animSpeed = 0f;
        if (vertical > 0f) animSpeed = 1f;
        else if (vertical < 0f) animSpeed = -1f;

        animator.SetFloat("Speed", animSpeed);
        animator.SetBool("IsRunning", isRunning);
    }

    void HandleJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }

    /// <summary>
    /// Handles BOTH aiming and shooting so they always work together.
    /// Right mouse = aim (hold), Left mouse while aiming = shoot.
    /// </summary>
    void HandleAimAndShoot()
    {
        // RIGHT MOUSE: hold to aim
        isAiming = Input.GetMouseButton(1);        // true while button held
        animator.SetBool("IsAiming", isAiming);    // drives UpperBody layer

        // LEFT MOUSE: shoot ONLY if we are aiming
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Shoot");
        }
    }
}
