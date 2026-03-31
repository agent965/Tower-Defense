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

    public string targetTag = "Enemy";
    public LayerMask detectionLayer;

    private double timeSinceLastAttack = 0;
    private bool fTarget;
    private float rotationSpeed = 360f;

    void FixedUpdate()
    {
        FaceEnemy();

        timeSinceLastAttack += Time.fixedDeltaTime;

        if (timeSinceLastAttack >= atkCd)
        {
            SearchEnemy();
        }
    }

    public void init_Tower(double damage, int pierce, float range, double projectileSpeed,
        double attackCooldown, double buyValue,
        double sellValue, int multiShot, bool facesTarget)
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

        // Detect all layers so OverlapCircle finds enemies
        detectionLayer = ~0;
    }

    public double GetSellValue() { return sVal; }
    public double GetBuyValue() { return bVal; }

    private Transform FindEnemyInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rng);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(targetTag))
                return hit.transform;
        }
        return null;
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

        HomingProjectile homing = proj.AddComponent<HomingProjectile>();
        homing.speed = (float)prjSpd;
        homing.SetTarget(target, dmg);
    }

    private void CreateBasicProjectile(float angle)
    {
        GameObject proj = Create2DSquare();

        BasicProjectile basic = proj.AddComponent<BasicProjectile>();
        basic.SetAttributes(angle, (float)prjSpd, rng, transform.position, dmg, prc);
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