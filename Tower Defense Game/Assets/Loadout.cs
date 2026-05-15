using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Loadout : MonoBehaviour
{
    public static Loadout Instance { get; private set; }

    [Header("Tower Icons (drag your sprites in)")]
    public Sprite basicIcon;
    public Sprite sniperIcon;
    public Sprite sprayIcon;
    public Sprite rapidIcon;
    public Sprite slowIcon;
    public Sprite mortarIcon;
    public Sprite buffIcon;

    [Header("Selection Rules")]
    public int maxSlots = 5;

    [Header("Background dimming (fakes a blur)")]
    [Tooltip("Name of the scene GameObject holding the background SpriteRenderer.")]
    public string backgroundObjectName = "Background";
    [Tooltip("Color the background sprite is multiplied by when the loadout is open. Darker grey ≈ more 'blur' feel.")]
    public Color backgroundDimColor = new Color(0.35f, 0.38f, 0.45f, 1f);
    [Tooltip("Color of the full-screen overlay drawn between the background and the loadout UI.")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Style")]
    public Color accentColor       = new Color(1f, 0.82f, 0.25f, 1f);
    public Color cardColor         = new Color(0.16f, 0.20f, 0.28f, 0.96f);
    public Color cardSelectedColor = new Color(0.30f, 0.55f, 0.40f, 1f);
    public Color slotEmptyColor    = new Color(0.10f, 0.13f, 0.18f, 0.85f);
    public Color slotFilledColor   = new Color(0.20f, 0.32f, 0.50f, 1f);
    public Color confirmColor      = new Color(0.42f, 0.78f, 0.30f, 1f);
    public Color backButtonColor   = new Color(0.55f, 0.55f, 0.60f, 1f);

    private Canvas canvas;
    private GameObject overlay;
    private TextMeshProUGUI countLabel;

    private SpriteRenderer cachedBackgroundSR;
    private Color cachedBackgroundColor;

    private class TowerCardUI
    {
        public TowerPlacer.TowerType type;
        public GameObject root;
        public Image background;
        public GameObject selectedBorder;
    }
    private class SlotUI
    {
        public GameObject root;
        public Image bg;
        public Image iconImg;
        public TextMeshProUGUI label;
    }

    private List<TowerCardUI> cards = new List<TowerCardUI>();
    private List<SlotUI> slots = new List<SlotUI>();

    void Awake()
    {
        Instance = this;
        TowerLoadout.maxSlots = maxSlots;

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Find the existing Background GameObject so we can dim it on Show()
        GameObject bg = GameObject.Find(backgroundObjectName);
        if (bg != null)
        {
            cachedBackgroundSR = bg.GetComponent<SpriteRenderer>();
            if (cachedBackgroundSR != null) cachedBackgroundColor = cachedBackgroundSR.color;
        }

        BuildUI();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ───────────────────────────────────────────────────────

    public void Show()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);
        if (MainMenu.Instance != null) MainMenu.Instance.HideUI();
        if (cachedBackgroundSR != null) cachedBackgroundSR.color = backgroundDimColor;
        RefreshAll();
    }

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (MainMenu.Instance != null) MainMenu.Instance.ShowUI();
        if (cachedBackgroundSR != null) cachedBackgroundSR.color = cachedBackgroundColor;
    }

    // ── UI construction ──────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("LoadoutCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform root = new GameObject("Root").AddComponent<RectTransform>();
        root.SetParent(canvasObj.transform, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

        // Dim overlay over the (already-darkened) background
        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(root, false);
        RectTransform ort = overlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        Image oi = overlay.AddComponent<Image>();
        oi.color = overlayColor;
        oi.raycastTarget = true; // block clicks on what's underneath

        // ── Title block ──────────────────────────────────────────────────
        var title = MakeText(root, "LOADOUT",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -80f), new Vector2(900f, 100f),
            84f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        title.outlineWidth = 0.18f; title.outlineColor = Color.black;

        // Accent underline
        GameObject under = new GameObject("Underline");
        under.transform.SetParent(root, false);
        RectTransform urt = under.AddComponent<RectTransform>();
        urt.anchorMin = new Vector2(0.5f, 1f); urt.anchorMax = new Vector2(0.5f, 1f);
        urt.pivot = new Vector2(0.5f, 1f);
        urt.anchoredPosition = new Vector2(0f, -160f);
        urt.sizeDelta = new Vector2(180f, 5f);
        Image ui = under.AddComponent<Image>(); ui.color = accentColor;

        MakeText(root, "Choose which towers to bring into battle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -180f), new Vector2(900f, 40f),
            26f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center, FontStyles.Italic);

        countLabel = MakeText(root, "",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -222f), new Vector2(900f, 40f),
            28f, accentColor, TextAlignmentOptions.Center, FontStyles.Bold);

        // ── Available towers grid ────────────────────────────────────────
        MakeText(root, "AVAILABLE",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -280f), new Vector2(400f, 30f),
            20f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);

        GameObject grid = new GameObject("AvailableGrid");
        grid.transform.SetParent(root, false);
        RectTransform grt = grid.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 1f); grt.anchorMax = new Vector2(0.5f, 1f);
        grt.pivot = new Vector2(0.5f, 1f);
        grt.anchoredPosition = new Vector2(0f, -320f);
        grt.sizeDelta = new Vector2(1200f, 380f);
        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(170f, 170f);
        glg.spacing  = new Vector2(20f, 20f);
        glg.padding  = new RectOffset(10, 10, 10, 10);
        glg.childAlignment = TextAnchor.UpperCenter;

        foreach (TowerPlacer.TowerType t in System.Enum.GetValues(typeof(TowerPlacer.TowerType)))
            cards.Add(MakeCard(grid.transform, t));

        // ── Your loadout slot row ────────────────────────────────────────
        MakeText(root, "YOUR LOADOUT",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 270f), new Vector2(400f, 30f),
            20f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);

        GameObject slotRow = new GameObject("SlotRow");
        slotRow.transform.SetParent(root, false);
        RectTransform srt = slotRow.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0f); srt.anchorMax = new Vector2(0.5f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 140f);
        srt.sizeDelta = new Vector2(1100f, 130f);
        HorizontalLayoutGroup hlg = slotRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 22f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        for (int i = 0; i < maxSlots; i++)
            slots.Add(MakeSlot(slotRow.transform, i));

        // ── Bottom buttons ───────────────────────────────────────────────
        MakeBottomButton(root, "BACK",    backButtonColor,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(60f, 50f), () => Hide());

        MakeBottomButton(root, "CONFIRM", confirmColor,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-60f, 50f), () => { TowerLoadout.Save(); Hide(); });
    }

    TowerCardUI MakeCard(Transform parent, TowerPlacer.TowerType type)
    {
        TowerCardUI ui = new TowerCardUI { type = type };

        GameObject card = new GameObject($"Card_{type}");
        card.transform.SetParent(parent, false);
        ui.root = card;

        ui.background = card.AddComponent<Image>();
        ui.background.color = cardColor;

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = ui.background;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => OnCardClicked(type));

        ButtonHoverScale hover = card.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.06f;

        // Selected accent — thin colored stripe at the top, hidden when not selected
        GameObject sel = new GameObject("SelectedBar");
        sel.transform.SetParent(card.transform, false);
        RectTransform srt = sel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(0f, 6f);
        Image si = sel.AddComponent<Image>(); si.color = accentColor; si.raycastTarget = false;
        ui.selectedBorder = sel;

        // Icon (centered, big)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 1f); irt.anchorMax = new Vector2(0.5f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0f, -15f);
        irt.sizeDelta = new Vector2(110f, 110f);
        Image im = iconObj.AddComponent<Image>();
        im.sprite = GetIconForType(type);
        im.preserveAspect = true;
        im.raycastTarget = false;

        // Name
        var label = MakeText(card.transform, type.ToString().ToUpper(),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(160f, 28f),
            20f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        label.outlineWidth = 0.18f; label.outlineColor = Color.black;

        return ui;
    }

    SlotUI MakeSlot(Transform parent, int index)
    {
        SlotUI ui = new SlotUI();

        GameObject slot = new GameObject($"Slot_{index}");
        slot.transform.SetParent(parent, false);
        LayoutElement le = slot.AddComponent<LayoutElement>();
        le.preferredWidth  = 120f;
        le.preferredHeight = 120f;
        ui.root = slot;

        ui.bg = slot.AddComponent<Image>();
        ui.bg.color = slotEmptyColor;

        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = ui.bg;
        btn.onClick.AddListener(() => OnSlotClicked(index));

        ButtonHoverScale hover = slot.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.05f;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = new Vector2(0f, 10f);
        irt.sizeDelta = new Vector2(70f, 70f);
        ui.iconImg = iconObj.AddComponent<Image>();
        ui.iconImg.preserveAspect = true;
        ui.iconImg.raycastTarget = false;

        // Label
        ui.label = MakeText(slot.transform, "EMPTY",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(120f, 22f),
            16f, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.Center, FontStyles.Bold);

        return ui;
    }

    void MakeBottomButton(Transform parent, string label, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos,
        System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(220f, 70f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.pressedColor     = new Color(0.62f, 0.62f, 0.62f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        ButtonHoverScale hover = btnObj.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.05f;

        var tmp = MakeText(btnObj.transform, label,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            32f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        tmp.outlineWidth = 0.2f; tmp.outlineColor = Color.black;
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
        tmp.text     = text;
        tmp.fontSize = fontSize;
        tmp.color    = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Behavior ─────────────────────────────────────────────────────────

    void OnCardClicked(TowerPlacer.TowerType type)
    {
        if (TowerLoadout.IsSelected(type))
            TowerLoadout.Remove(type);
        else
            TowerLoadout.Add(type);
        RefreshAll();
    }

    void OnSlotClicked(int index)
    {
        if (index >= TowerLoadout.Selected.Count) return;
        TowerLoadout.Remove(TowerLoadout.Selected[index]);
        RefreshAll();
    }

    void RefreshAll()
    {
        if (countLabel != null)
            countLabel.text = $"{TowerLoadout.Selected.Count} / {maxSlots} selected";

        foreach (TowerCardUI c in cards)
        {
            bool selected = TowerLoadout.IsSelected(c.type);
            c.background.color = selected ? cardSelectedColor : cardColor;
            if (c.selectedBorder != null) c.selectedBorder.SetActive(selected);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            SlotUI s = slots[i];
            if (i < TowerLoadout.Selected.Count)
            {
                TowerPlacer.TowerType t = TowerLoadout.Selected[i];
                s.iconImg.sprite = GetIconForType(t);
                s.iconImg.enabled = true;
                s.label.text = t.ToString().ToUpper();
                s.bg.color = slotFilledColor;
            }
            else
            {
                s.iconImg.enabled = false;
                s.label.text = "EMPTY";
                s.bg.color = slotEmptyColor;
            }
        }
    }

    Sprite GetIconForType(TowerPlacer.TowerType type)
    {
        switch (type)
        {
            case TowerPlacer.TowerType.Basic:  return basicIcon;
            case TowerPlacer.TowerType.Sniper: return sniperIcon;
            case TowerPlacer.TowerType.Spray:  return sprayIcon;
            case TowerPlacer.TowerType.Rapid:  return rapidIcon;
            case TowerPlacer.TowerType.Slow:   return slowIcon;
            case TowerPlacer.TowerType.Mortar: return mortarIcon;
            case TowerPlacer.TowerType.Buff:   return buffIcon;
            default: return null;
        }
    }
}
