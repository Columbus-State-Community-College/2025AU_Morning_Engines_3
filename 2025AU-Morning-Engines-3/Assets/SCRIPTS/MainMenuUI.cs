using UnityEngine;
using UnityEngine.UI;   // For Button, Image, Text

public class MainMenuUI : MonoBehaviour
{
    private RectTransform menuPanel;

    private void Awake()
    {
        // This script must be on MenuPanel (a RectTransform under the Canvas)
        menuPanel = GetComponent<RectTransform>();
        if (menuPanel == null)
        {
            Debug.LogError("MainMenuUI must be attached to the MenuPanel.");
        }
    }

    private void Start()
    {
        // Create all four buttons at startup
        CreateButton("ENTER CITY", OnEnterCity);
        CreateButton("CHARACTER", OnCharacter);
        CreateButton("SETTINGS", OnSettings);
        CreateButton("EXIT GAME", OnExitGame);
    }

    // ----- Button creation helper -----
    private Button CreateButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        // Create the button GameObject
        GameObject buttonGO = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(menuPanel, false);

        // Background (simple semi-transparent black box)
        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        // Button component
        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        // Ensure consistent size in the Vertical Layout Group
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 2000f;   // was 250
        layout.preferredHeight = 90f;   // was 60

        // Create text child
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");  // Unity 6 built-in font
        text.fontSize = 40;  // was 24

        // Stretch text to fill the button
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btn;
    }

    // ----- Button callbacks -----
    private void OnEnterCity()
    {
        Debug.Log("ENTER CITY clicked - will load world later.");
    }

    private void OnCharacter()
    {
        Debug.Log("CHARACTER clicked - will open inventory later.");
    }

    private void OnSettings()
    {
        Debug.Log("SETTINGS clicked - will open settings menu later.");
    }

    private void OnExitGame()
    {
        Debug.Log("EXIT GAME clicked - quitting game.");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
