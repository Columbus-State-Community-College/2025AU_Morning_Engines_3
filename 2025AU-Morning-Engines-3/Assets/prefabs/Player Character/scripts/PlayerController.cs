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
        HandleAim();
        HandleShoot();
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float vertical = 0f;

        // W / S input
        if (Input.GetKey(KeyCode.W))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S))
            vertical = -1f;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float moveSpeed = 0f;
        if (vertical > 0)
            moveSpeed = isRunning ? runSpeed : walkSpeed;
        else if (vertical < 0)
            moveSpeed = isRunning ? backwardRunSpeed : backwardWalkSpeed;

        // Move in forward direction
        Vector3 move = transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator params
        float animSpeed = 0f;
        if (vertical > 0) animSpeed = 1f;
        else if (vertical < 0) animSpeed = -1f;

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

    void HandleAim()
    {
        // Right mouse button
        bool isAiming = Input.GetMouseButton(1);
        animator.SetBool("IsAiming", isAiming);
    }

    void HandleShoot()
    {
        // Left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Shoot");
        }
    }
}
