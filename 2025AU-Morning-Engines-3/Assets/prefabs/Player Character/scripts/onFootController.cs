using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private string reloadTriggerParam = "Reload"; // MUST match Animator Trigger
    [SerializeField] private string strafeParam = "Strafe";        // -1 left, 0 idle, 1 right

    [Header("State")]
    public bool isActive = true;

    [Header("Shooting / Ammo")]
    public int maxAmmo = 8;
    public int currentAmmo;
    public TMP_Text ammoText;    // UI: bottom right
    public TMP_Text promptText;  // UI: messages like "Out of ammo", "Press R..."

    // Ammo box interaction (trigger-based)
    private bool isInAmmoBoxRange = false;
    private bool hasPickedUpAmmoFromBox = false; // becomes true after E
    private GameObject currentAmmoBox = null;    // the box we're standing in
    private TMP_Text currentBoxWorldPrompt = null; // 3D TMP text above the box

    private CharacterController controller;
    private Vector3 verticalVelocity;
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
            cameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (string.IsNullOrWhiteSpace(strafeParam))
            strafeParam = "Strafe";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ammo setup
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        if (promptText != null)
            promptText.text = "";
    }

    private void Update()
    {
        if (!isActive)
            return;

        HandleLook();
        GroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleAimAndShoot();
        HandleAmmoBoxInteraction(); // E + R + destroy box
    }

    // ------------- LOOK -------------
    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // ------------- GROUND CHECK -------------
    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;
    }

    // ------------- MOVEMENT -------------
    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A / D
        float v = Input.GetAxisRaw("Vertical");   // W / S

        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float moveSpeed = 0f;
        if (v > 0f)
            moveSpeed = wantsRun ? runSpeed : walkSpeed;
        else if (v < 0f)
            moveSpeed = wantsRun ? backwardRunSpeed : backwardWalkSpeed;
        else if (Mathf.Abs(h) > 0.01f)
            moveSpeed = walkSpeed;

        Vector3 moveDir = (transform.right * h + transform.forward * v);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            float animSpeed = 0f;
            if (v > 0f) animSpeed = 1f;
            else if (v < 0f) animSpeed = -1f;
            animator.SetFloat(speedParam, animSpeed);

            bool movingForwardBack = Mathf.Abs(v) > 0.1f;
            animator.SetBool(isRunningParam, wantsRun && movingForwardBack);

            float strafe = 0f;
            if (h < -0.1f) strafe = -1f;
            else if (h > 0.1f) strafe = 1f;
            animator.SetFloat(strafeParam, strafe);
        }
    }

    // ------------- JUMP -------------
    private void HandleJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            if (animator != null && !string.IsNullOrEmpty(jumpTriggerParam))
                animator.SetTrigger(jumpTriggerParam);

            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // ------------- GRAVITY -------------
    private void ApplyGravity()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // ------------- AIM & SHOOT -------------
    private void HandleAimAndShoot()
    {
        if (animator == null) return;

        bool rightHeld = Input.GetMouseButton(1);
        if (rightHeld != isAiming)
        {
            isAiming = rightHeld;
            animator.SetBool(aimBoolParam, isAiming);
        }

        if (isAiming && Input.GetMouseButtonDown(0))
        {
            if (currentAmmo <= 0)
            {
                ShowFindAmmoMessage();
                return;
            }

            animator.ResetTrigger(shootTriggerParam);
            animator.SetTrigger(shootTriggerParam);

            // TODO: actual shooting logic (raycast, projectile) here

            currentAmmo--;
            UpdateAmmoUI();

            if (currentAmmo <= 0)
                ShowFindAmmoMessage();
        }
    }

    // ------------- AMMO BOX INTERACTION (trigger-based E + R) -------------
    private void HandleAmmoBoxInteraction()
    {
        if (!isInAmmoBoxRange || currentAmmoBox == null)
            return;

        // STEP 1: inside trigger → show "Press E to interact"
        if (!hasPickedUpAmmoFromBox)
        {
            if (currentBoxWorldPrompt != null)
            {
                currentBoxWorldPrompt.text = "Press E to interact";
                currentBoxWorldPrompt.gameObject.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                hasPickedUpAmmoFromBox = true;
                Debug.Log("Pressed E: picked up ammo from box");

                if (currentBoxWorldPrompt != null)
                    currentBoxWorldPrompt.gameObject.SetActive(false);

                if (promptText != null)
                    promptText.text = "Press R to reload ammo";
            }
        }
        // STEP 2: after E → press R to reload + play animation + destroy box
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Pressed R: trying to reload from box");
                TryReloadFromBox();
            }
        }
    }

    private void TryReloadFromBox()
    {
        if (currentAmmoBox == null)
            return;

        if (currentAmmo >= maxAmmo)
        {
            if (promptText != null)
                promptText.text = "";
            return;
        }

        // 🔥 PLAY RELOAD ANIMATION
        if (animator != null && !string.IsNullOrEmpty(reloadTriggerParam))
        {
            Debug.Log("Reload trigger fired from TryReloadFromBox");
            animator.ResetTrigger(reloadTriggerParam);
            animator.SetTrigger(reloadTriggerParam);
        }

        // Refill immediately for now
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        if (promptText != null)
            promptText.text = "";

        // Destroy box so it's one-use
        Destroy(currentAmmoBox);
        currentAmmoBox = null;
        currentBoxWorldPrompt = null;
        isInAmmoBoxRange = false;
        hasPickedUpAmmoFromBox = false;
    }

    // ------------- UI HELPERS -------------
    private void ShowFindAmmoMessage()
    {
        if (promptText != null)
            promptText.text = "Out of ammo! FIND AMMO!";
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = "Ammo: " + currentAmmo + " / " + maxAmmo;
    }

    // ------------- TRIGGERS FOR AMMO BOXES -------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AmmoBox"))
        {
            isInAmmoBoxRange = true;
            hasPickedUpAmmoFromBox = false;
            currentAmmoBox = other.gameObject;

            // find 3D TMP text child
            currentBoxWorldPrompt = other.GetComponentInChildren<TMP_Text>(true);
            if (currentBoxWorldPrompt != null)
            {
                currentBoxWorldPrompt.text = "Press E to interact";
                currentBoxWorldPrompt.gameObject.SetActive(true);
            }

            Debug.Log("Entered AmmoBox trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("AmmoBox") && other.gameObject == currentAmmoBox)
        {
            isInAmmoBoxRange = false;
            hasPickedUpAmmoFromBox = false;

            if (currentBoxWorldPrompt != null)
            {
                currentBoxWorldPrompt.gameObject.SetActive(false);
                currentBoxWorldPrompt = null;
            }

            currentAmmoBox = null;

            if (promptText != null)
            {
                if (currentAmmo <= 0)
                    ShowFindAmmoMessage();
                else
                    promptText.text = "";
            }

            Debug.Log("Exited AmmoBox trigger");
        }
    }

    public bool IsAiming => isAiming;
}
