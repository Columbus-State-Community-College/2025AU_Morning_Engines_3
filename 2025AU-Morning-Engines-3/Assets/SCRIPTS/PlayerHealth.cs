using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f; // this is now BASE health
    private float currentHealth;
    private bool isDead = false;

    [Header("Animation References")]
    [SerializeField] private Animator animator;

    [SerializeField] private string flinchTriggerName = "Flinch";
    [SerializeField] private string flinchStateName = "Flinch";
    [SerializeField] private int flinchLayerIndex = 1;

    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string deathStateName = "Death";

    [SerializeField] private OnFootPlayerController playerController;

    private Vector3 deathPosition;

    private Canvas uiCanvas;
    private TextMeshProUGUI healthText;
    private GameObject deathScreen;
    private Button restartButton;

    private float baseMaxHealth;

    private void Awake()
    {
        baseMaxHealth = maxHealth;

        UpgradeStatsManager.EnsureExists();
        maxHealth = baseMaxHealth + UpgradeStatsManager.GetHealthBonus();

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
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("DEBUG: H pressed – applying 10 damage");
            TakeDamage(10f);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("DEBUG: J pressed – forcing flinch");
            PlayFlinchAnimation();
        }
    }

    private void LateUpdate()
    {
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

        if (!string.IsNullOrEmpty(flinchTriggerName))
        {
            animator.ResetTrigger(flinchTriggerName);
            animator.SetTrigger(flinchTriggerName);
        }

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

        deathPosition = transform.position;

        if (playerController != null)
        {
            playerController.isActive = false;
        }

        if (animator != null)
        {
            Debug.Log("PlayerHealth: Triggering death (" + deathTriggerName + ")");

            if (!string.IsNullOrEmpty(deathTriggerName))
            {
                animator.ResetTrigger(flinchTriggerName);
                animator.ResetTrigger(deathTriggerName);
                animator.SetTrigger(deathTriggerName);
            }

            if (!string.IsNullOrEmpty(deathStateName))
            {
                animator.Play(deathStateName, 0, 0f);
            }
        }

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

    private void CreateUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

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

        restartButton.onClick.AddListener(RestartGame);
    }
}
