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

    // UI references created at runtime
    private Canvas uiCanvas;
    private TextMeshProUGUI healthText;
    private GameObject deathScreen;
    private Button restartButton;

    private void Awake()
    {
        ResetHealth();
        CreateUI();
        UpdateHealthUI();
    }

    private void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        Time.timeScale = 1f;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            Die();
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
        isDead = true;

        // Freeze game
        Time.timeScale = 0f;

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
        // ---------------- EventSystem (needed for button clicks) ----------------
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // ---------------- Canvas ----------------
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

        // ---------------- Health Text ----------------
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

        // ---------------- Death Screen Panel ----------------
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

        // ---------------- Restart Button ----------------
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
