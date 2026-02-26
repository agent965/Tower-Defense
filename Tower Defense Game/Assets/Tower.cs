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
    public LayerMask detectionLayer = Physics2D.DefaultRaycastLayers;

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
    }

    private void SearchEnemy()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            rng,
            detectionLayer
        );

        if (hit != null && hit.CompareTag(targetTag))
        {
            ReleaseAttack(hit.transform);
        }
    }

    private void FaceEnemy()
    {
        if (!fTarget) return;

        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            rng,
            detectionLayer
        );

        if (hit != null && hit.CompareTag(targetTag))
        {
            RotateToward(hit.transform);
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
        homing.SetTarget(target);
    }

    private void CreateBasicProjectile(float angle)
    {
        GameObject proj = Create2DSquare();

        BasicProjectile basic = proj.AddComponent<BasicProjectile>();
        basic.SetAttributes(angle, (float)prjSpd, 10f, transform.position, dmg, prc);

        //(double damage, int pierce, float range, double projectileSpeed, double attackCooldown, double buyValue, double sellValue, int multiShot, bool facesTarget)
        //tower.init_Tower(2, 2, 4.0f, 7, 2, 2, 2, 0, false);
    }

    private GameObject Create2DSquare()
    {
        GameObject proj = new GameObject("Projectile");

        proj.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        proj.transform.localScale = Vector3.one * 0.2f;

        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        proj.AddComponent<BoxCollider2D>();

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