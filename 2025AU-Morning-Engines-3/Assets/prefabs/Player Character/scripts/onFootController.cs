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

    private const string ReloadTriggerName = "Reload";

    [Header("State")]
    public bool isActive = true;

    [Header("Shooting / Ammo")]
    public int maxAmmo = 8;
    public int currentAmmo;
    public TMP_Text ammoText;
    public TMP_Text promptText;

    [Header("UI Crosshair")]
    public GameObject crossHairUI;

    [Header("Weapon")]
    public ShotgunShoot shotgunShooter;   // <--- NEW: reference to shotgun script

    // Ammo box interaction
    private bool isInAmmoBoxRange = false;
    private bool hasPickedUpAmmoFromBox = false;
    private GameObject currentAmmoBox = null;
    private TMP_Text currentBoxWorldPrompt = null;

    // Rotation-based strafe
    private float rotateStrafe = 0f;
    private float lastYaw = 0f;
    private float lastYawDelta = 0f;

    [Header("Rotation Strafe Settings")]
    [SerializeField] private float rotateStrafeBlendSpeed = 12f;
    [SerializeField] private float yawToStrafeThreshold = 0.005f;
    [SerializeField] private float turningHoldTime = 0.25f;

    private float turningTimer = 0f;

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

        HideAllAmmoBoxWorldPrompts();

        lastYaw = transform.eulerAngles.y;

        if (crossHairUI == null)
        {
            crossHairUI = GameObject.Find("CrossHair");
        }

        if (crossHairUI != null)
        {
            crossHairUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActive)
        {
            if (crossHairUI != null) crossHairUI.SetActive(false);
            return;
        }

        HandleLook();
        GroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleAimAndShoot();
        HandleAmmoBoxInteraction();
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float rawMouseX = Input.GetAxis("Mouse X");
        float rawMouseY = Input.GetAxis("Mouse Y");

        float mouseX = rawMouseX * mouseSensitivity;
        float mouseY = rawMouseY * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        float currentYaw = transform.eulerAngles.y;
        lastYawDelta = Mathf.DeltaAngle(lastYaw, currentYaw);
        lastYaw = currentYaw;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

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
            animator.SetFloat(speedParam, animSpeed);

            bool movingForwardBack = Mathf.Abs(v) > 0.1f;
            animator.SetBool(isRunningParam, wantsRun && movingForwardBack);

            float strafe;

            if (h < -0.1f)
            {
                strafe = -1f;
                rotateStrafe = strafe;
                turningTimer = 0f;
            }
            else if (h > 0.1f)
            {
                strafe = 1f;
                rotateStrafe = strafe;
                turningTimer = 0f;
            }
            else
            {
                bool isStandingStill = Mathf.Abs(v) < 0.1f;

                if (isStandingStill && Mathf.Abs(lastYawDelta) > yawToStrafeThreshold)
                {
                    turningTimer = turningHoldTime;

                    float target = Mathf.Sign(lastYawDelta);
                    rotateStrafe = Mathf.MoveTowards(
                        rotateStrafe,
                        target,
                        rotateStrafeBlendSpeed * Time.deltaTime
                    );
                }
                else
                {
                    if (turningTimer > 0f)
                    {
                        turningTimer -= Time.deltaTime;
                    }

                    if (turningTimer <= 0f)
                    {
                        rotateStrafe = Mathf.MoveTowards(
                            rotateStrafe,
                            0f,
                            rotateStrafeBlendSpeed * Time.deltaTime
                        );
                    }
                }

                strafe = rotateStrafe;
            }

            animator.SetFloat(strafeParam, strafe);
        }
    }

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

    private void ApplyGravity()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleAimAndShoot()
    {
        if (animator == null) return;

        bool rightHeld = Input.GetMouseButton(1);

        if (rightHeld != isAiming)
        {
            isAiming = rightHeld;
            animator.SetBool(aimBoolParam, isAiming);

            if (crossHairUI != null)
            {
                crossHairUI.SetActive(isAiming);
            }
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

            // NEW: actually fire shotgun pellets
            if (shotgunShooter != null)
            {
                shotgunShooter.Fire();
            }

            currentAmmo--;
            UpdateAmmoUI();

            if (currentAmmo <= 0)
            {
                ShowFindAmmoMessage();
            }
        }
    }

    private void HandleAmmoBoxInteraction()
    {
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

        if (hasPickedUpAmmoFromBox && Input.GetKeyDown(KeyCode.R))
        {
            TryReloadFromBox();
        }
    }

    private void TryReloadFromBox()
    {
        if (!hasPickedUpAmmoFromBox)
            return;

        if (animator != null)
        {
            animator.ResetTrigger(ReloadTriggerName);
            animator.SetTrigger(ReloadTriggerName);
        }

        if (currentAmmo < maxAmmo)
        {
            currentAmmo = maxAmmo;
            UpdateAmmoUI();
        }

        if (promptText != null)
        {
            promptText.text = "";
        }

        if (currentAmmoBox != null)
        {
            Destroy(currentAmmoBox);
        }

        currentAmmoBox = null;
        currentBoxWorldPrompt = null;
        isInAmmoBoxRange = false;
        hasPickedUpAmmoFromBox = false;
    }

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
        }
    }

    public bool IsAiming => isAiming;
}
