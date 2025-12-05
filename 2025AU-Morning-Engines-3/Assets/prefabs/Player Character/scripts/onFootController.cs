using UnityEngine;
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
    [SerializeField] private string strafeParam = "Strafe";

    // Hard-coded reload trigger name in Animator
    private const string ReloadTriggerName = "Reload";

    [Header("State")]
    public bool isActive = true;

    [Header("Shooting / Ammo")]
    public int maxAmmo = 8;
    public int currentAmmo;
    public TMP_Text ammoText;    // UI bottom-right
    public TMP_Text promptText;  // UI center/bottom messages

    // Ammo box interaction
    private bool isInAmmoBoxRange = false;        // true while inside trigger
    private bool hasPickedUpAmmoFromBox = false;  // true after pressing E; allows R anywhere
    private GameObject currentAmmoBox = null;     // last box we interacted with
    private TMP_Text currentBoxWorldPrompt = null; // 3D text above that box

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
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (string.IsNullOrWhiteSpace(strafeParam))
        {
            strafeParam = "Strafe";
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        if (promptText != null)
        {
            promptText.text = "";
        }

        // Hide all world prompts at start so they don't show from far away
        HideAllAmmoBoxWorldPrompts();
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
        HandleAmmoBoxInteraction();
    }

    // -------- LOOK --------
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

    // -------- GROUND --------
    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
    }

    // -------- MOVEMENT --------
    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float moveSpeed = 0f;
        if (v > 0f)
        {
            moveSpeed = wantsRun ? runSpeed : walkSpeed;
        }
        else if (v < 0f)
        {
            moveSpeed = wantsRun ? backwardRunSpeed : backwardWalkSpeed;
        }
        else if (Mathf.Abs(h) > 0.01f)
        {
            moveSpeed = walkSpeed;
        }

        Vector3 moveDir = (transform.right * h + transform.forward * v);
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            float animSpeed = 0f;
            if (v > 0f) animSpeed = 1f;
            else if (v < 0f) animSpeed = -1f;
            else animSpeed = 0f;
            animator.SetFloat(speedParam, animSpeed);

            bool movingForwardBack = Mathf.Abs(v) > 0.1f;
            animator.SetBool(isRunningParam, wantsRun && movingForwardBack);

            float strafe = 0f;
            if (h < -0.1f) strafe = -1f;
            else if (h > 0.1f) strafe = 1f;
            else strafe = 0f;
            animator.SetFloat(strafeParam, strafe);
        }
    }

    // -------- JUMP --------
    private void HandleJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            if (animator != null && !string.IsNullOrEmpty(jumpTriggerParam))
            {
                animator.SetTrigger(jumpTriggerParam);
            }

            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // -------- GRAVITY --------
    private void ApplyGravity()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // -------- AIM & SHOOT --------
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

            // TODO: shooting logic here

            currentAmmo--;
            UpdateAmmoUI();

            if (currentAmmo <= 0)
            {
                ShowFindAmmoMessage();
            }
        }
    }

    // -------- AMMO BOX INTERACTION --------
    private void HandleAmmoBoxInteraction()
    {
        // Step 1: inside trigger, haven't picked up yet -> show E and handle E
        if (isInAmmoBoxRange && currentAmmoBox != null && !hasPickedUpAmmoFromBox)
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
                {
                    currentBoxWorldPrompt.gameObject.SetActive(false);
                }

                if (promptText != null)
                {
                    promptText.text = "Press R to reload ammo";
                }
            }
        }

        // Step 2: after pickup, R works ANYWHERE
        if (hasPickedUpAmmoFromBox && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Pressed R: trying to reload from picked-up ammo");
            TryReloadFromBox();
        }
    }

    private void TryReloadFromBox()
    {
        // We only care that the player picked up ammo (hasPickedUpAmmoFromBox)
        if (!hasPickedUpAmmoFromBox)
            return;

        // 🔥 Always fire reload animation when R is pressed after pickup
        if (animator != null)
        {
            Debug.Log("Reload trigger fired from TryReloadFromBox");
            animator.ResetTrigger(ReloadTriggerName);
            animator.SetTrigger(ReloadTriggerName);
        }

        // Refill ammo if not already full
        if (currentAmmo < maxAmmo)
        {
            currentAmmo = maxAmmo;
            UpdateAmmoUI();
        }

        if (promptText != null)
        {
            promptText.text = "";
        }

        // Consume that box (if still around)
        if (currentAmmoBox != null)
        {
            Destroy(currentAmmoBox);
        }

        currentAmmoBox = null;
        currentBoxWorldPrompt = null;
        isInAmmoBoxRange = false;
        hasPickedUpAmmoFromBox = false; // need to interact with a new box next time
    }

    // -------- UI HELPERS --------
    private void ShowFindAmmoMessage()
    {
        if (promptText != null)
        {
            promptText.text = "Out of ammo! FIND AMMO!";
        }
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = "Ammo: " + currentAmmo + " / " + maxAmmo;
        }
    }

    // -------- WORLD PROMPTS --------
    private void HideAllAmmoBoxWorldPrompts()
    {
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("AmmoBox");
        foreach (GameObject box in boxes)
        {
            TMP_Text worldPrompt = box.GetComponentInChildren<TMP_Text>(true);
            if (worldPrompt != null)
            {
                worldPrompt.gameObject.SetActive(false);
            }
        }
    }

    // -------- TRIGGERS --------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AmmoBox"))
        {
            isInAmmoBoxRange = true;
            currentAmmoBox = other.gameObject;

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

            if (currentBoxWorldPrompt != null)
            {
                currentBoxWorldPrompt.gameObject.SetActive(false);
                currentBoxWorldPrompt = null;
            }

            // IMPORTANT: we KEEP hasPickedUpAmmoFromBox as-is here
            // so R still works after leaving, as long as you pressed E.
            if (!hasPickedUpAmmoFromBox && promptText != null)
            {
                if (currentAmmo <= 0)
                {
                    ShowFindAmmoMessage();
                }
                else
                {
                    promptText.text = "";
                }
            }

            Debug.Log("Exited AmmoBox trigger");
        }
    }

    public bool IsAiming
    {
        get { return isAiming; }
    }
}
