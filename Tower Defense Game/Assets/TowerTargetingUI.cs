using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class TowerTargetingUI : MonoBehaviour
{
    public static TowerTargetingUI Instance { get; private set; }

    private GameObject panel;
    private TextMeshProUGUI titleText;

    // Stats
    private TextMeshProUGUI statsText;

    // Targeting
    private Button firstBtn;
    private Button lastBtn;
    private Button strongestBtn;

    // Upgrade
    private Button upgradeBtn;
    private TextMeshProUGUI upgradeBtnText;

    // Sell
    private Button sellBtn;
    private TextMeshProUGUI sellBtnText;

    private GameObject currentTower;
    private GameObject rangeIndicator;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
        HidePanel();
    }

    void Start()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.GoldChanged += OnGoldChanged;
    }

    void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.GoldChanged -= OnGoldChanged;
    }

    void OnGoldChanged(int _) => RefreshUpgradeButton();

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HidePanel();

        if (currentTower == null)
            HidePanel();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void Show(GameObject tower)
    {
        currentTower = tower;
        RefreshAll();
        ShowRangeIndicator(tower);
        panel.SetActive(true);
    }

    public void Hide() => HidePanel();

    // ── Internal ───────────────────────────────────────────────────────────

    void HidePanel()
    {
        if (panel != null) panel.SetActive(false);
        HideRangeIndicator();
        currentTower = null;
    }

    void RefreshAll()
    {
        RefreshTitle();
        RefreshStats();
        RefreshHighlights();
        RefreshUpgradeButton();
        RefreshSellButton();
    }

    void RefreshTitle()
    {
        if (titleText == null || currentTower == null) return;
        titleText.text = currentTower.name.Replace("Tower", " Tower");
    }

    // ── Stats ──────────────────────────────────────────────────────────────

    void RefreshStats()
    {
        if (statsText == null || currentTower == null) return;

        Tower t = currentTower.GetComponent<Tower>();
        if (t != null)
        {
            statsText.text =
                $"DMG    <color=white>{t.GetDamage()}</color>\n" +
                $"RNG    <color=white>{t.GetRange():0.0}</color>\n" +
                $"CD     <color=white>{t.GetCooldown():0.00}s</color>\n" +
                $"LVL    <color=white>{t.GetUpgradeLevel()} / {(t.IsMaxLevel() ? t.GetUpgradeLevel() : t.GetUpgradeLevel() + 1)}</color>";
            return;
        }

        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null)
        {
            statsText.text =
                $"DMG    <color=white>{(int)mt.damage}</color>\n" +
                $"RNG    <color=white>{mt.range:0.0}</color>\n" +
                $"CD     <color=white>{mt.attackCooldown:0.00}s</color>\n" +
                $"SPLASH <color=white>{mt.splashRadius:0.0}</color>\n" +
                $"LVL    <color=white>{mt.GetUpgradeLevel()} / {(mt.IsMaxLevel() ? mt.GetUpgradeLevel() : mt.GetUpgradeLevel() + 1)}</color>";
        }
    }

    // ── Targeting ──────────────────────────────────────────────────────────

    void SetMode(TargetMode mode)
    {
        if (currentTower == null) return;
        Tower t = currentTower.GetComponent<Tower>();
        if (t != null) t.targetMode = mode;
        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null) mt.targetMode = mode;
        RefreshHighlights();
    }

    TargetMode GetCurrentMode()
    {
        if (currentTower == null) return TargetMode.First;
        Tower t = currentTower.GetComponent<Tower>();
        if (t != null) return t.targetMode;
        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null) return mt.targetMode;
        return TargetMode.First;
    }

    void RefreshHighlights()
    {
        TargetMode mode = GetCurrentMode();
        Highlight(firstBtn,     mode == TargetMode.First);
        Highlight(lastBtn,      mode == TargetMode.Last);
        Highlight(strongestBtn, mode == TargetMode.Strongest);
    }

    void Highlight(Button btn, bool active)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = active
                ? new Color(0.15f, 0.55f, 0.15f, 1f)
                : new Color(0.22f, 0.22f, 0.22f, 1f);
    }

    // ── Upgrade ────────────────────────────────────────────────────────────

    void DoUpgrade()
    {
        if (currentTower == null) return;

        Tower t = currentTower.GetComponent<Tower>();
        if (t != null && !t.IsMaxLevel())
        {
            int cost = t.GetUpgradeCost();
            if (EconomyManager.Instance.SpendGold(cost))
                t.Upgrade();
        }

        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null && !mt.IsMaxLevel())
        {
            int cost = mt.GetUpgradeCost();
            if (EconomyManager.Instance.SpendGold(cost))
                mt.Upgrade();
        }

        RefreshAll();
    }

    void RefreshUpgradeButton()
    {
        if (upgradeBtn == null || currentTower == null) return;

        bool isMax   = false;
        int  cost    = 0;
        string desc  = "";

        Tower t = currentTower.GetComponent<Tower>();
        if (t != null)
        { isMax = t.IsMaxLevel(); cost = t.GetUpgradeCost(); desc = t.GetUpgradeDescription(); }

        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null)
        { isMax = mt.IsMaxLevel(); cost = mt.GetUpgradeCost(); desc = mt.GetUpgradeDescription(); }

        bool canAfford = !isMax && EconomyManager.Instance != null
                         && EconomyManager.Instance.CanAfford(cost);

        upgradeBtn.interactable = !isMax && canAfford;

        Image img = upgradeBtn.GetComponent<Image>();
        if (img != null)
            img.color = isMax
                ? new Color(0.3f, 0.3f, 0.1f, 1f)
                : canAfford
                    ? new Color(0.55f, 0.4f, 0.05f, 1f)
                    : new Color(0.22f, 0.22f, 0.22f, 1f);

        if (upgradeBtnText != null)
            upgradeBtnText.text = isMax
                ? "MAX LEVEL"
                : $"Upgrade  {cost}g\n<size=16>{desc}</size>";
    }

    // ── Sell ───────────────────────────────────────────────────────────────

    void DoSell()
    {
        if (currentTower == null) return;

        Tower t = currentTower.GetComponent<Tower>();
        if (t != null) { TowerPlacer.Instance.SellTower(t); HidePanel(); return; }

        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null) { TowerPlacer.Instance.SellMortarTower(mt); HidePanel(); }
    }

    void RefreshSellButton()
    {
        if (sellBtnText == null || currentTower == null) return;

        int sellVal = 0;
        Tower t = currentTower.GetComponent<Tower>();
        if (t != null) sellVal = (int)t.GetSellValue();

        MortarTower mt = currentTower.GetComponent<MortarTower>();
        if (mt != null) sellVal = (int)mt.GetSellValue();

        sellBtnText.text = $"Sell  {sellVal}g";
    }

    // ── Range Indicator ────────────────────────────────────────────────────

    void ShowRangeIndicator(GameObject tower)
    {
        float range = 0f;
        Tower t = tower.GetComponent<Tower>();
        if (t != null) range = t.GetRange();

        MortarTower mt = tower.GetComponent<MortarTower>();
        if (mt != null) range = mt.range;

        if (range <= 0f) return;

        if (rangeIndicator == null)
            rangeIndicator = CreateRangeCircle();

        rangeIndicator.transform.position = tower.transform.position;
        rangeIndicator.transform.localScale = Vector3.one * range * 2f;
        rangeIndicator.SetActive(true);
    }

    void HideRangeIndicator()
    {
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);
    }

    GameObject CreateRangeCircle()
    {
        GameObject obj = new GameObject("RangeIndicator");
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 99;

        int texSize = 128;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        float center = texSize / 2f;
        float outerR = center;
        float innerR = center - 3f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerR && dist >= innerR)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.6f));
                else if (dist < innerR)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.04f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
        sr.color  = new Color(0.4f, 0.8f, 1f, 1f); // light blue tint

        return obj;
    }

    // ── UI Construction ────────────────────────────────────────────────────

    void BuildUI()
    {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Panel — right side, full height
        panel = new GameObject("TargetingPanel");
        panel.transform.SetParent(transform, false);

        RectTransform panelRT    = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(1f, 0f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(1f, 0.5f);
        panelRT.anchoredPosition = new Vector2(0f, 0f);
        panelRT.sizeDelta        = new Vector2(220f, 0f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.15f, 0.97f);

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRT    = header.AddComponent<RectTransform>();
        headerRT.anchorMin        = new Vector2(0f, 1f);
        headerRT.anchorMax        = new Vector2(1f, 1f);
        headerRT.pivot            = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, 0f);
        headerRT.sizeDelta        = new Vector2(0f, 120f);
        header.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.1f, 1f);

        GameObject titleObj          = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        titleText                    = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize           = 34;
        titleText.fontStyle          = FontStyles.Bold;
        titleText.alignment          = TextAlignmentOptions.Center;
        titleText.color              = Color.white;
        RectTransform titleRT        = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin            = Vector2.zero;
        titleRT.anchorMax            = Vector2.one;
        titleRT.sizeDelta            = Vector2.zero;
        titleRT.anchoredPosition     = Vector2.zero;

        // ── STATS section ─────────────────────────────────────────────────
        MakeSectionLabel(panel, "STATS", new Vector2(0f, -133f));

        GameObject statsObj      = new GameObject("StatsText");
        statsObj.transform.SetParent(panel.transform, false);
        statsText                = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.fontSize       = 22;
        statsText.lineSpacing    = 8f;
        statsText.alignment      = TextAlignmentOptions.Left;
        statsText.color          = new Color(0.7f, 0.7f, 0.7f, 1f);
        RectTransform statsRT    = statsObj.GetComponent<RectTransform>();
        statsRT.anchorMin        = new Vector2(0.5f, 1f);
        statsRT.anchorMax        = new Vector2(0.5f, 1f);
        statsRT.pivot            = new Vector2(0.5f, 1f);
        statsRT.sizeDelta        = new Vector2(200f, 155f);
        statsRT.anchoredPosition = new Vector2(0f, -175f);

        // ── TARGETING section ─────────────────────────────────────────────
        MakeSectionLabel(panel, "TARGETING", new Vector2(0f, -345f));
        firstBtn     = MakeButton(panel, "First",     new Vector2(0f, -390f), () => SetMode(TargetMode.First),     new Color(0.22f, 0.22f, 0.22f, 1f));
        lastBtn      = MakeButton(panel, "Last",      new Vector2(0f, -470f), () => SetMode(TargetMode.Last),      new Color(0.22f, 0.22f, 0.22f, 1f));
        strongestBtn = MakeButton(panel, "Strongest", new Vector2(0f, -550f), () => SetMode(TargetMode.Strongest), new Color(0.22f, 0.22f, 0.22f, 1f));

        // ── UPGRADE section ───────────────────────────────────────────────
        MakeSectionLabel(panel, "UPGRADE", new Vector2(0f, -638f));
        upgradeBtn     = MakeButton(panel, "", new Vector2(0f, -683f), DoUpgrade, new Color(0.55f, 0.4f, 0.05f, 1f));
        upgradeBtnText = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();

        // ── SELL button ───────────────────────────────────────────────────
        MakeSectionLabel(panel, "──────────────────", new Vector2(0f, -765f));
        sellBtn     = MakeButton(panel, "", new Vector2(0f, -805f), DoSell, new Color(0.55f, 0.1f, 0.1f, 1f));
        sellBtnText = sellBtn.GetComponentInChildren<TextMeshProUGUI>();
    }

    void MakeSectionLabel(GameObject parent, string label, Vector2 pos)
    {
        GameObject obj = new GameObject(label + "Label");
        obj.transform.SetParent(parent.transform, false);

        RectTransform rt    = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(200f, 42f);
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    Button MakeButton(GameObject parent, string label, Vector2 pos,
                      UnityEngine.Events.UnityAction onClick, Color baseColor)
    {
        GameObject obj = new GameObject(label + "Btn");
        obj.transform.SetParent(parent.transform, false);

        RectTransform rt    = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(200f, 70f);
        rt.anchoredPosition = pos;

        Image img   = obj.AddComponent<Image>();
        img.color   = baseColor;

        Button btn  = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb       = btn.colors;
        cb.normalColor      = baseColor;
        cb.highlightedColor = baseColor * 1.3f;
        cb.pressedColor     = baseColor * 0.7f;
        cb.disabledColor    = new Color(0.22f, 0.22f, 0.22f, 0.5f);
        btn.colors          = cb;

        GameObject textObj       = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI tmp      = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text                 = label;
        tmp.fontSize             = 22;
        tmp.fontStyle            = FontStyles.Bold;
        tmp.alignment            = TextAlignmentOptions.Center;
        tmp.color                = Color.white;
        RectTransform textRT     = textObj.GetComponent<RectTransform>();
        textRT.anchorMin         = Vector2.zero;
        textRT.anchorMax         = Vector2.one;
        textRT.sizeDelta         = Vector2.zero;
        textRT.anchoredPosition  = Vector2.zero;

        btn.onClick.AddListener(onClick);
        return btn;
    }
}
