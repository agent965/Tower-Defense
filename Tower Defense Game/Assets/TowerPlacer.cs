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

    // Tower type definitions
    public enum TowerType { Basic, Sniper, Spray, Rapid }

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
    }

    void Update()
    {
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
            default: return 100;
        }
    }

    private static Color GetTowerColor(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return new Color(0.3f, 0.8f, 1f, 1f);   // light blue
            case TowerType.Sniper: return new Color(1f, 0.3f, 0.3f, 1f);   // red
            case TowerType.Spray:  return new Color(0.3f, 1f, 0.3f, 1f);   // green
            case TowerType.Rapid:  return new Color(1f, 0.8f, 0.2f, 1f);   // yellow
            default: return Color.white;
        }
    }

    private Sprite GetTowerSprite(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return basicTowerSprite;
            case TowerType.Sniper: return sniperTowerSprite;
            case TowerType.Spray:  return sprayTowerSprite;
            case TowerType.Rapid:  return rapidTowerSprite;
            default: return basicTowerSprite;
        }
    }

    private static float GetTowerRange(TowerType type)
    {
        switch (type)
        {
            case TowerType.Basic:  return 3f;
            case TowerType.Sniper: return 6f;
            case TowerType.Spray:  return 2.5f;
            case TowerType.Rapid:  return 2.5f;
            default: return 3f;
        }
    }

    private void InitTower(Tower towerScript, TowerType type)
    {
        int cost = GetTowerCost(type);
        switch (type)
        {
            case TowerType.Basic:
                // Balanced: decent damage, single homing shot
                towerScript.init_Tower(10, 1, 3f, 8.0, 1.0, cost, cost / 2, 1, true);
                break;
            case TowerType.Sniper:
                // High damage, long range, slow fire rate
                towerScript.init_Tower(50, 3, 6f, 15.0, 3.0, cost, cost / 2, 1, true);
                break;
            case TowerType.Spray:
                // Low damage, short range, fires in 6 directions
                towerScript.init_Tower(5, 1, 2.5f, 6.0, 1.5, cost, cost / 2, 6, false);
                break;
            case TowerType.Rapid:
                // Low damage, fast fire rate, single shot
                towerScript.init_Tower(4, 1, 2.5f, 10.0, 0.3, cost, cost / 2, 1, true);
                break;
        }
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
        if (!GameManager.Instance.IsBuilding())
        {
            Debug.Log($"TowerPlacer: Can't place - game state is {GameManager.Instance.gameState}, not Building");
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

        Color towerColor = GetTowerColor(type);
        Color previewColor = new Color(towerColor.r, towerColor.g, towerColor.b, 0.5f);
        preview = CreateTowerVisual(previewColor, GetTowerSprite(type));
        previewRenderer = preview.GetComponent<SpriteRenderer>();

        // Create range indicator as child of preview
        rangeIndicator = CreateRangeIndicator(GetTowerRange(type));
        rangeIndicator.transform.SetParent(preview.transform, false);
    }

    // Convenience methods for UI buttons
    public void StartPlacingBasicTower()  { Debug.Log("TowerPlacer: StartPlacingBasicTower called"); StartPlacing(TowerType.Basic); }
    public void StartPlacingSniperTower() { StartPlacing(TowerType.Sniper); }
    public void StartPlacingSprayTower()  { StartPlacing(TowerType.Spray); }
    public void StartPlacingRapidTower()  { StartPlacing(TowerType.Rapid); }

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

    // --- Placement logic ---

    private void UpdatePreviewColor(Vector3 position)
    {
        if (previewRenderer == null) return;

        Collider2D overlap = Physics2D.OverlapCircle(position, placementCheckRadius, placementBlockedLayer);
        bool canPlace = overlap == null && EconomyManager.Instance.CanAfford(GetTowerCost(currentTowerType));

        Color towerColor = GetTowerColor(currentTowerType);
        if (canPlace)
            previewRenderer.color = new Color(towerColor.r, towerColor.g, towerColor.b, 0.5f);
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
        Color color = GetTowerColor(currentTowerType);
        GameObject tower = CreateTowerVisual(color, GetTowerSprite(currentTowerType));
        tower.transform.position = position;
        tower.name = currentTowerType.ToString() + "Tower";

        // Add a small collider so towers block each other from overlapping
        BoxCollider2D col = tower.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        Tower towerScript = tower.AddComponent<Tower>();
        InitTower(towerScript, currentTowerType);

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

    private GameObject CreateRangeIndicator(float range)
    {
        GameObject ring = new GameObject("RangeIndicator");

        // Scale relative to the tower's 0.5 local scale: need range*2 diameter in world,
        // but since parent scale is 0.5, child needs (range * 2 / 0.5) = range * 4
        float localScale = range * 4f;
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
