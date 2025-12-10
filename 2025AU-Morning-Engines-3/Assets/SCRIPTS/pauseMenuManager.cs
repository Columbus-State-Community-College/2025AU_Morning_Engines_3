using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Exact name of your main menu scene as it appears in Build Settings.")]
    public string mainMenuSceneName = "mainMenuScene";

    private GameObject pauseRoot;
    private Button resumeButton;
    private Button quitButton;

    private bool isPaused = false;

    // To properly restore player state
    private OnFootPlayerController onFoot;
    private bool wasOnFootActive = true;

    private void Awake()
    {
        // Grab player controller once
        onFoot = FindObjectOfType<OnFootPlayerController>();

        EnsureEventSystem();
        CreatePauseUI();

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;

        // Freeze game time
        Time.timeScale = 0f;

        // Disable on-foot controls (so camera & movement stop)
        if (onFoot != null)
        {
            wasOnFootActive = onFoot.isActive;
            onFoot.isActive = false;
        }

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        isPaused = false;

        Time.timeScale = 1f;

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        // Restore on-foot state to what it was before pausing
        if (onFoot != null)
        {
            onFoot.isActive = wasOnFootActive;
        }

        // Back to FPS mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void QuitToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("PauseMenuManager: mainMenuSceneName is empty. Set it in the inspector.");
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreatePauseUI()
    {
        // Root object
        pauseRoot = new GameObject("PauseMenuRoot");

        // Canvas
        Canvas canvas = pauseRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = pauseRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        pauseRoot.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = pauseRoot.GetComponent<RectTransform>();
        canvasRT.anchorMin = Vector2.zero;
        canvasRT.anchorMax = Vector2.one;
        canvasRT.pivot = new Vector2(0.5f, 0.5f);
        canvasRT.anchoredPosition = Vector2.zero;
        canvasRT.sizeDelta = Vector2.zero;

        // Fullscreen dark overlay
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(pauseRoot.transform, false);

        Image overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform overlayRT = overlayObj.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.pivot = new Vector2(0.5f, 0.5f);
        overlayRT.anchoredPosition = Vector2.zero;
        overlayRT.sizeDelta = Vector2.zero;

        // Center panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(overlayObj.transform, false);

        RectTransform panelRT = panelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(400f, 260f);

        // "PAUSED" title at top
        GameObject titleObj = new GameObject("PausedText");
        titleObj.transform.SetParent(panelObj.transform, false);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 60;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -20f);
        titleRT.sizeDelta = new Vector2(400f, 80f);

        // Resume button (middle)
        resumeButton = CreateButton(panelObj.transform, "ResumeButton", "RESUME");
        RectTransform resumeRT = resumeButton.GetComponent<RectTransform>();
        resumeRT.anchorMin = new Vector2(0.5f, 0.5f);
        resumeRT.anchorMax = new Vector2(0.5f, 0.5f);
        resumeRT.pivot = new Vector2(0.5f, 0.5f);
        resumeRT.anchoredPosition = new Vector2(0f, -10f);
        resumeRT.sizeDelta = new Vector2(300f, 60f);

        resumeButton.onClick.AddListener(ResumeGame);

        // Quit button (below)
        quitButton = CreateButton(panelObj.transform, "QuitButton", "QUIT TO MENU");
        RectTransform quitRT = quitButton.GetComponent<RectTransform>();
        quitRT.anchorMin = new Vector2(0.5f, 0.5f);
        quitRT.anchorMax = new Vector2(0.5f, 0.5f);
        quitRT.pivot = new Vector2(0.5f, 0.5f);
        quitRT.anchoredPosition = new Vector2(0f, -90f);
        quitRT.sizeDelta = new Vector2(300f, 60f);

        quitButton.onClick.AddListener(QuitToMainMenu);
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 60f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 36;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = Vector2.zero;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        button.colors = colors;

        return button;
    }
}
