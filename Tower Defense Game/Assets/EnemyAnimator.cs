using System.Collections;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;
    private bool isBoss;
    private bool initialized;

    // Walk bob
    private float walkPhase;
    private Vector3 lastPos;

    // Hit reaction
    private float hitFlashTimer;
    private float hitPunchTimer;
    private const float HIT_FLASH_DURATION = 0.08f;
    private const float HIT_PUNCH_DURATION = 0.12f;

    // Death
    private bool dying;

    void TryInit()
    {
        if (initialized) return;
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        baseColor = sr.color;
        baseScale = transform.localScale;
        lastPos   = transform.position;

        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) isBoss = enemy.isBoss;

        initialized = true;
    }

    void Update()
    {
        if (!initialized) TryInit();
        if (!initialized || dying || sr == null) return;

        // ── Movement detection ────────────────────────────────────────────
        Vector3 delta = transform.position - lastPos;
        bool moving = delta.sqrMagnitude > 0.00001f;
        lastPos = transform.position;

        // ── Walk bob (squash/stretch when moving) ─────────────────────────
        float bobX = 1f, bobY = 1f;
        if (moving)
        {
            walkPhase += Time.deltaTime * 9f;
            float bob = Mathf.Sin(walkPhase);
            bobY = 1f + bob * 0.06f;
            bobX = 1f - bob * 0.06f;
        }
        else
        {
            walkPhase = 0f;
        }

        // ── Boss pulse ────────────────────────────────────────────────────
        float pulse = 1f;
        if (isBoss)
            pulse = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f) * 0.05f;

        // ── Hit punch (brief scale spike) ─────────────────────────────────
        float punch = 1f;
        if (hitPunchTimer > 0f)
        {
            hitPunchTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(hitPunchTimer / HIT_PUNCH_DURATION);
            punch = 1f + t * 0.2f;
        }

        transform.localScale = new Vector3(
            baseScale.x * bobX * pulse * punch,
            baseScale.y * bobY * pulse * punch,
            baseScale.z
        );

        // ── Color: hit flash overrides boss tint ─────────────────────────
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(hitFlashTimer / HIT_FLASH_DURATION);
            sr.color = Color.Lerp(baseColor, Color.white, t);
        }
        else if (isBoss)
        {
            float b = (Mathf.Sin(Time.time * Mathf.PI * 2f) + 1f) * 0.5f;
            sr.color = Color.Lerp(baseColor, new Color(1f, 0.5f, 0.4f, baseColor.a), b * 0.3f);
        }
        else
        {
            sr.color = baseColor;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void PlayHit()
    {
        if (!initialized) TryInit();
        hitFlashTimer = HIT_FLASH_DURATION;
        hitPunchTimer = HIT_PUNCH_DURATION;
    }

    public void PlayDeath(System.Action onComplete)
    {
        if (dying)
        {
            onComplete?.Invoke();
            return;
        }
        if (!initialized) TryInit();
        dying = true;
        StartCoroutine(DeathRoutine(onComplete));
    }

    IEnumerator DeathRoutine(System.Action onComplete)
    {
        // Stop game logic on this enemy
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Use unscaled time so the animation plays through hit-stop
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(1f, 1.4f, t);
            transform.localScale = new Vector3(
                baseScale.x * scale,
                baseScale.y * scale,
                baseScale.z
            );

            transform.Rotate(0f, 0f, 720f * Time.unscaledDeltaTime);

            if (sr != null)
            {
                Color c = baseColor;
                c.a = baseColor.a * (1f - t);
                sr.color = c;
            }

            yield return null;
        }

        onComplete?.Invoke();
    }
}
