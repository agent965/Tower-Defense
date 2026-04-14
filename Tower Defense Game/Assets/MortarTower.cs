using System.Collections;
using UnityEngine;

public class MortarTower : MonoBehaviour
{
    public enum TowerLevel { Level1, Level2 }
    public TowerLevel currentLevel = TowerLevel.Level1;

    [Header("Renderer (auto-assigned if null)")]
    public SpriteRenderer spriteRenderer;

    // Sprite arrays — auto-loaded from Resources/Mortar/ if not set in Inspector
    [Header("Level 1 Animations (auto-loaded)")]
    public Sprite[] level1Idle;
    public Sprite[] level1Shoot;
    public Sprite[] level1Reload;

    [Header("Level 2 Animations (auto-loaded)")]
    public Sprite[] level2Idle;
    public Sprite[] level2Shoot;
    public Sprite[] level2Reload;

    [Header("Projectile Prefabs (auto-created if null)")]
    public GameObject level1ProjectilePrefab;
    public GameObject level2ProjectilePrefab;

    [Header("Fire Settings")]
    public Transform firePoint;
    public float idleFrameRate   = 0.2f;
    public float shootFrameRate  = 0.1f;
    public float reloadFrameRate = 0.15f;
    public float attackCooldown  = 2f;

    [Header("Sprite Size")]
    [Tooltip("Pixels per unit for auto-loaded sprites. Increase to make sprites smaller.")]
    public float pixelsPerUnit = 512f;

    [Header("Combat")]
    public float range        = 4f;
    public float damage       = 30f;
    public float splashRadius = 1.2f;
    public string targetTag   = "Enemy";

    [Header("Economy")]
    public double buyValue;
    public double sellValue;

    // Runtime sprite sets
    private Sprite[] currentIdle;
    private Sprite[] currentShoot;
    private Sprite[] currentReload;

    private bool isAttacking = false;
    private Coroutine idleRoutine;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        // Auto-assign SpriteRenderer
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

        // Load sprites from Resources if not already assigned in the Inspector
        if (level1Idle   == null || level1Idle.Length   == 0) level1Idle   = LoadSprites("Mortar/Lv1/Idle");
        if (level1Shoot  == null || level1Shoot.Length  == 0) level1Shoot  = LoadSprites("Mortar/Lv1/Shoot/Idle", "Mortar/Lv1/Shoot/Charge", "Mortar/Lv1/Shoot/Shoot");
        if (level1Reload == null || level1Reload.Length == 0) level1Reload = LoadSprites("Mortar/Lv1/Reload/Charge", "Mortar/Lv1/Reload/Charge2", "Mortar/Lv1/Reload/Charge3", "Mortar/Lv1/Reload/ChargeAlt", "Mortar/Lv1/Reload/Reloaded");

        if (level2Idle   == null || level2Idle.Length   == 0) level2Idle   = LoadSprites("Mortar/Lv2/Idle");
        if (level2Shoot  == null || level2Shoot.Length  == 0) level2Shoot  = LoadSprites("Mortar/Lv2/Shoot/Charge1", "Mortar/Lv2/Shoot/Charge2", "Mortar/Lv2/Shoot/Fire", "Mortar/Lv2/Shoot/Fire2");
        if (level2Reload == null || level2Reload.Length == 0) level2Reload = LoadSprites(
            "Mortar/Lv2/Reload/Charge1", "Mortar/Lv2/Reload/Charge1Alt",
            "Mortar/Lv2/Reload/Charge2", "Mortar/Lv2/Reload/Charge3",
            "Mortar/Lv2/Reload/Charge4", "Mortar/Lv2/Reload/Charge5",
            "Mortar/Lv2/Reload/Charge5Alt", "Mortar/Lv2/Reload/Charge6",
            "Mortar/Lv2/Reload/Charge7",  "Mortar/Lv2/Reload/Reloaded");
    }

    void Start()
    {
        SetLevel(currentLevel);

        if (currentIdle != null && currentIdle.Length > 0)
            idleRoutine = StartCoroutine(PlayLooping(currentIdle, idleFrameRate));
    }

    void FixedUpdate()
    {
        if (isAttacking) return;
        Transform target = FindEnemyInRange();
        if (target != null)
            StartCoroutine(AttackRoutine(target, target.position));
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void Init(float dmg, float splash, float rng, float cooldown, double bVal, double sVal, TowerLevel level)
    {
        damage        = dmg;
        splashRadius  = splash;
        range         = rng;
        attackCooldown = cooldown;
        buyValue      = bVal;
        sellValue     = sVal;
        SetLevel(level);
    }

    public double GetSellValue() => sellValue;
    public double GetBuyValue()  => buyValue;

    public void SetLevel(TowerLevel level)
    {
        currentLevel = level;
        if (level == TowerLevel.Level1)
        {
            currentIdle   = level1Idle;
            currentShoot  = level1Shoot;
            currentReload = level1Reload;
        }
        else
        {
            currentIdle   = level2Idle;
            currentShoot  = level2Shoot;
            currentReload = level2Reload;
        }
    }

    public void Attack(Vector3 targetPosition)
    {
        if (!isAttacking)
            StartCoroutine(AttackRoutine(null, targetPosition));
    }

    // ── Internal ───────────────────────────────────────────────────────────

    private Transform FindEnemyInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy) continue;
            if (!hit.CompareTag(targetTag)) continue;
            // Skip enemies with no HP left (dying this frame)
            Enemy e = hit.GetComponent<Enemy>();
            if (e != null) return hit.transform;
        }
        return null;
    }

    IEnumerator AttackRoutine(Transform targetTransform, Vector3 fallbackPos = default)
    {
        isAttacking = true;

        if (idleRoutine != null) StopCoroutine(idleRoutine);

        if (currentShoot != null && currentShoot.Length > 0)
            yield return StartCoroutine(PlayOnce(currentShoot, shootFrameRate));

        // Confirm the target is still alive before firing
        bool targetAlive = targetTransform != null && targetTransform.gameObject != null
                           && targetTransform.gameObject.activeInHierarchy;

        Vector3 fireTarget = targetAlive ? targetTransform.position : fallbackPos;

        // Only fire if the target was still alive, or we were given a direct position
        if (targetAlive || targetTransform == null)
            FireProjectiles(fireTarget);

        if (currentReload != null && currentReload.Length > 0)
            yield return StartCoroutine(PlayOnce(currentReload, reloadFrameRate));

        if (currentIdle != null && currentIdle.Length > 0)
            idleRoutine = StartCoroutine(PlayLooping(currentIdle, idleFrameRate));

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    void FireProjectiles(Vector3 target)
    {
        SpawnProjectile(level1ProjectilePrefab, target, "Mortar/Impact/Proj1/Flying", "Mortar/Impact/Proj1/Impact");
        if (currentLevel == TowerLevel.Level2)
            SpawnProjectile(level2ProjectilePrefab, target, "Mortar/Impact/Proj2/Flying", "Mortar/Impact/Proj2/Impact");
    }

    void SpawnProjectile(GameObject prefab, Vector3 target, string flyingPath, string impactPath)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;

        GameObject proj = prefab != null
            ? Instantiate(prefab, origin, Quaternion.identity)
            : new GameObject("MortarProjectile");

        proj.transform.position = origin;

        MortarProjectile mp = proj.GetComponent<MortarProjectile>() ?? proj.AddComponent<MortarProjectile>();

        // Auto-load air/ground sprites with matching PPU if not already set on the prefab
        if (mp.airSprite    == null) mp.airSprite    = LoadSprite(flyingPath);
        if (mp.groundSprite == null) mp.groundSprite = LoadSprite(impactPath);

        // Ensure a SpriteRenderer exists
        if (proj.GetComponent<SpriteRenderer>() == null)
        {
            var sr = proj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 101;
        }

        mp.Initialize(target, damage, splashRadius);
    }

    // ── Animation helpers ──────────────────────────────────────────────────

    IEnumerator PlayLooping(Sprite[] frames, float rate)
    {
        int index = 0;
        while (true)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(rate);
        }
    }

    IEnumerator PlayOnce(Sprite[] frames, float rate)
    {
        foreach (Sprite s in frames)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = s;
            yield return new WaitForSeconds(rate);
        }
    }

    // ── Sprite loader util ─────────────────────────────────────────────────

    /// <summary>Loads one or more sprites by Resources path (no extension), respecting pixelsPerUnit.</summary>
    private Sprite[] LoadSprites(params string[] paths)
    {
        var list = new System.Collections.Generic.List<Sprite>();
        foreach (string path in paths)
        {
            Sprite s = LoadSprite(path);
            if (s != null) list.Add(s);
            else Debug.LogWarning($"MortarTower: sprite not found at Resources/{path}");
        }
        return list.ToArray();
    }

    private Sprite LoadSprite(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point; // keeps pixel art crisp
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
    }
}
