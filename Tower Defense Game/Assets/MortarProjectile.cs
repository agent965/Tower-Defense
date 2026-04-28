using System.Collections;
using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    [Header("Sprites (auto-loaded if null)")]
    public Sprite airSprite;
    public Sprite groundSprite;

    [Header("Arc Settings")]
    public float arcHeight      = 2f;
    public float travelTime     = 1f;
    public float impactDuration = 0.3f;
    [Tooltip("Multiplier applied on top of splashRadius for the impact visual. 1.0 = exact splash diameter (often too small if the sprite has transparent padding).")]
    public float impactScale    = 3f;
    [Tooltip("Max scale of the airborne projectile sprite (the 'star').")]
    public float airSpriteScale = 0.7f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;
    private float damage;
    private float splashRadius;

    private SpriteRenderer spriteRenderer;
    private bool hasLanded = false;
    private bool initialized = false;

    // ── Called by MortarTower before the object is active ─────────────────
    public void Initialize(Vector3 target, float dmg, float splash)
    {
        targetPosition = target;
        damage         = dmg;
        splashRadius   = splash;
        initialized    = true;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 101;

        if (airSprite != null)
            spriteRenderer.sprite = airSprite;

        startPosition = transform.position;
        timer = 0f;
    }

    void Update()
    {
        if (!initialized || hasLanded) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / travelTime);

        if (t >= 1f)
        {
            Land();
            return;
        }

        // Parabolic arc
        Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, t);
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = linearPos + Vector3.up * height;

        // Scale up while rising, scale down while falling — gives a nice lob feel
        float scale = airSpriteScale * (0.5f + Mathf.Sin(t * Mathf.PI) * 0.5f);
        transform.localScale = Vector3.one * scale;
    }

    // ── Landing ────────────────────────────────────────────────────────────

    void Land()
    {
        hasLanded = true;
        transform.position = targetPosition;

        if (groundSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = groundSprite;

        // Scale the impact visual so its diameter ≈ splashRadius * 2 (true to gameplay)
        float spriteWidth = spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size.x
            : 0f;
        if (spriteWidth > 0.001f)
            transform.localScale = Vector3.one * (splashRadius * 2f / spriteWidth) * impactScale;
        else
            transform.localScale = Vector3.one * impactScale;

        ApplySplashDamage();
        StartCoroutine(DestroyAfterImpact());
    }

    void ApplySplashDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, splashRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.TakeDamage(damage);
            }
        }
    }

    IEnumerator DestroyAfterImpact()
    {
        yield return new WaitForSeconds(impactDuration);
        Destroy(gameObject);
    }
}
