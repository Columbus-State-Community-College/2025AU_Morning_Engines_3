using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CharacterUpgradesUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Vector2 panelSize = new Vector2(1250f, 620f);
    [SerializeField] private bool showPointsHeader = false;

    [Header("Icons (Sprites)")]
    [SerializeField] private Sprite damageIcon;
    [SerializeField] private Sprite pelletsIcon;
    [SerializeField] private Sprite healthIcon;

    [Header("Sizing")]
    [SerializeField] private float iconSize = 150f;

    private TextMeshProUGUI pointsText;

    private TextMeshProUGUI dmgStatText;
    private TextMeshProUGUI pelStatText;
    private TextMeshProUGUI hpStatText;

    private Button dmgButton;
    private Button pelButton;
    private Button hpButton;

    private void Awake()
    {
        UpgradePointsManager.GetPoints();
        UpgradeStatsManager.EnsureExists();
        EnsureEventSystem();
    }

    private void OnEnable()
    {
        UpgradePointsManager.OnPointsChanged += HandlePointsChanged;
        UpgradeStatsManager.OnUpgradesChanged += HandleUpgradesChanged;
    }

    private void OnDisable()
    {
        UpgradePointsManager.OnPointsChanged -= HandlePointsChanged;
        UpgradeStatsManager.OnUpgradesChanged -= HandleUpgradesChanged;
    }

    private void Start()
    {
        BuildUI();
        RefreshUI();
    }

    private void HandlePointsChanged(int _) => RefreshUI();
    private void HandleUpgradesChanged() => RefreshUI();

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void BuildUI()
    {
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            GameObject c = new GameObject("CharacterUpgradesCanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            c.AddComponent<GraphicRaycaster>();
        }

        // MAIN PANEL
        GameObject panelObj = new GameObject("UpgradesPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        panelObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = panelSize;

        VerticalLayoutGroup panelVlg = panelObj.AddComponent<VerticalLayoutGroup>();
        panelVlg.padding = new RectOffset(40, 40, 40, 40);
        panelVlg.spacing = 30;
        panelVlg.childAlignment = TextAnchor.UpperCenter;
        panelVlg.childControlHeight = true;
        panelVlg.childControlWidth = true;
        panelVlg.childForceExpandHeight = false;

        // TITLE
        TextMeshProUGUI title = CreateTMP(panelObj.transform, "UPGRADES", 50, TextAlignmentOptions.Center);
        title.GetComponent<LayoutElement>().preferredHeight = 80f;

        if (showPointsHeader)
        {
            pointsText = CreateTMP(panelObj.transform, "", 28, TextAlignmentOptions.Center);
            pointsText.GetComponent<LayoutElement>().preferredHeight = 55f;
        }

        // ROW
        GameObject rowObj = new GameObject("UpgradeRow", typeof(RectTransform));
        rowObj.transform.SetParent(panelObj.transform, false);

        LayoutElement rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 420f;

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        CreateUpgradeCard(rowObj.transform, damageIcon, "DAMAGE", "+0.5 per upgrade",
            out dmgStatText, out dmgButton, () => UpgradeStatsManager.TryBuyDamageUpgrade(1));

        CreateUpgradeCard(rowObj.transform, pelletsIcon, "PELLETS", "+1 pellet per upgrade",
            out pelStatText, out pelButton, () => UpgradeStatsManager.TryBuyPelletsUpgrade(1));

        CreateUpgradeCard(rowObj.transform, healthIcon, "HEALTH", "+10 max health per upgrade",
            out hpStatText, out hpButton, () => UpgradeStatsManager.TryBuyHealthUpgrade(1));

        // FOOTER
        TextMeshProUGUI footer = CreateTMP(panelObj.transform, "Cost: 1 Upgrade Point each.", 22, TextAlignmentOptions.Center);
        footer.GetComponent<LayoutElement>().preferredHeight = 45f;
    }

    private void RefreshUI()
    {
        int points = UpgradePointsManager.GetPoints();
        if (pointsText != null) pointsText.text = $"Unspent Points: {points}";

        int dmgLv = UpgradeStatsManager.Instance?.DamageUpgrades ?? 0;
        int pelLv = UpgradeStatsManager.Instance?.PelletsUpgrades ?? 0;
        int hpLv  = UpgradeStatsManager.Instance?.HealthUpgrades ?? 0;

        dmgStatText.text = $"Level: {dmgLv}\nBonus: +{UpgradeStatsManager.GetDamageBonus():0.0} dmg/pellet";
        pelStatText.text = $"Level: {pelLv}\nBonus: +{UpgradeStatsManager.GetPelletsBonus()} pellets/shot";
        hpStatText.text  = $"Level: {hpLv}\nBonus: +{UpgradeStatsManager.GetHealthBonus():0} max HP";

        bool canBuy = points > 0;
        dmgButton.interactable = pelButton.interactable = hpButton.interactable = canBuy;
    }

    private void CreateUpgradeCard(
        Transform parent,
        Sprite icon,
        string title,
        string desc,
        out TextMeshProUGUI statText,
        out Button button,
        System.Action onClick)
    {
        GameObject card = new GameObject(title + "Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);

        card.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.13f, 0.9f);

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 24, 24);
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandHeight = false;

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 420f;

        GameObject iconObj = new GameObject("Icon", typeof(Image), typeof(LayoutElement));
        iconObj.transform.SetParent(card.transform, false);
        iconObj.GetComponent<Image>().sprite = icon;
        iconObj.GetComponent<Image>().preserveAspect = true;

        LayoutElement iconLE = iconObj.GetComponent<LayoutElement>();
        iconLE.preferredHeight = iconSize;
        iconLE.preferredWidth = iconSize;

        TextMeshProUGUI titleTMP = CreateTMP(card.transform, title, 36, TextAlignmentOptions.Center);
        titleTMP.GetComponent<LayoutElement>().preferredHeight = 50f;

        TextMeshProUGUI descTMP = CreateTMP(card.transform, desc, 24, TextAlignmentOptions.Center);
        descTMP.GetComponent<LayoutElement>().preferredHeight = 48f;

        statText = CreateTMP(card.transform, "", 23, TextAlignmentOptions.Center);
        statText.GetComponent<LayoutElement>().preferredHeight = 90f;

        GameObject btnObj = new GameObject("UpgradeButton", typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(card.transform, false);

        btnObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        btnObj.GetComponent<LayoutElement>().preferredHeight = 64f;

        button = btnObj.GetComponent<Button>();
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI btnText = CreateTMP(btnObj.transform, "UPGRADE (1)", 26, TextAlignmentOptions.Center);
        btnText.rectTransform.anchorMin = Vector2.zero;
        btnText.rectTransform.anchorMax = Vector2.one;
        btnText.rectTransform.offsetMin = new Vector2(12, 8);
        btnText.rectTransform.offsetMax = new Vector2(-12, -8);
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("TMPText", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;

        go.AddComponent<LayoutElement>().preferredHeight = size * 1.8f;
        return tmp;
    }
}
