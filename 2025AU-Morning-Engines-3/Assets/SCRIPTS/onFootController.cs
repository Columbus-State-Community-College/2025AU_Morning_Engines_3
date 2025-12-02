using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OnFootPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("State")]
    public bool isActive = true; // Turned off when in vehicle

    private CharacterController controller;
    private Vector3 velocity;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isActive)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate around Y (turn left/right)
        transform.Rotate(Vector3.up * mouseX);

        // Vertical camera rotation (look up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        // WASD input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Simple jump
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
