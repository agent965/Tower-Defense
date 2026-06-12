using UnityEngine;
using System.Collections.Generic;

public class Tower : MonoBehaviour
{
    private double dmg;
    private int prc;
    private float rng;
    private double prjSpd;
    private double atkCd;
    private double bVal;
    private double sVal;
    public int mShot;
    private string dbuff;

    public string targetTag = "Enemy";
    public LayerMask detectionLayer;
    public TargetMode targetMode = TargetMode.First;

    private double timeSinceLastAttack = 0;
    private bool fTarget;
    private float rotationSpeed = 360f;
    private float damageMultiplier  = 1f;
    private float cooldownMultiplier = 1f;
    private readonly Dictionary<BuffTower, (float dmg, float cd)> activeBuffs = new Dictionary<BuffTower, (float, float)>();

    // Bullet visual (set by TowerPlacer after init_Tower). If null, falls back
    // to a small procedural white square so the tower still functions.
    private Sprite bulletSprite;
    private float  bulletScale = 0.2f;
    private float  barrelOffset = 0f;          // along firing direction
    private float  barrelVerticalOffset = 0f;  // perpendicular to firing direction (sprite's "up")
    private static Sprite fallbackBulletSprite;

    // ── Laser mode (activated when a Sprite is set + tower reaches max upgrade) ──
    private Sprite laserModeSprite;     // sprite to swap to on final upgrade
    private Sprite chargeUpSprite;      // sprite shown during the charge-up animation
    private bool   isLaserMode = false;
    private bool   isCharging  = false; // disables attacks during charge-up
    private GameObject laserRoot;
    private LineRenderer laserGlow, laserMid, laserCore;
    private GameObject laserImpact;
    private SpriteRenderer laserImpactSR;
    private static Sprite laserGlowSprite;

    void FixedUpdate()
    {
        if (isCharging) return;     // freeze attacks during the charge-up animation

        FaceEnemy();

        timeSinceLastAttack += Time.fixedDeltaTime;

        if (timeSinceLastAttack >= atkCd / cooldownMultiplier)
        {
            SearchEnemy();
        }
    }

    public void init_Tower(double damage, int pierce, float range, double projectileSpeed,
        double attackCooldown, double buyValue,
        double sellValue, int multiShot, bool facesTarget, string debuff)
    {
        dmg = damage;
        prc = pierce;
        rng = range;

        prjSpd = projectileSpeed;
        atkCd = attackCooldown;
        bVal = buyValue;
        sVal = sellValue;
        mShot = multiShot;
        fTarget = facesTarget;
        dbuff = debuff;

        // Detect all layers so OverlapCircle finds enemies
        detectionLayer = ~0;
    }

    // Called by TowerPlacer right after init_Tower. Pass null sprite to keep the
    // fallback white square.
    public void SetBullet(Sprite sprite, float scale, float spawnOffset, float verticalOffset)
    {
        bulletSprite         = sprite;
        bulletScale          = scale > 0f ? scale : 0.2f;
        barrelOffset         = spawnOffset;
        barrelVerticalOffset = verticalOffset;
    }

    // Optional final-upgrade sprite swap (e.g. Rapid → AidenRapidLaser). When set
    // AND the tower reaches max upgrade level, it switches to continuous laser mode.
    public void SetLaserModeSprite(Sprite sprite)
    {
        laserModeSprite = sprite;
    }

    // Optional sprite shown during the charge-up animation before laser mode activates
    public void SetChargeUpSprite(Sprite sprite)
    {
        chargeUpSprite = sprite;
    }

    public double GetSellValue()          { return sVal; }
    public double GetBuyValue()           { return bVal; }
    public int    GetDamage()             => (int)dmg;
    public float  GetRange()              => rng;
    public float  GetCooldown()           => (float)atkCd;
    public float  GetDamageMultiplier()   => damageMultiplier;
    public float  GetCooldownMultiplier() => cooldownMultiplier;
    public bool   IsBuffed()              => damageMultiplier > 1f || cooldownMultiplier > 1f;

    // ── Upgrade system ─────────────────────────────────────────────────────

    private int upgradeLevel = 0;
    private TowerUpgradeData[] upgrades;

    public void SetUpgrades(TowerUpgradeData[] data) { upgrades = data; }

    public int  GetUpgradeLevel() => upgradeLevel;
    public bool IsMaxLevel()      => upgrades == null || upgradeLevel >= upgrades.Length;

    public int GetUpgradeCost()
    {
        if (IsMaxLevel()) return 0;
        return upgrades[upgradeLevel].cost;
    }

    public string GetUpgradeDescription()
    {
        if (IsMaxLevel()) return "MAX LEVEL";
        return upgrades[upgradeLevel].description;
    }

    public void Upgrade()
    {
        if (IsMaxLevel()) return;
        TowerUpgradeData u = upgrades[upgradeLevel];
        upgradeLevel++;

        dmg   += u.dmgAdd;
        rng   *= u.rangeMult;
        atkCd *= u.cooldownMult;
        sVal  += u.cost * 0.5;

        UpgradeEffect.Play(transform);

        // Final upgrade unlocks laser mode if a laser sprite was provided
        if (IsMaxLevel() && laserModeSprite != null && !isLaserMode)
            EnableLaserMode();
    }

    // Called by BuffTower each tick while this tower is in range
    public void ApplyBuff(BuffTower source, float dmgMult, float cdMult)
    {
        activeBuffs[source] = (dmgMult, cdMult);
        RecalculateBuffs();
    }

    // Called by BuffTower when this tower leaves range or the BuffTower is destroyed
    public void RemoveBuff(BuffTower source)
    {
        if (activeBuffs.Remove(source))
            RecalculateBuffs();
    }

    private void RecalculateBuffs()
    {
        damageMultiplier  = 1f;
        cooldownMultiplier = 1f;
        foreach (var b in activeBuffs.Values)
        {
            damageMultiplier  += (b.dmg - 1f);
            cooldownMultiplier += (b.cd - 1f);
        }
    }

    private Transform FindEnemyInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rng);

        Transform best       = null;
        int       bestWP     = -1;
        float     bestDist   = float.MaxValue;
        float     bestHP     = -1f;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(targetTag)) continue;
            Enemy e = hit.GetComponent<Enemy>();
            if (e == null) continue;
            if (e.IsPetrified()) continue;

            switch (targetMode)
            {
                case TargetMode.First:
                    int   wp   = e.GetWaypointIndex();
                    float dist = e.GetDistanceToNextWaypoint();
                    if (wp > bestWP || (wp == bestWP && dist < bestDist))
                    { bestWP = wp; bestDist = dist; best = hit.transform; }
                    break;
                case TargetMode.Last:
                    if (best == null || e.GetWaypointIndex() < bestWP)
                    { bestWP = e.GetWaypointIndex(); best = hit.transform; }
                    break;
                case TargetMode.Strongest:
                    if (e.GetCurrentHP() > bestHP)
                    { bestHP = e.GetCurrentHP(); best = hit.transform; }
                    break;
            }
        }
        return best;
    }

    private void SearchEnemy()
    {
        Transform enemy = FindEnemyInRange();
        if (enemy != null)
        {
            ReleaseAttack(enemy);
        }
    }

    private void FaceEnemy()
    {
        if (!fTarget) return;

        Transform enemy = FindEnemyInRange();
        if (enemy != null)
        {
            RotateToward(enemy);
        }
    }

    private void ReleaseAttack(Transform target)
    {
        timeSinceLastAttack = 0;

        // Laser mode: damage the target directly, no projectile spawn.
        // The continuous beam visual is driven by Update() each frame.
        if (isLaserMode)
        {
            Enemy e = target.GetComponent<Enemy>();
            if (e != null)
            {
                e.TakeDamage((float)(dmg * damageMultiplier));
                if (dbuff != "none") e.Debuff(dbuff);
            }
            return;
        }

        AudioManager.Instance?.PlayTowerShoot();

        if (mShot <= 1)
        {
            CreateHomingProjectile(target);
        }
        else
        {
            float increment = 360f / mShot;

            for (int i = 0; i < mShot; i++)
            {
                CreateBasicProjectile(increment * i);
            }
        }
    }

    // ── Laser mode ───────────────────────────────────────────────────────

    void Update()
    {
        if (isLaserMode) UpdateLaserVisuals();
    }

    void EnableLaserMode()
    {
        // If we have a charge-up sprite, do the DBZ-style power-up sequence first
        if (chargeUpSprite != null)
        {
            StartChargeUp();
            return;
        }
        // Otherwise activate immediately
        ActivateLaserMode();
    }

    void StartChargeUp()
    {
        isCharging = true;

        // Swap to the charge-up sprite (Goku scream pose)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = chargeUpSprite;

        // Build the aura. onComplete fires when the animation finishes.
        ChargeUpEffect.Play(transform, 2.5f, () =>
        {
            isCharging = false;
            ActivateLaserMode();
        });
    }

    void ActivateLaserMode()
    {
        isLaserMode = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = laserModeSprite;

        BuildLaserVisuals();
    }

    void BuildLaserVisuals()
    {
        laserRoot = new GameObject("LaserVisuals");
        laserRoot.transform.SetParent(transform, false);
        laserRoot.transform.localPosition = Vector3.zero;

        // Three layered LineRenderers create a fake-glow beam without needing bloom.
        laserGlow = MakeLine("LaserGlow", 0.34f, new Color(1f, 0.15f, 0.15f, 0.30f), 49);
        laserMid  = MakeLine("LaserMid",  0.16f, new Color(1f, 0.35f, 0.35f, 0.85f), 50);
        laserCore = MakeLine("LaserCore", 0.05f, new Color(1f, 0.95f, 0.9f, 1f),     51);

        // Soft glow dot at the impact point
        laserImpact = new GameObject("LaserImpact");
        laserImpact.transform.SetParent(laserRoot.transform, true);
        laserImpactSR = laserImpact.AddComponent<SpriteRenderer>();
        laserImpactSR.sprite = GetLaserGlowSprite();
        laserImpactSR.color  = new Color(1f, 0.4f, 0.3f, 0.95f);
        laserImpactSR.sortingOrder = 52;
        laserImpact.SetActive(false);
    }

    LineRenderer MakeLine(string name, float width, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(laserRoot.transform);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = color;
        lr.endColor      = color;
        lr.sortingOrder  = sortingOrder;
        lr.useWorldSpace = true;
        lr.enabled       = false;
        return lr;
    }

    void UpdateLaserVisuals()
    {
        Transform target = FindEnemyInRange();
        if (target == null)
        {
            SetLine(laserGlow, false);
            SetLine(laserMid,  false);
            SetLine(laserCore, false);
            if (laserImpact != null) laserImpact.SetActive(false);
            return;
        }

        // Start the beam in front of the tower at the gun barrel (forward + vertical offset)
        Vector3 toTarget = target.position - transform.position;
        Vector3 start = transform.position
                      + toTarget.normalized * Mathf.Max(barrelOffset, 0.1f)
                      + (Vector3)transform.up * barrelVerticalOffset;
        Vector3 end   = target.position;

        DrawLine(laserGlow, start, end);
        DrawLine(laserMid,  start, end);
        DrawLine(laserCore, start, end);

        // Pulse the beam width
        float pulse = 1f + Mathf.Sin(Time.time * 28f) * 0.20f;
        laserGlow.startWidth = laserGlow.endWidth = 0.34f * pulse;
        laserMid.startWidth  = laserMid.endWidth  = 0.16f * pulse;

        // Impact dot pulses + rotates for sparkle
        if (laserImpact != null)
        {
            laserImpact.SetActive(true);
            laserImpact.transform.position = end;
            float impactPulse = 1f + Mathf.Sin(Time.time * 32f) * 0.25f;
            laserImpact.transform.localScale = Vector3.one * 0.55f * impactPulse;
            laserImpact.transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        }
    }

    void SetLine(LineRenderer lr, bool enabled)
    {
        if (lr != null) lr.enabled = enabled;
    }

    void DrawLine(LineRenderer lr, Vector3 a, Vector3 b)
    {
        if (lr == null) return;
        lr.enabled = true;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
    }

    static Sprite GetLaserGlowSprite()
    {
        if (laserGlowSprite != null) return laserGlowSprite;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float maxR = size / 2f - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = d > maxR ? 0f : 1f - d / maxR;
            a *= a;  // sharper falloff
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        laserGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return laserGlowSprite;
    }

    private void CreateHomingProjectile(Transform target)
    {
        // Spawn in front of the tower along the line to the target,
        // plus a perpendicular offset (tower's "up") for gun height
        Vector3 toTarget = (target.position - transform.position);
        Vector3 spawnPos = transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
            spawnPos += toTarget.normalized * barrelOffset;
        spawnPos += transform.up * barrelVerticalOffset;

        GameObject proj = Create2DSquare(spawnPos);
        proj.name = dbuff;
        HomingProjectile homing = proj.AddComponent<HomingProjectile>();
        homing.speed = (float)prjSpd;
        homing.SetTarget(target, dmg * damageMultiplier);
    }

    private void CreateBasicProjectile(float angle)
    {
        // Spawn in front of the tower along this shot's firing direction,
        // plus a perpendicular offset (tower's "up") for gun height
        float radians = angle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        Vector3 spawnPos = transform.position + dir * barrelOffset + (Vector3)transform.up * barrelVerticalOffset;

        GameObject proj = Create2DSquare(spawnPos);
        proj.name = dbuff;
        BasicProjectile basic = proj.AddComponent<BasicProjectile>();
        basic.SetAttributes(angle, (float)prjSpd, rng, spawnPos, dmg * damageMultiplier, prc);
    }

    private GameObject Create2DSquare(Vector3 position)
    {
        GameObject proj = new GameObject("Projectile");

        proj.transform.position = new Vector3(position.x, position.y, 0f);
        proj.transform.localScale = Vector3.one * bulletScale;

        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = bulletSprite != null ? bulletSprite : GetFallbackBulletSprite();
        sr.sortingOrder = 101;

        BoxCollider2D bc = proj.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        // Match collider size to the sprite so hit detection is consistent
        if (sr.sprite != null)
            bc.size = sr.sprite.bounds.size;

        Rigidbody2D rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        return proj;
    }

    private static Sprite GetFallbackBulletSprite()
    {
        if (fallbackBulletSprite != null) return fallbackBulletSprite;
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        fallbackBulletSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackBulletSprite;
    }

    private SpriteRenderer cachedSR;
    private bool wasAimingLeft;

    private void RotateToward(Transform target)
    {
        if (target == null) return;

        Vector2 dir = target.position - transform.position;

        // Aim with rotation in [-90, 90] only, then mirror sprite when target is left.
        // Keeps the tower right-side-up regardless of aim direction.
        // Math: with flipX on, the sprite's "+X" renders at world angle (rot + 180),
        // so to aim at world angle beta we set rot = beta - 180.
        float beta = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bool aimingLeft = beta > 90f || beta < -90f;
        float angle = aimingLeft ? beta - 180f : beta;

        if (cachedSR == null) cachedSR = GetComponent<SpriteRenderer>();
        if (cachedSR != null) cachedSR.flipX = aimingLeft;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        // When the flip state changes, snap rotation instantly — otherwise
        // RotateTowards would sweep the long way around 180° because the
        // target rotation jumped while the visual is unchanged (flipX compensates).
        if (aimingLeft != wasAimingLeft)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

        wasAimingLeft = aimingLeft;
    }
}
