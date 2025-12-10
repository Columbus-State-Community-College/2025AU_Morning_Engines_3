using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Animation References")]
    [SerializeField] private Animator animator;                 // player animator

    [SerializeField] private string flinchTriggerName = "Flinch";
    [SerializeField] private string flinchStateName = "Flinch"; // state name on upper-body layer
    [SerializeField] private int flinchLayerIndex = 1;          // index of upper-body layer (0 = base, 1 = first extra layer)

    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string deathStateName = "Death";   // name of death state in base layer

    [SerializeField] private OnFootPlayerController playerController; // to disable movement when dead

    // Store position at moment of death so we don't fall
    private Vector3 deathPosition;

    // UI references created at runtime
    private Canvas uiCanvas;
    private TextMeshProUGUI healthText;
    private GameObject deathScreen;
    private Button restartButton;

    private void Awake()
    {
        CacheReferences();
        ResetHealth();
        CreateUI();
        UpdateHealthUI();
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                Debug.LogError("PlayerHealth: No Animator found on player or children!");
            else
                Debug.Log("PlayerHealth: Animator auto-assigned to " + animator.gameObject.name);
        }

        if (playerController == null)
        {
            playerController = GetComponent<OnFootPlayerController>();
            if (playerController == null)
                Debug.LogWarning("PlayerHealth: No OnFootPlayerController found on this object.");
        }
    }

    private void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // DEBUG: press H to test damage / flinch without enemies
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("DEBUG: H pressed – applying 10 damage");
            TakeDamage(10f);
        }

        // DEBUG: press J to FORCE flinch animation (no health change)
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("DEBUG: J pressed – forcing flinch");
            PlayFlinchAnimation();
        }
    }

    private void LateUpdate()
    {
        // Lock player at death position so they don't fall through the map
        if (isDead)
        {
            transform.position = deathPosition;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log("PlayerHealth: Took damage " + amount + ", new health = " + currentHealth);

        UpdateHealthUI();

        // Play FLINCH animation when taking damage (if still alive)
        if (currentHealth > 0f)
        {
            PlayFlinchAnimation();
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void PlayFlinchAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("PlayerHealth: Cannot flinch, animator is null.");
            return;
        }

        Debug.Log("PlayerHealth: Triggering flinch (" + flinchTriggerName + ") on layer " + flinchLayerIndex);

        // Fire the trigger so any transitions that listen for it still work
        if (!string.IsNullOrEmpty(flinchTriggerName))
        {
            animator.ResetTrigger(flinchTriggerName);
            animator.SetTrigger(flinchTriggerName);
        }

        // Play flinch state ONLY on the chosen layer (upper-body layer)
        if (!string.IsNullOrEmpty(flinchStateName))
        {
            int safeLayerIndex = Mathf.Clamp(flinchLayerIndex, 0, animator.layerCount - 1);
            animator.Play(flinchStateName, safeLayerIndex, 0f);
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth} / {maxHealth}";
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PlayerHealth: DIE called");

        // Save position at moment of death so we can lock it
        deathPosition = transform.position;

        // Stop player controls
        if (playerController != null)
        {
            playerController.isActive = false;
        }

        // Play DEATH animation on base layer
        if (animator != null)
        {
            Debug.Log("PlayerHealth: Triggering death (" + deathTriggerName + ")");

            if (!string.IsNullOrEmpty(deathTriggerName))
            {
                animator.ResetTrigger(flinchTriggerName);
                animator.ResetTrigger(deathTriggerName);
                animator.SetTrigger(deathTriggerName);
            }

            // Force death state if you want to be extra sure it plays.
            // Make sure deathStateName matches your death state's name in the Animator.
            if (!string.IsNullOrEmpty(deathStateName))
            {
                animator.Play(deathStateName, 0, 0f); // base layer = 0
            }
        }

        // Do NOT freeze time, or the Animator will stop
        // Time.timeScale = 0f;

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restart button clicked, reloading scene...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===========================================================
    // ===================== UI CREATION =========================
    // ===========================================================
    private void CreateUI()
    {
        // EventSystem (needed for button clicks)
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("RuntimeCanvas");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.anchorMin = Vector2.zero;
        canvasRT.anchorMax = Vector2.one;
        canvasRT.pivot = new Vector2(0.5f, 0.5f);
        canvasRT.anchoredPosition = Vector2.zero;
        canvasRT.sizeDelta = Vector2.zero;

        // Health Text
        GameObject healthObj = new GameObject("HealthText");
        healthObj.transform.SetParent(canvasObj.transform, false);

        healthText = healthObj.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 32;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform healthRT = healthObj.GetComponent<RectTransform>();
        healthRT.anchorMin = new Vector2(0f, 1f);
        healthRT.anchorMax = new Vector2(0f, 1f);
        healthRT.pivot = new Vector2(0f, 1f);
        healthRT.anchoredPosition = new Vector2(20f, -20f);
        healthRT.sizeDelta = new Vector2(400f, 80f);

        // Death Screen Panel
        GameObject deathObj = new GameObject("DeathScreen");
        deathObj.transform.SetParent(canvasObj.transform, false);
        deathScreen = deathObj;

        Image panel = deathObj.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.9f);

        RectTransform panelRT = deathObj.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = Vector2.zero;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        deathScreen.SetActive(false);

        // "YOU DIED" Text
        GameObject textObj = new GameObject("DeathText");
        textObj.transform.SetParent(deathObj.transform, false);

        TextMeshProUGUI deathText = textObj.AddComponent<TextMeshProUGUI>();
        deathText.text = "YOU DIED!";
        deathText.fontSize = 80;
        deathText.alignment = TextAlignmentOptions.Center;
        deathText.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.7f);
        textRT.anchorMax = new Vector2(0.5f, 0.7f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(800f, 200f);

        // Restart Button
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(deathObj.transform, false);

        restartButton = buttonObj.AddComponent<Button>();

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f);

        RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRT.pivot = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = Vector2.zero;
        buttonRT.sizeDelta = new Vector2(300f, 80f);

        // Button text
        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "RESTART";
        btnText.fontSize = 42;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;

        RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.pivot = new Vector2(0.5f, 0.5f);
        btnTextRT.anchoredPosition = Vector2.zero;
        btnTextRT.sizeDelta = Vector2.zero;

        // Button click hookup
        restartButton.onClick.AddListener(RestartGame);
    }
}
