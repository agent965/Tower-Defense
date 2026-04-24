using UnityEngine;

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
    private float damageMultiplier = 1f;
    private float cooldownMultiplier = 1f;

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

    public double GetSellValue() { return sVal; }
    public double GetBuyValue()  { return bVal; }
    public int    GetDamage()    => (int)dmg;
    public float  GetRange()     => rng;
    public float  GetCooldown()  => (float)atkCd;

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
    }

    public void SetBuff(float dmgMult, float cdMult)
    {
        damageMultiplier = dmgMult;
        cooldownMultiplier = cdMult;
    }

    public void ClearBuff()
    {
        damageMultiplier = 1f;
        cooldownMultiplier = 1f;
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
        GameObject proj = Create2DSquare();
        proj.name = dbuff;
        HomingProjectile homing = proj.AddComponent<HomingProjectile>();
        homing.speed = (float)prjSpd;
        homing.SetTarget(target, dmg * damageMultiplier);
    }

    private void CreateBasicProjectile(float angle)
    {
        GameObject proj = Create2DSquare();
        proj.name = dbuff;
        BasicProjectile basic = proj.AddComponent<BasicProjectile>();
        basic.SetAttributes(angle, (float)prjSpd, rng, transform.position, dmg * damageMultiplier, prc);
    }

    private GameObject Create2DSquare()
    {
        GameObject proj = new GameObject("Projectile");

        proj.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        proj.transform.localScale = Vector3.one * 0.2f;

        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sr.sortingOrder = 101;

        BoxCollider2D bc = proj.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;

        Rigidbody2D rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        return proj;
    }

    private void RotateToward(Transform target)
    {
        if (target == null) return;

        Vector2 dir = target.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}