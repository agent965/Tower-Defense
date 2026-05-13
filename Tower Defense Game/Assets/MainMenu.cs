using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "Village";

    [Header("Title")]
    public string titleText = "TOWER DEFENSE";
    public Sprite titleSprite;          // optional — drag in to replace text
    public string subtitleText = "";    // optional — small line under title

    [Header("Style")]
    public Color bgColor          = new Color(0.05f, 0.06f, 0.10f, 1f);
    public Color accentColor      = new Color(1f, 0.82f, 0.25f, 1f);
    public Color titleColor       = Color.white;
    public Color buttonBgColor    = new Color(0.15f, 0.18f, 0.25f, 0.95f);
    public Color buttonTextColor  = Color.white;

    [Header("Buttons")]
    public bool showSettings = false;
    public bool showQuit     = true;

    [Header("Background")]
    public bool animatedBackground = true;
    public Color particleColor     = new Color(1f, 1f, 1f, 0.25f);

    private Canvas canvas;
    private RectTransform root;
    private RectTransform titleRoot;
    private RectTransform buttonColumn;
    private static Sprite cachedDotSprite;

    void Awake()
    {
        // Make sure there's an EventSystem so buttons work
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        BuildUI();
    }

    void Start()
    {
        StartCoroutine(EntryAnimation());
        if (animatedBackground) StartCoroutine(SpawnBackgroundParticles());
    }

    // ── UI construction ──────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Root container = full screen
        GameObject rootObj = new GameObject("Root");
        rootObj.transform.SetParent(canvasObj.transform, false);
        root = rootObj.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // Background
        Image bg = rootObj.AddComponent<Image>();
        bg.color = bgColor;

        // Vignette (subtle dark border to focus center)
        BuildVignette(root);

        // Title area
        BuildTitle(root);

        // Button column
        BuildButtons(root);

        // Footer text
        BuildFooter(root);
    }

    void BuildVignette(RectTransform parent)
    {
        // Four dark gradient strips on each edge — fakes a vignette without a shader
        AddEdgeFade(parent, new Vector2(0f, 0f), new Vector2(1f, 0f), 180f, true);  // bottom
        AddEdgeFade(parent, new Vector2(0f, 1f), new Vector2(1f, 1f), 180f, false); // top
    }

    void AddEdgeFade(RectTransform parent, Vector2 aMin, Vector2 aMax, float height, bool fromBottom)
    {
        GameObject fade = new GameObject("EdgeFade");
        fade.transform.SetParent(parent, false);
        RectTransform rt = fade.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot     = new Vector2(0.5f, fromBottom ? 0f : 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
        Image img = fade.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.35f);
        img.raycastTarget = false;
    }

    void BuildTitle(RectTransform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        titleRoot = titleObj.AddComponent<RectTransform>();
        titleRoot.anchorMin = new Vector2(0.5f, 0.78f);
        titleRoot.anchorMax = new Vector2(0.5f, 0.78f);
        titleRoot.pivot     = new Vector2(0.5f, 0.5f);
        titleRoot.sizeDelta = new Vector2(1400f, 250f);

        VerticalLayoutGroup vlg = titleObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 8f;
        vlg.childControlHeight = false;
        vlg.childControlWidth  = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title — sprite if assigned, otherwise text
        if (titleSprite != null)
        {
            GameObject img = new GameObject("TitleSprite");
            img.transform.SetParent(titleObj.transform, false);
            Image si = img.AddComponent<Image>();
            si.sprite = titleSprite;
            si.preserveAspect = true;
            LayoutElement le = img.AddComponent<LayoutElement>();
            le.preferredHeight = 180f;
        }
        else
        {
            GameObject txt = new GameObject("TitleText");
            txt.transform.SetParent(titleObj.transform, false);
            TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text         = titleText;
            tmp.fontSize     = 120f;
            tmp.color        = titleColor;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            LayoutElement le = txt.AddComponent<LayoutElement>();
            le.preferredHeight = 160f;
        }

        // Accent underline
        GameObject under = new GameObject("Underline");
        under.transform.SetParent(titleObj.transform, false);
        RectTransform urt = under.AddComponent<RectTransform>();
        urt.sizeDelta = new Vector2(420f, 5f);
        Image ui = under.AddComponent<Image>();
        ui.color = accentColor;
        LayoutElement ule = under.AddComponent<LayoutElement>();
        ule.preferredHeight = 5f;
        ule.preferredWidth  = 420f;

        // Subtitle
        if (!string.IsNullOrEmpty(subtitleText))
        {
            GameObject sub = new GameObject("Subtitle");
            sub.transform.SetParent(titleObj.transform, false);
            TextMeshProUGUI st = sub.AddComponent<TextMeshProUGUI>();
            st.text         = subtitleText;
            st.fontSize     = 28f;
            st.color        = new Color(1f, 1f, 1f, 0.65f);
            st.alignment    = TextAlignmentOptions.Center;
            st.fontStyle    = FontStyles.Italic;
            LayoutElement sle = sub.AddComponent<LayoutElement>();
            sle.preferredHeight = 36f;
        }
    }

    void BuildButtons(RectTransform parent)
    {
        GameObject col = new GameObject("ButtonColumn");
        col.transform.SetParent(parent, false);
        buttonColumn = col.AddComponent<RectTransform>();
        buttonColumn.anchorMin = new Vector2(0.5f, 0.35f);
        buttonColumn.anchorMax = new Vector2(0.5f, 0.35f);
        buttonColumn.pivot     = new Vector2(0.5f, 0.5f);
        buttonColumn.sizeDelta = new Vector2(420f, 400f);

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 18f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        // Play (primary, larger, pulses)
        MakeButton(col.transform, "PLAY", 90f, accentColor, new Color(0.1f, 0.1f, 0.12f, 1f), true, () =>
        {
            SceneManager.LoadScene(gameSceneName);
        });

        if (showSettings)
        {
            MakeButton(col.transform, "SETTINGS", 70f, buttonBgColor, buttonTextColor, false, () =>
            {
                Debug.Log("MainMenu: Settings clicked (no settings scene yet)");
            });
        }

        if (showQuit)
        {
            MakeButton(col.transform, "QUIT", 70f, buttonBgColor, buttonTextColor, false, () =>
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
        }
    }

    Button MakeButton(Transform parent, string label, float height, Color bg, Color textColor, bool primary, System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);

        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.color = bg;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        // Hover scale + (optional) pulse for primary
        ButtonHoverScale hover = btnObj.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.05f;
        hover.pulse      = primary;

        // Label
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(btnObj.transform, false);
        RectTransform lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text         = label;
        tmp.fontSize     = primary ? 48f : 36f;
        tmp.color        = primary ? new Color(0.1f, 0.1f, 0.12f, 1f) : textColor;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.raycastTarget = false;

        return btn;
    }

    void BuildFooter(RectTransform parent)
    {
        GameObject foot = new GameObject("Footer");
        foot.transform.SetParent(parent, false);
        RectTransform rt = foot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 20f);
        rt.sizeDelta = new Vector2(0f, 30f);

        TextMeshProUGUI tmp = foot.AddComponent<TextMeshProUGUI>();
        tmp.text      = "v0.1   •   Press PLAY to start";
        tmp.fontSize  = 22f;
        tmp.color     = new Color(1f, 1f, 1f, 0.35f);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ── Background particles ─────────────────────────────────────────────

    IEnumerator SpawnBackgroundParticles()
    {
        while (true)
        {
            SpawnDot();
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }

    void SpawnDot()
    {
        if (root == null) return;
        GameObject dot = new GameObject("BgDot");
        dot.transform.SetParent(root, false);
        RectTransform rt = dot.AddComponent<RectTransform>();
        rt.sizeDelta = Vector2.one * Random.Range(4f, 10f);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), -20f);

        Image img = dot.AddComponent<Image>();
        img.sprite = GetDotSprite();
        img.color  = particleColor;
        img.raycastTarget = false;

        dot.AddComponent<BgDot>().Init(Random.Range(40f, 90f), Random.Range(6f, 14f));
        // BgDot self-destructs when off-screen
    }

    static Sprite GetDotSprite()
    {
        if (cachedDotSprite != null) return cachedDotSprite;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float maxR = size / 2f - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = d > maxR ? 0f : (1f - d / maxR);
            a *= a;
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedDotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedDotSprite;
    }

    // ── Entry animation ──────────────────────────────────────────────────

    IEnumerator EntryAnimation()
    {
        // Disable interaction during animation
        CanvasGroup cg = canvas.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;

        // Starting offsets
        Vector3 titleStart = titleRoot.anchoredPosition3D;
        Vector3 btnStart   = buttonColumn.anchoredPosition3D;
        titleRoot.anchoredPosition3D    = titleStart   + new Vector3(0f,  60f, 0f);
        buttonColumn.anchoredPosition3D = btnStart     + new Vector3(0f, -60f, 0f);

        float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float e = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

            cg.alpha = e;
            titleRoot.anchoredPosition3D    = Vector3.Lerp(titleStart + new Vector3(0f,  60f, 0f), titleStart, e);
            buttonColumn.anchoredPosition3D = Vector3.Lerp(btnStart   + new Vector3(0f, -60f, 0f), btnStart,   e);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
    }
}

// Hover scale + optional pulse for the primary button
public class ButtonHoverScale : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    public float hoverScale = 1.05f;
    public bool  pulse;

    private Vector3 baseScale;
    private bool hovered;

    void Start() { baseScale = transform.localScale; }

    void Update()
    {
        float targetScale = hovered ? hoverScale : 1f;
        if (pulse && !hovered)
            targetScale = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) * 0.025f;

        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, Time.unscaledDeltaTime * 12f);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) { hovered = true; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)  { hovered = false; }
}

// Drifts up, fades, self-destructs
public class BgDot : MonoBehaviour
{
    private float speed;
    private float drift;
    private float seed;
    private RectTransform rt;
    private Image img;
    private Color baseColor;

    public void Init(float speed, float drift)
    {
        this.speed = speed;
        this.drift = drift;
        seed = Random.Range(0f, 100f);
    }

    void Start()
    {
        rt  = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (img != null) baseColor = img.color;
    }

    void Update()
    {
        if (rt == null) return;
        Vector2 pos = rt.anchoredPosition;
        pos.y += speed * Time.unscaledDeltaTime;
        pos.x += Mathf.Sin((Time.unscaledTime + seed) * 1.5f) * drift * Time.unscaledDeltaTime;
        rt.anchoredPosition = pos;

        // Fade as it rises past ~80% of screen
        float screenH = Screen.height;
        // Convert anchoredPosition (bottom-anchored center) into a 0..1 vertical fraction
        float frac = pos.y / 1080f;
        if (frac > 0.7f)
        {
            float fade = 1f - Mathf.InverseLerp(0.7f, 1.05f, frac);
            if (img != null) img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * fade);
        }

        if (frac > 1.05f) Destroy(gameObject);
    }
}
