using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [Header("Placement Settings")]
    public LayerMask placementBlockedLayer;
    public float placementCheckRadius = 0.4f;

    [Header("Tower Sprites")]
    public Sprite basicTowerSprite;
    public Sprite sniperTowerSprite;
    public Sprite sprayTowerSprite;
    public Sprite rapidTowerSprite;
    public Sprite buffTowerSprite;

    // Tower type definitions
    public enum TowerType { Basic, Sniper, Spray, Rapid, Slow, Mortar, Buff }

    private bool isPlacing = false;
    private TowerType currentTowerType;
    private GameObject preview;
    private GameObject rangeIndicator;
    private SpriteRenderer previewRenderer;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Auto-create the targeting UI singleton if it's not in the scene
        if (TowerTargetingUI.Instance == null)
        {
            GameObject uiObj = new GameObject("TowerTargetingUI");
            uiObj.AddComponent<TowerTargetingUI>();
        }
    }

    void Update()
    {
        // Tower selection click (only when not in placement mode)
        if (!isPlacing && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Ignore clicks that land on a UI element (e.g. targeting buttons)
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Vector2 clickScreen = Mouse.current.position.ReadValue();
                Vector3 clickWorld  = Camera.main.ScreenToWorldPoint(new Vector3(clickScreen.x, clickScreen.y, 0f));
                clickWorld.z = 0f;

                RaycastHit2D hit = Physics2D.Raycast(clickWorld, Vector2.zero);
                if (hit.collider != null && hit.collider.GetComponent<TowerClickHandler>() != null)
                    TowerTargetingUI.Instance?.Show(hit.collider.gameObject);
                else
                    TowerTargetingUI.Instance?.Hide();
            }
        }

        if (!isPlacing) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld.z = 0f;

        if (preview != null)
            preview.transform.position = mouseWorld;

        // Update preview color based on placement validity
        UpdatePreviewColor(mouseWorld);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlace(mouseWorld);
        }

        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    // --- Tower stat definitions ---

    public static int GetTowerCost(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return 100;
            case TowerType.Sniper: return 200;
            case TowerType.Spray:  return 150;
            case TowerType.Rapid:  return 125;
            case TowerType.Slow:   return 125;
            case TowerType.Mortar: return 175;
            case TowerType.Buff:   return 150;
            default: return 100;
        }
    }

    private static Sprite cachedMortarPreview;

    private Sprite GetTowerSprite(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return basicTowerSprite;
            case TowerType.Sniper: return sniperTowerSprite;
            case TowerType.Spray:  return sprayTowerSprite;
            case TowerType.Rapid:  return rapidTowerSprite;
            case TowerType.Slow:   return rapidTowerSprite;
            case TowerType.Mortar: return LoadMortarPreview();
            case TowerType.Buff:   return buffTowerSprite;
            default: return basicTowerSprite;
        }
    }

    // MortarTower loads its runtime sprites from Resources/ at PPU 128.
    // Match that here so the preview size equals the placed tower's size.
    private static Sprite LoadMortarPreview()
    {
        if (cachedMortarPreview != null) return cachedMortarPreview;
        Texture2D tex = Resources.Load<Texture2D>("Mortar/Lv1/Idle");
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        cachedMortarPreview = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            128f
        );
        return cachedMortarPreview;
    }

    private static float GetTowerRange(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return 3f;
            case TowerType.Sniper: return 6f;
            case TowerType.Spray:  return 2.5f;
            case TowerType.Rapid:  return 2.5f;
            case TowerType.Slow:   return 2.5f;
            case TowerType.Mortar: return 4f;
            case TowerType.Buff:   return 3f;
            default: return 3f;
        }
    }

    private void InitTower(Tower towerScript, TowerType type)
    {
        int cost = GetTowerCost(type);
        switch (type)
        {
            case TowerType.Basic:
                towerScript.init_Tower(10, 1, 3f, 8.0, 1.0, cost, cost / 2, 1, true, "none");
                towerScript.SetUpgrades(new TowerUpgradeData[]
                {
                    new TowerUpgradeData { cost = 75,  dmgAdd = 5,  rangeMult = 1.1f, cooldownMult = 1f,    description = "+5 DMG  +10% RNG" },
                    new TowerUpgradeData { cost = 100, dmgAdd = 10, rangeMult = 1f,   cooldownMult = 0.85f, description = "+10 DMG  -15% CD" },
                });
                break;
            case TowerType.Sniper:
                towerScript.init_Tower(50, 3, 6f, 15.0, 3.0, cost, cost / 2, 1, true, "Poison");
                towerScript.SetUpgrades(new TowerUpgradeData[]
                {
                    new TowerUpgradeData { cost = 125, dmgAdd = 40, rangeMult = 1.15f, cooldownMult = 1f, description = "+40 DMG  +15% RNG" },
                    new TowerUpgradeData { cost = 175, dmgAdd = 60, rangeMult = 1.1f,  cooldownMult = 1f, description = "+60 DMG  +10% RNG" },
                });
                break;
            case TowerType.Spray:
                towerScript.init_Tower(5, 1, 2.5f, 6.0, 1.5, cost, cost / 2, 6, false, "none");
                towerScript.SetUpgrades(new TowerUpgradeData[]
                {
                    new TowerUpgradeData { cost = 80,  dmgAdd = 3, rangeMult = 1f,   cooldownMult = 0.9f,  description = "+3 DMG  -10% CD" },
                    new TowerUpgradeData { cost = 110, dmgAdd = 5, rangeMult = 1.1f, cooldownMult = 0.85f, description = "+5 DMG  +10% RNG  -15% CD" },
                });
                break;
            case TowerType.Rapid:
                towerScript.init_Tower(4, 1, 2.5f, 10.0, 0.3, cost, cost / 2, 1, true, "Slow");
                towerScript.SetUpgrades(new TowerUpgradeData[]
                {
                    new TowerUpgradeData { cost = 60, dmgAdd = 10, rangeMult = 1f, cooldownMult = 0.8f,  description = "+10 DMG  -20% CD" },
                    new TowerUpgradeData { cost = 90, dmgAdd = 10, rangeMult = 1f, cooldownMult = 0.75f, description = "+10 DMG  -25% CD" },
                });
                break;
            case TowerType.Slow:
                towerScript.init_Tower(4, 1, 2.5f, 10.0, 0.3, cost, cost / 2, 1, true, "Slow");
                towerScript.SetUpgrades(new TowerUpgradeData[]
                {
                    new TowerUpgradeData { cost = 70,  dmgAdd = 2, rangeMult = 1.1f, cooldownMult = 1f, description = "+2 DMG  +10% RNG" },
                    new TowerUpgradeData { cost = 100, dmgAdd = 3, rangeMult = 1.1f, cooldownMult = 1f, description = "+3 DMG  +10% RNG" },
                });
                break;
        }
    }

    private void InitMortarTower(MortarTower mortar)
    {
        int cost = GetTowerCost(TowerType.Mortar);
        mortar.Init(30f, 1.2f, 4f, 2.5f, cost, cost / 2, MortarTower.TowerLevel.Level1);
        mortar.SetUpgrades(new TowerUpgradeData[]
        {
            new TowerUpgradeData { cost = 130, dmgAdd = 100, rangeMult = 1f,   cooldownMult = 1f,   splashAdd = 0.3f, description = "+100 DMG  +0.3 Splash" },
            new TowerUpgradeData { cost = 175, dmgAdd = 50,  rangeMult = 1.1f, cooldownMult = 0.8f, splashAdd = 0f,   description = "+50 DMG  +10% RNG  -20% CD" },
        });
    }

    private void InitBuffTower(BuffTower buff)
    {
        int cost = GetTowerCost(TowerType.Buff);
        // +30% damage and +30% attack speed to nearby towers within range 3
        buff.Init(1.3f, 1.3f, 3f, cost, cost / 2);
    }

    // --- Public API for UI buttons ---

    public void StartPlacing(TowerType type)
    {
        Debug.Log($"TowerPlacer: StartPlacing called for {type}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("TowerPlacer: GameManager.Instance is null!");
            return;
        }
        if (GameManager.Instance.IsGameOver())
        {
            Debug.Log($"TowerPlacer: Can't place - game is over");
            return;
        }
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("TowerPlacer: EconomyManager.Instance is null!");
            return;
        }
        if (!EconomyManager.Instance.CanAfford(GetTowerCost(type)))
        {
            Debug.Log($"TowerPlacer: Can't afford {type} tower (cost: {GetTowerCost(type)}, gold: {EconomyManager.Instance.GetCurrentGold()})");
            return;
        }

        // Close the tower buy menu when entering placement mode
        TowerMenuUI menu = FindFirstObjectByType<TowerMenuUI>();
        if (menu != null) menu.CloseTowerMenu();

        CancelPlacement();
        isPlacing = true;
        currentTowerType = type;

        preview = CreateTowerVisual(new Color(1f, 1f, 1f, 0.5f), GetTowerSprite(type));
        previewRenderer = preview.GetComponent<SpriteRenderer>();

        // Create range indicator as child of preview
        float parentScale = preview.transform.localScale.x;
        rangeIndicator = CreateRangeIndicator(GetTowerRange(type), parentScale);
        rangeIndicator.transform.SetParent(preview.transform, false);
    }

    // Convenience methods for UI buttons
    public void StartPlacingBasicTower()  { Debug.Log("TowerPlacer: StartPlacingBasicTower called"); StartPlacing(TowerType.Basic); }
    public void StartPlacingSniperTower() { StartPlacing(TowerType.Sniper); }
    public void StartPlacingSprayTower()  { StartPlacing(TowerType.Spray); }
    public void StartPlacingRapidTower()  { StartPlacing(TowerType.Rapid); }
    public void StartPlacingSlowTower()   { StartPlacing(TowerType.Slow); }
    public void StartPlacingMortarTower() { StartPlacing(TowerType.Mortar); }
    public void StartPlacingBuffTower()   { StartPlacing(TowerType.Buff); }



    public bool IsPlacing() { return isPlacing; }

    // --- Sell tower ---

    public void SellTower(Tower tower)
    {
        if (tower == null) return;
        int refund = (int)tower.GetSellValue();
        EconomyManager.Instance.AddGold(refund);
        Debug.Log($"TowerPlacer: Sold tower for {refund} gold");
        Destroy(tower.gameObject);
    }

    public void SellMortarTower(MortarTower mortar)
    {
        if (mortar == null) return;
        int refund = (int)mortar.GetSellValue();
        EconomyManager.Instance.AddGold(refund);
        Debug.Log($"TowerPlacer: Sold mortar tower for {refund} gold");
        Destroy(mortar.gameObject);
    }

    public void SellBuffTower(BuffTower buff)
    {
        if (buff == null) return;
        int refund = (int)buff.GetSellValue();
        EconomyManager.Instance.AddGold(refund);
        Debug.Log($"TowerPlacer: Sold buff tower for {refund} gold");
        Destroy(buff.gameObject);
    }

    // --- Placement logic ---

    private void UpdatePreviewColor(Vector3 position)
    {
        if (previewRenderer == null) return;

        Collider2D overlap = Physics2D.OverlapCircle(position, placementCheckRadius, placementBlockedLayer);
        bool canPlace = overlap == null && EconomyManager.Instance.CanAfford(GetTowerCost(currentTowerType));

        if (canPlace)
            previewRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        else
            previewRenderer.color = new Color(1f, 0.2f, 0.2f, 0.5f); // red tint = can't place
    }

    private void TryPlace(Vector3 position)
    {
        Collider2D overlap = Physics2D.OverlapCircle(position, placementCheckRadius, placementBlockedLayer);
        if (overlap != null)
        {
            Debug.Log("TowerPlacer: Can't place here!");
            return;
        }

        int cost = GetTowerCost(currentTowerType);
        if (!EconomyManager.Instance.SpendGold(cost))
            return;

        // Create the actual tower
        GameObject tower = CreateTowerVisual(Color.white, GetTowerSprite(currentTowerType));
        tower.transform.position = position;
        tower.name = currentTowerType.ToString() + "Tower";
        tower.layer = LayerMask.NameToLayer("PlacementBlocked");

        // Add a small collider so towers block each other from overlapping
        BoxCollider2D col = tower.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        if (currentTowerType == TowerType.Mortar)
        {
            MortarTower mortar = tower.AddComponent<MortarTower>();
            InitMortarTower(mortar);
        }
        else if (currentTowerType == TowerType.Buff)
        {
            BuffTower buff = tower.AddComponent<BuffTower>();
            InitBuffTower(buff);
        }
        else
        {
            Tower towerScript = tower.AddComponent<Tower>();
            InitTower(towerScript, currentTowerType);
        }

        tower.AddComponent<TowerClickHandler>();

        Debug.Log($"TowerPlacer: Placed {currentTowerType} tower at {position}");
        CancelPlacement();
    }

    private void CancelPlacement()
    {
        isPlacing = false;
        if (preview != null)
            Destroy(preview);
        preview = null;
        previewRenderer = null;
        rangeIndicator = null;
    }

    // --- Visual creation helpers ---

    private GameObject CreateTowerVisual(Color color, Sprite sprite)
    {
        GameObject obj = new GameObject("TowerVisual");

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sr.sprite = sprite;
            obj.transform.localScale = Vector3.one * 0.5f;
        }
        else
        {
            // Create a solid colored texture since Texture2D.whiteTexture doesn't work with Sprite.Create
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            obj.transform.localScale = Vector3.one;
        }
        sr.color = color;
        sr.sortingOrder = 100;

        return obj;
    }

    private GameObject CreateRangeIndicator(float range, float parentScale)
    {
        GameObject ring = new GameObject("RangeIndicator");

        // diameter in world = range * 2, compensate for parent scale
        float localScale = (range * 2f) / parentScale;
        ring.transform.localScale = Vector3.one * localScale;

        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 99;

        // Create a circle texture at runtime
        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        float center = texSize / 2f;
        float outerRadius = center;
        float innerRadius = center - 2f; // thin ring

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.3f));
                else if (dist <= outerRadius)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.05f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
        sr.color = Color.white;

        return ring;
    }
}
