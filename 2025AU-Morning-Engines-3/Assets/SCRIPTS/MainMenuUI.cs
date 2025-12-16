using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private RectTransform menuPanel;

    private void Awake()
    {
        menuPanel = GetComponent<RectTransform>();
        if (menuPanel == null)
        {
            Debug.LogError("MainMenuUI must be attached to the MenuPanel.");
        }
    }

    private void Start()
    {
        CreateButton("ENTER CITY", OnEnterCity);
        CreateButton("CHARACTER", OnCharacter);
        CreateButton("SETTINGS", OnSettings);
        CreateButton("EXIT GAME", OnExitGame);
    }

    private Button CreateButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(
            label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonGO.transform.SetParent(menuPanel, false);

        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 2000f;
        layout.preferredHeight = 90f;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 40;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btn;
    }

    private void OnEnterCity()
    {
        SceneManager.LoadScene("MAINgameScene");
    }

    private void OnCharacter()
    {
        SceneManager.LoadScene("CharacterScene");
    }

    private void OnSettings()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    private void OnExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
