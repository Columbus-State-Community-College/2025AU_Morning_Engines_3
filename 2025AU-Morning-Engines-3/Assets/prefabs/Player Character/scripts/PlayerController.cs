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

    [Header("Animator Reference")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameter Names")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string aimBoolParam = "IsAiming";
    [SerializeField] private string shootTriggerParam = "Shoot";

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isAiming;   // local state we control

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAimAndShoot();
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        float vertical = 0f;

        // W / S input
        if (Input.GetKey(KeyCode.W))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S))
            vertical = -1f;

        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float moveSpeed = 0f;
        if (vertical > 0f)
            moveSpeed = wantsRun ? runSpeed : walkSpeed;
        else if (vertical < 0f)
            moveSpeed = wantsRun ? backwardRunSpeed : backwardWalkSpeed;

        Vector3 move = transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator params
        float animSpeed = 0f;
        if (vertical > 0f) animSpeed = 1f;
        else if (vertical < 0f) animSpeed = -1f;

        if (animator != null)
        {
            animator.SetFloat(speedParam, animSpeed);
            animator.SetBool(isRunningParam, wantsRun);
        }
    }

    void HandleJump()
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

    /// <summary>
    /// RIGHT MOUSE (hold) = aim
    /// LEFT MOUSE (while aiming) = shoot
    /// </summary>
    void HandleAimAndShoot()
    {
        if (animator == null) return;

        // Hold right mouse to aim
        bool rightHeld = Input.GetMouseButton(1);

        if (rightHeld != isAiming)
        {
            isAiming = rightHeld;
            animator.SetBool(aimBoolParam, isAiming);
            Debug.Log(isAiming ? "AIM ON" : "AIM OFF");
        }

        // Shoot only while aiming
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            Debug.Log("SHOOT pressed while aiming");
            animator.ResetTrigger(shootTriggerParam);
            animator.SetTrigger(shootTriggerParam);
        }
    }
}
