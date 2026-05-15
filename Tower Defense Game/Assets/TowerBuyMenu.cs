using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Procedural in-game tower buy menu. Builds a bottom bar of cards, one per
// tower in the player's loadout. Auto-hides the legacy UIManager buy buttons.
public class TowerBuyMenu : MonoBehaviour
{
    public static TowerBuyMenu Instance { get; private set; }

    [Header("Tower Sprites (drag yours in)")]
    public Sprite basicSprite;
    public Sprite sniperSprite;
    public Sprite spraySprite;
    public Sprite rapidSprite;
    public Sprite slowSprite;
    public Sprite mortarSprite;
    public Sprite buffSprite;

    [Header("Layout")]
    public float cardWidth        = 130f;
    public float cardHeight       = 130f;
    public float cardSpacing      = 14f;
    public float barHeight        = 160f;
    public float barBottomPadding = 24f;
    public RectOffset barPadding  = null; // initialized in Awake (RectOffset isn't serializable directly with a default)

    [Header("Style")]
    public Color barColor              = new Color(0.05f, 0.07f, 0.12f, 0.95f);
    public Color cardColor             = new Color(0.20f, 0.24f, 0.32f, 1f);
    public Color cardCantAffordColor   = new Color(0.13f, 0.15f, 0.20f, 0.85f);
    public Color costTextColor         = new Color(1f, 0.85f, 0.25f, 1f);
    public Color cantAffordCostColor   = new Color(1f, 0.4f, 0.35f, 1f);
    public Color iconCantAffordTint    = new Color(0.55f, 0.55f, 0.55f, 0.6f);
    public Color nameTextColor         = Color.white;

    [Header("Behavior")]
    public bool hideLegacyBuyButtons = true;

    private Canvas canvas;
    private List<TowerCardUI> cards = new List<TowerCardUI>();

    private class TowerCardUI
    {
        public TowerPlacer.TowerType type;
        public int cost;
        public GameObject root;
        public Image bg;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Button button;
        public bool canAffordCached = true;
    }

    void Awake()
    {
        Instance = this;
        if (barPadding == null) barPadding = new RectOffset(20, 20, 16, 16);

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        BuildUI();
        if (hideLegacyBuyButtons) HideLegacyButtons();
    }

    void Start()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.GoldChanged += OnGoldChanged;
            OnGoldChanged(EconomyManager.Instance.GetCurrentGold());
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (EconomyManager.Instance != null) EconomyManager.Instance.GoldChanged -= OnGoldChanged;
    }

    // ── UI construction ──────────────────────────────────────────────────

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("TowerBuyMenuCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Bottom bar — sized to fit the loadout's tower count
        var towers = TowerLoadout.Selected;
        int n = towers.Count;
        float barWidth = n * cardWidth + Mathf.Max(0, n - 1) * cardSpacing + barPadding.left + barPadding.right;

        GameObject bar = new GameObject("BottomBar");
        bar.transform.SetParent(canvasObj.transform, false);
        RectTransform brt = bar.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot     = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, barBottomPadding);
        brt.sizeDelta = new Vector2(barWidth, barHeight);

        Image bg = bar.AddComponent<Image>();
        bg.color = barColor;

        HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = cardSpacing;
        hlg.padding = barPadding;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        foreach (var type in towers)
            cards.Add(MakeCard(bar.transform, type));
    }

    TowerCardUI MakeCard(Transform parent, TowerPlacer.TowerType type)
    {
        TowerCardUI ui = new TowerCardUI
        {
            type = type,
            cost = TowerPlacer.GetTowerCost(type)
        };

        GameObject card = new GameObject($"Card_{type}");
        card.transform.SetParent(parent, false);
        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = cardWidth;
        le.preferredHeight = cardHeight;
        ui.root = card;

        ui.bg = card.AddComponent<Image>();
        ui.bg.color = cardColor;

        ui.button = card.AddComponent<Button>();
        ui.button.targetGraphic = ui.bg;
        ColorBlock cb = ui.button.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.fadeDuration     = 0.08f;
        ui.button.colors = cb;
        ui.button.onClick.AddListener(() =>
        {
            if (TowerPlacer.Instance != null)
                TowerPlacer.Instance.StartPlacing(type);
        });

        ButtonHoverScale hover = card.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.06f;

        // Icon (top portion)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 1f); irt.anchorMax = new Vector2(0.5f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0f, -8f);
        irt.sizeDelta = new Vector2(72f, 72f);
        ui.icon = iconObj.AddComponent<Image>();
        ui.icon.sprite = GetSpriteForType(type);
        ui.icon.preserveAspect = true;
        ui.icon.raycastTarget = false;

        // Name (middle)
        ui.nameText = MakeText(card.transform, type.ToString().ToUpper(),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 36f), new Vector2(cardWidth, 20f),
            16f, nameTextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        ui.nameText.outlineWidth = 0.15f;
        ui.nameText.outlineColor = Color.black;

        // Cost (bottom)
        ui.costText = MakeText(card.transform, $"{ui.cost}g",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(cardWidth, 28f),
            22f, costTextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        ui.costText.outlineWidth = 0.15f;
        ui.costText.outlineColor = Color.black;

        return ui;
    }

    TextMeshProUGUI MakeText(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
        float fontSize, Color color, TextAlignmentOptions align, FontStyles style)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Behavior ─────────────────────────────────────────────────────────

    void HideLegacyButtons()
    {
        UIManager um = FindFirstObjectByType<UIManager>();
        if (um == null) return;
        if (um.BuyBasicTowerButton)  um.BuyBasicTowerButton.gameObject.SetActive(false);
        if (um.BuySniperTowerButton) um.BuySniperTowerButton.gameObject.SetActive(false);
        if (um.BuySprayTowerButton)  um.BuySprayTowerButton.gameObject.SetActive(false);
        if (um.BuyRapidTowerButton)  um.BuyRapidTowerButton.gameObject.SetActive(false);
        if (um.BuyMortarTowerButton) um.BuyMortarTowerButton.gameObject.SetActive(false);
    }

    void OnGoldChanged(int gold)
    {
        foreach (var c in cards)
        {
            bool canAfford = gold >= c.cost;
            if (canAfford == c.canAffordCached) continue;

            c.canAffordCached    = canAfford;
            c.bg.color           = canAfford ? cardColor : cardCantAffordColor;
            c.costText.color     = canAfford ? costTextColor : cantAffordCostColor;
            c.icon.color         = canAfford ? Color.white : iconCantAffordTint;
            if (c.button != null) c.button.interactable = canAfford;
        }
    }

    Sprite GetSpriteForType(TowerPlacer.TowerType type)
    {
        switch (type)
        {
            case TowerPlacer.TowerType.Basic:  return basicSprite;
            case TowerPlacer.TowerType.Sniper: return sniperSprite;
            case TowerPlacer.TowerType.Spray:  return spraySprite;
            case TowerPlacer.TowerType.Rapid:  return rapidSprite;
            case TowerPlacer.TowerType.Slow:   return slowSprite;
            case TowerPlacer.TowerType.Mortar: return mortarSprite;
            case TowerPlacer.TowerType.Buff:   return buffSprite;
            default: return null;
        }
    }
}
