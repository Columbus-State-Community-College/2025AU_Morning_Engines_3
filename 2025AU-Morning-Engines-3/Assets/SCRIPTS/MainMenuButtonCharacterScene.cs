using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterMainMenuButton : MonoBehaviour
{
    public string mainMenuSceneName = "mainMenuScene";

    private void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in CharacterScene.");
            return;
        }

        // Create button
        GameObject buttonGO = new GameObject(
            "MainMenuButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonGO.transform.SetParent(canvas.transform, false);

        // Button visuals
        Image bg = buttonGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        // RectTransform setup
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(30f, 30f);
        rt.sizeDelta = new Vector2(300f, 80f);

        // Button logic
        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(GoToMainMenu);

        // Text
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);

        Text txt = textGO.GetComponent<Text>();
        txt.text = "MAIN MENU";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 36;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
