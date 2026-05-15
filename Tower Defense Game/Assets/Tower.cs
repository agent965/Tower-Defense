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
    private float  barrelOffset = 0f;   // world units in front of the tower
    private static Sprite fallbackBulletSprite;

    void FixedUpdate()
    {
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
    public void SetBullet(Sprite sprite, float scale, float spawnOffset)
    {
        bulletSprite = sprite;
        bulletScale  = scale > 0f ? scale : 0.2f;
        barrelOffset = spawnOffset;
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

    private void CreateHomingProjectile(Transform target)
    {
        // Spawn in front of the tower along the line to the target
        Vector3 toTarget = (target.position - transform.position);
        Vector3 spawnPos = transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
            spawnPos += toTarget.normalized * barrelOffset;

        GameObject proj = Create2DSquare(spawnPos);
        proj.name = dbuff;
        HomingProjectile homing = proj.AddComponent<HomingProjectile>();
        homing.speed = (float)prjSpd;
        homing.SetTarget(target, dmg * damageMultiplier);
    }

    private void CreateBasicProjectile(float angle)
    {
        // Spawn in front of the tower along this shot's firing direction
        float radians = angle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        Vector3 spawnPos = transform.position + dir * barrelOffset;

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