using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("Input")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Scene Names")]
    // Main menu scene is called "mainMenuScene.unity" on disk,
    // but you load it by its name WITHOUT the .unity extension.
    public string mainMenuSceneName = "mainMenuScene";

    private bool isPaused = false;
    private GameObject pauseRoot;   // Root UI object for the pause menu

    private void Start()
    {
        // Make sure we start unpaused
        Time.timeScale = 1f;
        CreatePauseUI();
        SetPauseMenuActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // ----- Pause / Resume -----
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        SetPauseMenuActive(true);

        // Unlock and show cursor (adjust if your controller does this differently)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        SetPauseMenuActive(false);

        // Lock and hide cursor again if that's your normal state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPauseMenuActive(bool active)
    {
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(active);
        }
    }

    // ----- UI Creation -----
    private void CreatePauseUI()
    {
        // Ensure there's an EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Try to find an existing Canvas
        Canvas existingCanvas = FindObjectOfType<Canvas>();

        if (existingCanvas == null)
        {
            // Create a new overlay Canvas
            GameObject canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            existingCanvas = canvasGO.GetComponent<Canvas>();
            existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Root object for pause menu overlay
        pauseRoot = new GameObject("PauseMenuRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pauseRoot.transform.SetParent(existingCanvas.transform, false);

        RectTransform rootRect = pauseRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Dark background
        Image bg = pauseRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        // Create a vertical layout panel in the center
        GameObject panelGO = new GameObject("ButtonsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelGO.transform.SetParent(pauseRoot.transform, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600f, 250f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vLayout = panelGO.GetComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.spacing = 20f;
        vLayout.padding = new RectOffset(20, 20, 20, 20);

        // Create a title
        CreateLabel(panelGO.transform, "PAUSED", 52);

        // Buttons
        CreateButton(panelGO.transform, "RESUME", ResumeGame);
        CreateButton(panelGO.transform, "MAIN MENU", OnMainMenu);
        CreateButton(panelGO.transform, "EXIT GAME", OnExitGame);
    }

    private void CreateLabel(Transform parent, string textValue, int fontSize)
    {
        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(parent, false);

        Text text = textGO.GetComponent<Text>();
        text.text = textValue;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        LayoutElement layout = textGO.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 80f;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        // Button object
        GameObject buttonGO = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 400f;
        layout.preferredHeight = 60f;

        // Text
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    // ----- Button callbacks -----
    private void OnMainMenu()
    {
        // Make sure time is normal before changing scenes
        Time.timeScale = 1f;
        Debug.Log("PauseMenu: Loading main menu scene: " + mainMenuSceneName);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnExitGame()
    {
        Debug.Log("PauseMenu: Exiting game.");
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
