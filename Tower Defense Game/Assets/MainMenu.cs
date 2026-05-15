using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Scene")]
    public string gameSceneName = "Village";

    [Header("Title")]
    public Sprite titleSprite;                   // your "TITAN DEFENSE" art
    public string titleFallbackText = "TOWER DEFENSE";  // only shown if no sprite
    public float  titleWidth  = 780f;
    public float  titleHeight = 340f;

    [Header("Buttons — layout")]
    public float buttonWidth   = 560f;
    public float buttonHeight  = 110f;
    public float buttonSpacing = 18f;
    [Tooltip("Vertical offset of the button column from screen center (negative = down).")]
    public float buttonsYOffset = -120f;

    [Header("Play")]
    public Sprite playSprite;

    [Header("Loadout")]
    public bool   showLoadout = true;
    public Sprite loadoutSprite;

    [Header("Shop")]
    public bool   showShop = true;
    public Sprite shopSprite;

    [Header("Settings")]
    public bool   showSettings = true;
    public Sprite settingsSprite;

    [Header("Quit")]
    public bool   showQuit = true;
    public Sprite quitSprite;

    [Header("Footer (bottom-left)")]
    public string versionText = "v0.1.0";

    [Header("Social Icons (bottom-right)")]
    public SocialButton[] socialButtons = new SocialButton[0];

    [System.Serializable]
    public class SocialButton
    {
        public Sprite icon;
        public string url;
        public Color  tint = Color.white;
    }

    [Header("Animation")]
    public bool playEntryAnimation = false;

    private Canvas canvas;
    private RectTransform root;
    private RectTransform titleRoot;
    private RectTransform buttonColumn;

    void Awake()
    {
        Instance = this;
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Use the new Input System's UI module — the legacy StandaloneInputModule
            // throws an error when the project is set to "Input System Package".
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Called by Loadout (or any modal panel) to hide/show the main menu UI
    public void HideUI() { if (canvas != null) canvas.gameObject.SetActive(false); }
    public void ShowUI() { if (canvas != null) canvas.gameObject.SetActive(true); }

    void Start()
    {
        if (playEntryAnimation) StartCoroutine(EntryAnimation());
    }

    // ── UI construction ──────────────────────────────────────────────────

    void BuildUI()
    {
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

        GameObject rootObj = new GameObject("Root");
        rootObj.transform.SetParent(canvasObj.transform, false);
        root = rootObj.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        BuildTitle(root);
        BuildButtons(root);
        BuildFooter(root);
        BuildSocialButtons(root);
    }

    void BuildTitle(RectTransform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        titleRoot = titleObj.AddComponent<RectTransform>();
        titleRoot.anchorMin = new Vector2(0f, 1f);
        titleRoot.anchorMax = new Vector2(0f, 1f);
        titleRoot.pivot     = new Vector2(0f, 1f);
        titleRoot.anchoredPosition = new Vector2(50f, -30f);
        titleRoot.sizeDelta = new Vector2(titleWidth, titleHeight);

        if (titleSprite != null)
        {
            Image img = titleObj.AddComponent<Image>();
            img.sprite = titleSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else if (!string.IsNullOrEmpty(titleFallbackText))
        {
            TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text         = titleFallbackText;
            tmp.fontSize     = 110f;
            tmp.color        = Color.white;
            tmp.alignment    = TextAlignmentOptions.Left;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;
        }
    }

    void BuildButtons(RectTransform parent)
    {
        GameObject col = new GameObject("ButtonColumn");
        col.transform.SetParent(parent, false);
        buttonColumn = col.AddComponent<RectTransform>();
        buttonColumn.anchorMin = new Vector2(0f, 0.5f);
        buttonColumn.anchorMax = new Vector2(0f, 0.5f);
        buttonColumn.pivot     = new Vector2(0f, 0.5f);
        buttonColumn.anchoredPosition = new Vector2(60f, buttonsYOffset);
        buttonColumn.sizeDelta = new Vector2(buttonWidth, 600f);

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.spacing = buttonSpacing;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        MakeMenuButton(col.transform, "PLAY", playSprite, buttonHeight + 8f, true, () =>
        {
            SceneManager.LoadScene(gameSceneName);
        });

        if (showLoadout)
            MakeMenuButton(col.transform, "LOADOUT", loadoutSprite, buttonHeight, false, () =>
            {
                if (Loadout.Instance != null) Loadout.Instance.Show();
                else Debug.LogWarning("MainMenu: No Loadout component found in the scene.");
            });

        if (showShop)
            MakeMenuButton(col.transform, "SHOP", shopSprite, buttonHeight, false, () =>
            {
                Debug.Log("MainMenu: SHOP clicked");
            });

        if (showSettings)
            MakeMenuButton(col.transform, "SETTINGS", settingsSprite, buttonHeight, false, () =>
            {
                Debug.Log("MainMenu: SETTINGS clicked");
            });

        if (showQuit)
            MakeMenuButton(col.transform, "QUIT", quitSprite, buttonHeight, false, () =>
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
    }

    void MakeMenuButton(Transform parent, string fallbackLabel, Sprite sprite, float height, bool primary, System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{fallbackLabel}");
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }
        else
        {
            // Fallback flat rectangle so the button is still visible/clickable
            img.color = new Color(0.18f, 0.20f, 0.26f, 1f);
        }

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.78f, 0.78f, 0.78f, 1f);  // darker on hover
        cb.pressedColor     = new Color(0.60f, 0.60f, 0.60f, 1f);  // even darker on press
        cb.fadeDuration     = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        ButtonHoverScale hover = btnObj.AddComponent<ButtonHoverScale>();
        hover.hoverScale = 1.04f;
        hover.pulse      = primary;

        // Show the fallback label text only when no sprite is provided
        if (sprite == null)
        {
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            RectTransform lrt = lblObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = lblObj.AddComponent<TextMeshProUGUI>();
            tmp.text         = fallbackLabel;
            tmp.fontSize     = primary ? 44f : 36f;
            tmp.color        = Color.white;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;
        }
    }

    void BuildFooter(RectTransform parent)
    {
        if (string.IsNullOrEmpty(versionText)) return;
        GameObject foot = new GameObject("Version");
        foot.transform.SetParent(parent, false);
        RectTransform rt = foot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(30f, 20f);
        rt.sizeDelta = new Vector2(200f, 30f);

        TextMeshProUGUI tmp = foot.AddComponent<TextMeshProUGUI>();
        tmp.text         = versionText;
        tmp.fontSize     = 22f;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.BottomLeft;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        tmp.raycastTarget = false;
    }

    void BuildSocialButtons(RectTransform parent)
    {
        if (socialButtons == null || socialButtons.Length == 0) return;

        GameObject row = new GameObject("SocialRow");
        row.transform.SetParent(parent, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-30f, 25f);
        rt.sizeDelta = new Vector2(64f * socialButtons.Length + 16f * (socialButtons.Length - 1), 64f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.spacing = 12f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        foreach (SocialButton s in socialButtons)
        {
            if (s == null || s.icon == null) continue;

            GameObject b = new GameObject("Social");
            b.transform.SetParent(row.transform, false);
            Image im = b.AddComponent<Image>();
            im.sprite = s.icon;
            im.color  = s.tint;
            im.preserveAspect = true;

            LayoutElement le = b.AddComponent<LayoutElement>();
            le.preferredWidth  = 64f;
            le.preferredHeight = 64f;

            Button btn = b.AddComponent<Button>();
            btn.targetGraphic = im;
            string url = s.url;
            btn.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
            });

            ButtonHoverScale h = b.AddComponent<ButtonHoverScale>();
            h.hoverScale = 1.15f;
        }
    }

    // ── Entry animation (optional) ───────────────────────────────────────

    IEnumerator EntryAnimation()
    {
        CanvasGroup cg = canvas.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;

        Vector3 titleStart = titleRoot.anchoredPosition3D;
        Vector3 btnStart   = buttonColumn.anchoredPosition3D;
        titleRoot.anchoredPosition3D    = titleStart   + new Vector3(0f,  40f, 0f);
        buttonColumn.anchoredPosition3D = btnStart     + new Vector3(-60f, 0f, 0f);

        float duration = 0.55f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float e = 1f - Mathf.Pow(1f - t, 3f);

            cg.alpha = e;
            titleRoot.anchoredPosition3D    = Vector3.Lerp(titleStart + new Vector3(0f,  40f, 0f), titleStart, e);
            buttonColumn.anchoredPosition3D = Vector3.Lerp(btnStart   + new Vector3(-60f, 0f, 0f), btnStart,   e);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
    }
}

// Add this to any UI Button GameObject for smooth hover scale-up + press-down.
// Toggle 'pulse' for a gentle idle bounce.
public class ButtonHoverScale : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IPointerUpHandler
{
    public float hoverScale = 1.05f;
    public bool  pulse;
    [Tooltip("How far (pixels) the button shifts down when held. Negative = down.")]
    public float pressOffsetY = -6f;

    private Vector3 baseScale = Vector3.one;
    private Vector3 basePosition;
    private float currentOffsetY;
    private bool initialized;
    private bool hovered;
    private bool pressed;

    System.Collections.IEnumerator Start()
    {
        // Wait one frame so the parent LayoutGroup can assign our position first.
        yield return null;
        baseScale    = transform.localScale;
        basePosition = transform.localPosition;
        initialized  = true;
    }

    void Update()
    {
        if (!initialized) return;

        // Hover scale (with optional idle pulse on primary)
        float targetScale = hovered ? hoverScale : 1f;
        if (pulse && !hovered)
            targetScale = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) * 0.025f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, Time.unscaledDeltaTime * 12f);

        // Press-down offset
        float target = pressed ? pressOffsetY : 0f;
        currentOffsetY = Mathf.Lerp(currentOffsetY, target, Time.unscaledDeltaTime * 22f);
        transform.localPosition = basePosition + new Vector3(0f, currentOffsetY, 0f);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) { hovered = true; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
    {
        hovered = false;
        pressed = false; // drag-off shouldn't leave it stuck pressed
    }
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { pressed = true; }
    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)   { pressed = false; }
}
