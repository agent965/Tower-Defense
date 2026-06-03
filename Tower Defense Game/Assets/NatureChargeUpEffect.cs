using UnityEngine;

// Wind-themed charge-up effect (purple/gold). Continuously spawns many small
// particles that orbit the tower while leaving fading trails, creating the look
// of flowing wind currents. Tints the tower sprite during charge, then releases
// a soft burst and swaps the sprite via the onComplete callback.
public class NatureChargeUpEffect : MonoBehaviour
{
    private static readonly Color Purple = new Color(0.62f, 0.30f, 0.88f, 1f);
    private static readonly Color Gold   = new Color(1f, 0.82f, 0.25f, 1f);

    public static void Play(Transform tower, SpriteRenderer towerSR, float duration, System.Action onComplete = null)
    {
        GameObject obj = new GameObject("NatureChargeUpEffect");
        obj.transform.position = tower.position;

        NatureChargeUpEffect n = obj.AddComponent<NatureChargeUpEffect>();
        n.tower      = tower;
        n.towerSR    = towerSR;
        n.duration   = duration;
        n.onComplete = onComplete;
        n.Build();
    }

    private Transform tower;
    private SpriteRenderer towerSR;
    private float duration = 2.5f;
    private System.Action onComplete;

    private float elapsed;
    private float ringTimer;
    private float spawnAccumulator;       // fractional spawn count carry-over
    private Color originalTowerColor = Color.white;
    private SpriteRenderer baseGlow;

    [Tooltip("Particles spawned per second at full intensity.")]
    private const float MAX_SPAWN_RATE = 110f;

    private static Sprite cachedDot;
    private static Sprite cachedRing;
    private static Sprite cachedGlow;

    void Build()
    {
        if (towerSR != null) originalTowerColor = towerSR.color;

        // Ground glow — subtle base layer
        GameObject glow = new GameObject("BaseGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        baseGlow = glow.AddComponent<SpriteRenderer>();
        baseGlow.sprite       = GetGlowSprite();
        baseGlow.color        = new Color(Purple.r, Purple.g, Purple.b, 0f);
        baseGlow.sortingOrder = 48;
        glow.transform.localScale = Vector3.one * 2.2f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (tower != null) transform.position = tower.position;

        float t = Mathf.Clamp01(elapsed / duration);
        float intensity = Mathf.SmoothStep(0f, 1f, t);

        // ── Tower sprite tint cycles purple ↔ gold ───────────────────────
        if (towerSR != null)
        {
            float cycle = (Mathf.Sin(Time.time * 1.6f) + 1f) * 0.5f;
            Color tint  = Color.Lerp(Purple, Gold, cycle);
            towerSR.color = Color.Lerp(originalTowerColor, tint, intensity * 0.85f);
        }

        // ── Base glow: gentle pulse, color drifts ────────────────────────
        if (baseGlow != null)
        {
            float gp = 1f + Mathf.Sin(Time.time * 2.4f) * 0.10f;
            baseGlow.transform.localScale = Vector3.one * 2.2f * gp;
            float cycle = (Mathf.Sin(Time.time * 1.6f) + 1f) * 0.5f;
            Color c = Color.Lerp(Purple, Gold, cycle);
            baseGlow.color = new Color(c.r, c.g, c.b, 0.45f * intensity);
        }

        // ── Continuously spawn orbiting particles (with trails) ──────────
        spawnAccumulator += MAX_SPAWN_RATE * intensity * Time.deltaTime;
        int spawnThisFrame = Mathf.FloorToInt(spawnAccumulator);
        spawnAccumulator -= spawnThisFrame;
        for (int i = 0; i < spawnThisFrame; i++)
            SpawnOrbitParticle(intensity);

        // ── Soft expanding rings every ~0.7s ─────────────────────────────
        ringTimer -= Time.deltaTime;
        if (ringTimer <= 0f && elapsed > 0.3f)
        {
            SpawnSoftRing();
            ringTimer = 0.7f;
        }

        // Very gentle camera shake
        CameraShake.Shake(0.04f, 0.003f + intensity * 0.015f);

        if (elapsed >= duration)
            Finish();
    }

    void Finish()
    {
        // Soft golden expanding glow burst
        GameObject obj = new GameObject("FinalBurst");
        obj.transform.position = transform.position;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetGlowSprite();
        sr.color        = new Color(1f, 0.85f, 0.4f, 0.92f);
        sr.sortingOrder = 99;
        obj.transform.localScale = Vector3.one;
        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime   = 0.65f;
        r.startScale = 1f;
        r.endScale   = 5.5f;

        // Two soft rings staggered
        SpawnFinalRing(0.00f, 4.2f, 0.7f,  new Color(Purple.r, Purple.g, Purple.b, 0.72f));
        SpawnFinalRing(0.10f, 3.6f, 0.6f,  new Color(Gold.r,   Gold.g,   Gold.b,   0.80f));

        CameraShake.Shake(0.30f, 0.06f);

        if (towerSR != null) towerSR.color = originalTowerColor;

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    // ── Particle spawners ────────────────────────────────────────────────

    void SpawnOrbitParticle(float intensity)
    {
        GameObject obj = new GameObject("OrbitParticle");
        // Spawn at a random point on a wide orbit ring
        float angle  = Random.Range(0f, 360f);
        float radius = Random.Range(0.55f, 1.85f);   // pushed outward — felt more
        float rad    = angle * Mathf.Deg2Rad;
        obj.transform.position = transform.position
            + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius * 0.7f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetDotSprite();
        Color baseColor = Random.value < 0.5f ? Purple : Gold;
        sr.color = baseColor;
        sr.sortingOrder = 50;
        float size = Random.Range(0.10f, 0.18f);     // bigger dots
        obj.transform.localScale = Vector3.one * size;

        // Trail — gives the wind-current look
        TrailRenderer tr = obj.AddComponent<TrailRenderer>();
        tr.material      = new Material(Shader.Find("Sprites/Default"));
        tr.time          = Random.Range(0.55f, 0.95f);   // longer trails
        tr.startWidth    = size * 0.95f;
        tr.endWidth      = 0f;
        tr.startColor    = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f);
        tr.endColor      = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        tr.numCapVertices = 2;
        tr.numCornerVertices = 3;
        tr.sortingOrder  = 49;
        tr.minVertexDistance = 0.01f;
        tr.autodestruct = false;

        // Orbit behavior — random direction, varying speeds + radial drift
        OrbitWindParticle p = obj.AddComponent<OrbitWindParticle>();
        p.center            = transform;
        p.angle             = angle;
        p.radius            = radius;
        p.angularSpeed      = Random.Range(70f, 220f) * (Random.value < 0.5f ? -1f : 1f);
        p.radiusDrift       = Random.Range(-0.45f, 0.45f);
        p.radiusWobble      = Random.Range(0.08f, 0.22f);
        p.wobbleSpeed       = Random.Range(2.5f, 5f);
        p.upwardDrift       = Random.Range(0.15f, 0.45f);
        p.lifetime          = Random.Range(1.3f, 2.1f);
        p.baseColor         = baseColor;
        p.essenceSpriteFn   = GetDotSprite;       // share the dot sprite for essence
        p.spawnsEssence     = true;
    }

    void SpawnSoftRing()
    {
        GameObject obj = new GameObject("SoftRing");
        obj.transform.position = transform.position;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetRingSprite();
        Color c         = Random.value < 0.5f ? Purple : Gold;
        sr.color        = new Color(c.r, c.g, c.b, 0.5f);
        sr.sortingOrder = 47;
        obj.transform.localScale = Vector3.one * 0.5f;
        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime   = 1.1f;
        r.startScale = 0.5f;
        r.endScale   = 3.6f;
    }

    void SpawnFinalRing(float delaySec, float endScale, float lifetime, Color color)
    {
        GameObject obj = new GameObject("FinalRing");
        obj.transform.position = transform.position;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetRingSprite();
        sr.color        = color;
        sr.sortingOrder = 98;
        obj.transform.localScale = Vector3.one * 0.5f;
        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime   = lifetime;
        r.startScale = 0.5f;
        r.endScale   = endScale;
        r.startDelay = delaySec;
    }

    // ── Procedural sprites ───────────────────────────────────────────────

    static Sprite GetDotSprite()
    {
        if (cachedDot != null) return cachedDot;
        int size = 16;
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
        cachedDot = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedDot;
    }

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

// One swirling wind particle. Orbits its center while drifting in radius and
// rising slightly. Lifetime-driven bell-curve alpha. Trail renders behind it.
// Optionally sheds tiny "essence" particles that drift off and fade.
public class OrbitWindParticle : MonoBehaviour
{
    public Transform center;
    public float angle;            // current angle (degrees)
    public float radius;
    public float angularSpeed;     // deg/sec, can be negative
    public float radiusDrift;      // radius change per second
    public float radiusWobble;     // amplitude of sinusoidal radius variation
    public float wobbleSpeed;
    public float upwardDrift;      // gentle vertical motion
    public float lifetime;
    public Color baseColor = Color.white;

    // Essence shedding — tiny sparkles that fall off the main particle as it moves
    public bool spawnsEssence = false;
    public System.Func<Sprite> essenceSpriteFn;

    private float elapsed;
    private float wobbleSeed;
    private float verticalOffset;
    private float essenceTimer;
    private SpriteRenderer sr;
    private Vector3 prevPos;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        wobbleSeed = Random.Range(0f, 100f);
        prevPos = transform.position;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (center == null || elapsed >= lifetime)
        {
            // Let any remaining trail fade out before final cleanup
            Destroy(gameObject, 0.6f);
            enabled = false;
            return;
        }

        angle  += angularSpeed * Time.deltaTime;
        radius += radiusDrift  * Time.deltaTime;
        verticalOffset += upwardDrift * Time.deltaTime;

        float r = radius + Mathf.Sin((elapsed + wobbleSeed) * wobbleSpeed) * radiusWobble;
        float rad = angle * Mathf.Deg2Rad;

        // Squashed ellipse for a more grounded feel
        Vector3 pos = center.position
            + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r * 0.65f + verticalOffset, 0f);
        transform.position = pos;

        // Bell-curve alpha: fade in, fade out
        float talpha = Mathf.Sin((elapsed / lifetime) * Mathf.PI);   // 0 → 1 → 0
        if (sr != null)
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, talpha);

        // Shed tiny essence sparkles perpendicular to current motion
        if (spawnsEssence)
        {
            essenceTimer += Time.deltaTime;
            if (essenceTimer >= 0.06f)
            {
                essenceTimer = 0f;
                ShedEssence(pos, talpha);
            }
        }

        prevPos = pos;
    }

    void ShedEssence(Vector3 pos, float parentAlpha)
    {
        GameObject e = new GameObject("Essence");
        e.transform.position = pos;

        SpriteRenderer esr = e.AddComponent<SpriteRenderer>();
        esr.sprite       = essenceSpriteFn != null ? essenceSpriteFn() : null;
        esr.color        = new Color(baseColor.r, baseColor.g, baseColor.b, parentAlpha * 0.9f);
        esr.sortingOrder = 49;
        e.transform.localScale = Vector3.one * Random.Range(0.035f, 0.075f);

        // Drift direction = motion-perpendicular + a touch outward + slight rise
        Vector3 motion = (pos - prevPos);
        Vector3 perp;
        if (motion.sqrMagnitude > 0.0001f)
        {
            motion.Normalize();
            // Perpendicular to motion in 2D
            perp = new Vector3(-motion.y, motion.x, 0f);
            if (Random.value < 0.5f) perp = -perp;
        }
        else
        {
            float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            perp = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        }
        Vector3 outward = (pos - center.position).normalized * 0.3f;

        RisingParticle rp = e.AddComponent<RisingParticle>();
        rp.lifetime   = Random.Range(0.35f, 0.65f);
        rp.velocity   = perp * Random.Range(0.4f, 0.9f) + outward + new Vector3(0f, Random.Range(0.1f, 0.4f), 0f);
        rp.scaleDecay = 0.4f;
    }
}
