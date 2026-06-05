using UnityEngine;

// Persistent aura attached to a fully-upgraded Buff tower (ARCHDRUID form).
// - A purple energy ring around the tower bobbing up and down
// - Periodic soft pulses emitted from the staff tip
public class ArchdruidAura : MonoBehaviour
{
    private static readonly Color Purple = new Color(0.62f, 0.30f, 0.88f, 1f);
    private static readonly Color Gold   = new Color(1f, 0.82f, 0.25f, 1f);

    // ── Configurable from the outside ───────────────────────────────────
    [Tooltip("Offset (in world units, relative to the tower center) of the staff tip — where the pulse emanates from.")]
    public Vector2 staffPulseOffset = new Vector2(0f, 0.5f);
    [Tooltip("Seconds between staff-tip pulses.")]
    public float pulseInterval = 1.5f;

    // ── Internal state ──────────────────────────────────────────────────
    private SpriteRenderer ring;
    private float pulseTimer;
    private static Sprite cachedRing;
    private static Sprite cachedGlow;

    void Start()
    {
        BuildRing();
        // Stagger initial pulse so it doesn't all fire on frame 1
        pulseTimer = Random.Range(0f, pulseInterval * 0.5f);
    }

    void BuildRing()
    {
        GameObject ringObj = new GameObject("AuraRing");
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = Vector3.zero;

        ring = ringObj.AddComponent<SpriteRenderer>();
        ring.sprite       = GetRingSprite();
        ring.color        = new Color(Purple.r, Purple.g, Purple.b, 0.7f);
        ring.sortingOrder = 48;
        // Flattened ellipse — reads like a halo at the tower's feet
        ringObj.transform.localScale = new Vector3(1.5f, 0.55f, 1f);
    }

    void Update()
    {
        // ── Bob the energy ring up and down ──────────────────────────────
        if (ring != null)
        {
            float bob = Mathf.Sin(Time.time * 2.4f);
            ring.transform.localPosition = new Vector3(0f, bob * 0.35f, 0f);

            // Alpha pulse synced with bob (subtle)
            float alpha = 0.55f + (bob + 1f) * 0.5f * 0.25f;   // 0.55..0.80
            // Color drifts purple → slightly toward gold at the bob peak
            float warmth = (bob + 1f) * 0.5f * 0.35f;
            Color c = Color.Lerp(Purple, Gold, warmth);
            ring.color = new Color(c.r, c.g, c.b, alpha);

            // Slight scale breathing
            float s = 1f + Mathf.Sin(Time.time * 1.8f) * 0.05f;
            ring.transform.localScale = new Vector3(1.5f * s, 0.55f * s, 1f);
        }

        // ── Periodic staff-tip pulse ─────────────────────────────────────
        pulseTimer += Time.deltaTime;
        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            EmitStaffPulse();
        }
    }

    void EmitStaffPulse()
    {
        Vector3 tipWorld = transform.position + new Vector3(staffPulseOffset.x, staffPulseOffset.y, 0f);

        // Bright glow point at the tip itself
        GameObject flash = new GameObject("StaffFlash");
        flash.transform.position = tipWorld;
        SpriteRenderer fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite       = GetGlowSprite();
        fsr.color        = new Color(1f, 0.85f, 0.4f, 0.95f);
        fsr.sortingOrder = 60;
        flash.transform.localScale = Vector3.one * 0.35f;
        ExpandingRing flashR = flash.AddComponent<ExpandingRing>();
        flashR.lifetime   = 0.35f;
        flashR.startScale = 0.35f;
        flashR.endScale   = 0.8f;

        // Expanding ring shockwave from the tip
        GameObject ringObj = new GameObject("StaffPulse");
        ringObj.transform.position = tipWorld;
        SpriteRenderer rsr = ringObj.AddComponent<SpriteRenderer>();
        rsr.sprite       = GetRingSprite();
        rsr.color        = new Color(Purple.r, Purple.g, Purple.b, 0.8f);
        rsr.sortingOrder = 59;
        ringObj.transform.localScale = Vector3.one * 0.2f;
        ExpandingRing ringR = ringObj.AddComponent<ExpandingRing>();
        ringR.lifetime   = 0.7f;
        ringR.startScale = 0.2f;
        ringR.endScale   = 2.6f;

        // Inner gold ring for that complementary color flash
        GameObject innerObj = new GameObject("StaffPulseInner");
        innerObj.transform.position = tipWorld;
        SpriteRenderer isr = innerObj.AddComponent<SpriteRenderer>();
        isr.sprite       = GetRingSprite();
        isr.color        = new Color(Gold.r, Gold.g, Gold.b, 0.9f);
        isr.sortingOrder = 60;
        innerObj.transform.localScale = Vector3.one * 0.2f;
        ExpandingRing innerR = innerObj.AddComponent<ExpandingRing>();
        innerR.lifetime   = 0.55f;
        innerR.startScale = 0.2f;
        innerR.endScale   = 1.7f;
        innerR.startDelay = 0.08f;
    }

    // ── Procedural sprites (shared cache) ────────────────────────────────

    static Sprite GetRingSprite()
    {
        if (cachedRing != null) return cachedRing;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        float thickness = 3f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float dr = Mathf.Abs(d - radius);
            if (dr > thickness) { px[y * size + x] = Color.clear; continue; }
            float fade = 1f - dr / thickness;
            fade = Mathf.Pow(fade, 1.2f);
            px[y * size + x] = new Color(1f, 1f, 1f, fade);
        }
        tex.SetPixels(px); tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedRing;
    }

    static Sprite GetGlowSprite()
    {
        if (cachedGlow != null) return cachedGlow;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float maxR = size / 2f - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = d > maxR ? 0f : 1f - d / maxR;
            a *= a;
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px); tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedGlow;
    }
}
