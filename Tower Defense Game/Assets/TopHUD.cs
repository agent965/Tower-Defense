using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopHUD : MonoBehaviour
{
    public static TopHUD Instance { get; private set; }

    // Per-stat config: drag a sprite into Icon to use a graphic instead of/in addition
    // to the text Label. Leave Label blank if you only want the icon + value.
    [System.Serializable]
    public class StatConfig
    {
        public Sprite icon;             // optional — drag in to show an icon
        public string label = "HP";     // optional — leave blank for icon-only
        public Color  color = Color.white;
        [Tooltip("Tints the icon to match the value color. Disable if your icon already has color.")]
        public bool   tintIcon = true;
        [Tooltip("Pixel size of the icon image.")]
        public float  iconSize = 40f;
    }

    [Header("Bar Style")]
    public float barHeight = 70f;
    public Color barColor       = new Color(0.05f, 0.06f, 0.10f, 1f);
    public Color accentColor    = new Color(1f, 0.82f, 0.25f, 1f);

    [Header("Stats (drag sprites into Icon, blank Label = icon-only)")]
    public StatConfig hp       = new StatConfig { label = "HP",   color = new Color(1f, 0.35f, 0.35f, 1f) };
    public StatConfig gold     = new StatConfig { label = "GOLD", color = new Color(1f, 0.85f, 0.25f, 1f) };
    public StatConfig shield   = new StatConfig { label = "SHLD", color = new Color(0.45f, 0.85f, 1f, 1f) };
    public StatConfig wave     = new StatConfig { label = "WAVE", color = new Color(0.85f, 0.6f, 1f, 1f) };
    public StatConfig enemies  = new StatConfig { label = "LEFT", color = new Color(1f, 0.55f, 0.45f, 1f) };

    [Header("Buy Shield Button")]
    public bool showBuyShield = true;
    public Sprite buyShieldIcon;  // optional

    private Canvas canvas;
    private RectTransform bar;
    private StatSlot hpSlot, goldSlot, shieldSlot, waveSlot, enemiesSlot;
    private Button buyShieldBtn;
    private TextMeshProUGUI buyShieldLabel;

    private int prevHP = -1, prevGold = -1, prevShield = -1;

    // Holds the runtime UI refs for one stat so we can update value/animate
    private class StatSlot
    {
        public StatConfig config;
        public Image icon;
        public TextMeshProUGUI text;
        public RectTransform root;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        BuildUI();
        HideLegacyUI();
    }

    void Start()
    {
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged  += OnHPChanged;
            HealthManager.Instance.ShieldChanged += OnShieldChanged;
            OnHPChanged(HealthManager.Instance.GetCurrentLives());
            OnShieldChanged(HealthManager.Instance.GetCurrentShields());
        }
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.GoldChanged += OnGoldChanged;
            OnGoldChanged(EconomyManager.Instance.GetCurrentGold());
        }
        UpdateWaveDisplay();
    }

    void OnDestroy()
    {
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged  -= OnHPChanged;
            HealthManager.Instance.ShieldChanged -= OnShieldChanged;
        }
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.GoldChanged -= OnGoldChanged;
    }

    void Update()
    {
        UpdateWaveDisplay();
    }

    // ── UI construction ──────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("TopHUDCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Bar background
        GameObject barObj = new GameObject("Bar");
        barObj.transform.SetParent(canvasObj.transform, false);
        bar = barObj.AddComponent<RectTransform>();
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot     = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, barHeight);
        barObj.AddComponent<Image>().color = barColor;

        // Accent stripe
        GameObject stripe = new GameObject("AccentStripe");
        stripe.transform.SetParent(barObj.transform, false);
        RectTransform sr = stripe.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 0f);
        sr.pivot     = new Vector2(0.5f, 0f);
        sr.anchoredPosition = Vector2.zero;
        sr.sizeDelta = new Vector2(0f, 3f);
        stripe.AddComponent<Image>().color = accentColor;

        // 5 stat slots evenly spaced
        hpSlot      = MakeStat(hp,      0);
        goldSlot    = MakeStat(gold,    1);
        shieldSlot  = MakeStat(shield,  2);
        waveSlot    = MakeStat(wave,    3);
        enemiesSlot = MakeStat(enemies, 4);

        if (showBuyShield)
            MakeBuyShieldButton();
    }

    StatSlot MakeStat(StatConfig cfg, int slotIndex)
    {
        const int totalSlots = 5;
        float slotW = 1f / totalSlots;

        GameObject slot = new GameObject($"Slot_{cfg.label}");
        slot.transform.SetParent(bar, false);
        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(slotW * slotIndex, 0f);
        rt.anchorMax = new Vector2(slotW * (slotIndex + 1), 1f);
        rt.offsetMin = new Vector2(8f, 6f);
        rt.offsetMax = new Vector2(-8f, -6f);

        // Layout: icon on left (if set) + text
        HorizontalLayoutGroup hlg = slot.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        StatSlot s = new StatSlot { config = cfg, root = rt };

        // Icon (optional)
        if (cfg.icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = cfg.icon;
            img.preserveAspect = true;
            img.color = cfg.tintIcon ? cfg.color : Color.white;

            LayoutElement le = iconObj.AddComponent<LayoutElement>();
            le.preferredWidth  = cfg.iconSize;
            le.preferredHeight = cfg.iconSize;
            s.icon = img;
        }

        // Text (label + value, or just value if label is blank)
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 32f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        s.text = tmp;

        SetStatValue(s, "--");
        return s;
    }

    void MakeBuyShieldButton()
    {
        GameObject btnObj = new GameObject("BuyShieldBtn");
        btnObj.transform.SetParent(bar, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.40f, 0f);
        rt.anchorMax = new Vector2(0.60f, 0f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta = new Vector2(0f, 28f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.45f, 0.85f, 1f, 0.9f);

        buyShieldBtn = btnObj.AddComponent<Button>();
        buyShieldBtn.targetGraphic = bg;
        buyShieldBtn.onClick.AddListener(() => {
            if (HealthManager.Instance != null) HealthManager.Instance.BuyShield();
        });

        // Layout inside the button
        HorizontalLayoutGroup hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(8, 8, 2, 2);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        if (buyShieldIcon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = buyShieldIcon;
            img.preserveAspect = true;
            LayoutElement le = iconObj.AddComponent<LayoutElement>();
            le.preferredWidth = 22; le.preferredHeight = 22;
        }

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        buyShieldLabel = labelObj.AddComponent<TextMeshProUGUI>();
        int cost = HealthManager.Instance != null ? HealthManager.Instance.shieldCost : 75;
        buyShieldLabel.text = $"Buy Shield ({cost}g)";
        buyShieldLabel.fontSize = 18f;
        buyShieldLabel.color = new Color(0.05f, 0.1f, 0.18f, 1f);
        buyShieldLabel.alignment = TextAlignmentOptions.Center;
        buyShieldLabel.fontStyle = FontStyles.Bold;
        buyShieldLabel.enableWordWrapping = false;
    }

    // ── Hide existing scene-placed UI ────────────────────────────────────

    void HideLegacyUI()
    {
        UIManager um = FindFirstObjectByType<UIManager>();
        if (um == null) return;
        if (um.HealthText)      um.HealthText.gameObject.SetActive(false);
        if (um.MoneyText)       um.MoneyText.gameObject.SetActive(false);
        if (um.ShieldText)      um.ShieldText.gameObject.SetActive(false);
        if (um.WaveText)        um.WaveText.gameObject.SetActive(false);
        if (um.EnemiesText)     um.EnemiesText.gameObject.SetActive(false);
        if (um.BuyShieldButton) um.BuyShieldButton.gameObject.SetActive(false);
    }

    // ── Value updates with flash animation ───────────────────────────────

    void OnHPChanged(int v)
    {
        SetStatValue(hpSlot, v.ToString());
        if (prevHP >= 0 && v != prevHP)
            StartCoroutine(PunchScale(hpSlot.root));
        prevHP = v;
    }

    void OnGoldChanged(int v)
    {
        SetStatValue(goldSlot, v.ToString());
        if (prevGold >= 0 && v != prevGold)
            StartCoroutine(PunchScale(goldSlot.root));
        prevGold = v;
    }

    void OnShieldChanged(int v)
    {
        SetStatValue(shieldSlot, v.ToString());
        if (prevShield >= 0 && v != prevShield)
            StartCoroutine(PunchScale(shieldSlot.root));
        prevShield = v;
    }

    void UpdateWaveDisplay()
    {
        if (WaveManager.Instance == null) return;
        int curWave = WaveManager.Instance.GetCurrentWave();
        int total   = WaveManager.Instance.totalWaves;
        SetStatValue(waveSlot, $"{Mathf.Max(curWave, 1)}/{total}");

        int alive   = WaveManager.Instance.GetEnemiesAlive();
        int toSpawn = WaveManager.Instance.GetEnemiesLeftToSpawn();
        string text = WaveManager.Instance.IsWaveInProgress() ? (alive + toSpawn).ToString() : "—";
        SetStatValue(enemiesSlot, text);
    }

    void SetStatValue(StatSlot s, string value)
    {
        if (s == null || s.text == null) return;
        string colorHex = ColorUtility.ToHtmlStringRGB(s.config.color);
        if (string.IsNullOrEmpty(s.config.label))
        {
            // Icon-only or value-only
            s.text.text = $"<b><color=#{colorHex}>{value}</color></b>";
        }
        else
        {
            s.text.text = $"<color=#FFFFFFAA><size=60%>{s.config.label}</size></color>  <b><color=#{colorHex}>{value}</color></b>";
        }
    }

    IEnumerator PunchScale(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 baseScale = Vector3.one;
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float n = elapsed / duration;
            float scale = 1f + Mathf.Sin(n * Mathf.PI) * 0.15f;
            t.localScale = baseScale * scale;
            yield return null;
        }
        t.localScale = baseScale;
    }
}
