using System.Collections.Generic;
using UnityEngine;

// DBZ-style power-up aura. Self-contained — call ChargeUpEffect.Play(...) and
// it spawns a GameObject that handles everything, invokes onComplete, and self-destructs.
public class ChargeUpEffect : MonoBehaviour
{
    public static void Play(Transform tower, float duration, System.Action onComplete = null)
    {
        GameObject obj = new GameObject("ChargeUpEffect");
        obj.transform.position = tower.position;

        ChargeUpEffect c = obj.AddComponent<ChargeUpEffect>();
        c.tower      = tower;
        c.duration   = duration;
        c.onComplete = onComplete;
        c.Build();
    }

    private Transform tower;
    private float duration = 2.5f;
    private System.Action onComplete;

    private float elapsed;
    private float lastRingTime;

    private SpriteRenderer mainAura;
    private SpriteRenderer innerCore;       // brighter white-hot core inside the red flame
    private readonly List<SpriteRenderer> spikes = new List<SpriteRenderer>();
    private float lightningTimer;
    private float sparkTimer;
    private float burstTimer;

    private static Sprite cachedFlame;
    private static Sprite cachedSpike;
    private static Sprite cachedRing;
    private static Sprite cachedGlow;
    private static Sprite cachedLightning;

    void Build()
    {
        // ── Main vertical aura behind tower (outer red glow) ─────────────
        GameObject auraObj = new GameObject("MainAura");
        auraObj.transform.SetParent(transform, false);
        auraObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        mainAura = auraObj.AddComponent<SpriteRenderer>();
        mainAura.sprite       = GetFlameSprite();
        mainAura.color        = new Color(1f, 0.18f, 0.05f, 0f);
        mainAura.sortingOrder = 50;
        auraObj.transform.localScale = new Vector3(2.5f, 3.8f, 1f);

        // ── Inner core aura (bright white-yellow, smaller, hotter) ───────
        GameObject coreObj = new GameObject("InnerCore");
        coreObj.transform.SetParent(transform, false);
        coreObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        innerCore = coreObj.AddComponent<SpriteRenderer>();
        innerCore.sprite       = GetFlameSprite();
        innerCore.color        = new Color(1f, 0.95f, 0.55f, 0f);
        innerCore.sortingOrder = 51;
        coreObj.transform.localScale = new Vector3(1.3f, 2.3f, 1f);

        // ── 12 radiating spikes ──────────────────────────────────────────
        const int spikeCount = 12;
        for (int i = 0; i < spikeCount; i++)
        {
            GameObject s = new GameObject($"Spike_{i}");
            s.transform.SetParent(transform, false);
            float angle = (i / (float)spikeCount) * 360f;
            s.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            s.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = s.AddComponent<SpriteRenderer>();
            sr.sprite       = GetSpikeSprite();
            sr.color        = new Color(1f, 0.4f, 0.15f, 0f);
            sr.sortingOrder = 52;
            s.transform.localScale = new Vector3(0.35f, 1.0f, 1f);

            spikes.Add(sr);
        }

        // ── Ground glow ──────────────────────────────────────────────────
        GameObject glow = new GameObject("GroundGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
        glowSR.sprite       = GetGlowSprite();
        glowSR.color        = new Color(1f, 0.2f, 0.1f, 0f);
        glowSR.sortingOrder = 49;
        glow.transform.localScale = Vector3.one * 2.5f;
        // store glow as the first spike so we update its color in Update (hacky but ok)
        // ...actually let me animate it separately:
        groundGlow = glowSR;
    }

    private SpriteRenderer groundGlow;

    void Update()
    {
        elapsed += Time.deltaTime;
        if (tower != null) transform.position = tower.position;

        float t = Mathf.Clamp01(elapsed / duration);
        float intensity = Mathf.Pow(t, 0.6f);     // ramps up faster early
        float chaos     = Mathf.Pow(t, 1.8f);     // builds slowly then peaks

        // Flicker — high-frequency multiplier that adds visual chaos
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 18f, 0f) - 0.5f) * 0.6f * intensity;

        // Main aura — bigger pulse, faster wobble, brighter
        if (mainAura != null)
        {
            float pulse  = 1f + Mathf.Sin(Time.time * 24f) * 0.15f * intensity;
            float wobble = Mathf.Sin(Time.time * 9f) * 0.15f;
            mainAura.transform.localScale = new Vector3(2.5f * pulse + wobble, 3.8f * pulse, 1f);
            mainAura.color = new Color(1f, 0.18f, 0.05f, intensity * flicker);  // alpha can exceed 1 → clipped, looks brighter
        }

        // Inner core — faster, smaller, white-hot
        if (innerCore != null)
        {
            float pulse  = 1f + Mathf.Sin(Time.time * 32f) * 0.18f * intensity;
            float wobble = Mathf.Sin(Time.time * 14f + 1.5f) * 0.10f;
            innerCore.transform.localScale = new Vector3(1.3f * pulse + wobble, 2.3f * pulse, 1f);
            innerCore.color = new Color(1f, 0.95f, 0.55f, 0.85f * intensity * flicker);
        }

        // Spikes — brighter (white-hot tips), longer at peak, rotate faster
        float spikeSpin = Time.time * 55f;
        for (int i = 0; i < spikes.Count; i++)
        {
            SpriteRenderer s = spikes[i];
            if (s == null) continue;
            float per = Mathf.Sin(Time.time * 28f + i * 0.7f) * 0.5f + 0.5f;
            float len = 1f + per * 1.0f;
            s.transform.localScale = new Vector3(0.38f + per * 0.20f, len * (0.5f + intensity * 1.8f), 1f);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, (i / (float)spikes.Count) * 360f - 90f + spikeSpin);
            // Color shifts toward white-yellow at peaks for a "burning hot" feel
            float hot = per * intensity;
            s.color = new Color(1f, 0.45f + hot * 0.45f, 0.2f + hot * 0.5f, intensity * flicker);
        }

        // Ground glow — slow pulse
        if (groundGlow != null)
        {
            float gp = 1f + Mathf.Sin(Time.time * 5f) * 0.15f;
            groundGlow.transform.localScale = Vector3.one * 2.5f * gp;
            groundGlow.color = new Color(1f, 0.2f, 0.1f, 0.55f * intensity);
        }

        // Rising flame particles — much more frequent as intensity climbs
        float flameChance = intensity * 2.0f * Time.deltaTime / 0.03f;
        if (Random.value < flameChance)
            SpawnRisingFlame(intensity);

        // Shockwave rings every ~0.4s
        if (elapsed > 0.2f && elapsed - lastRingTime > 0.4f)
        {
            SpawnShockRing(intensity);
            lastRingTime = elapsed;
        }

        // Lightning crackles — random short-lived jagged streaks
        lightningTimer -= Time.deltaTime;
        if (lightningTimer <= 0f)
        {
            int strikes = Random.Range(1, 3);
            for (int i = 0; i < strikes; i++) SpawnLightning(intensity);
            lightningTimer = Mathf.Lerp(0.35f, 0.10f, intensity);
        }

        // Sparks — small bright dots flying outward
        sparkTimer -= Time.deltaTime;
        if (sparkTimer <= 0f)
        {
            int count = 2 + Mathf.RoundToInt(intensity * 4f);
            for (int i = 0; i < count; i++) SpawnSpark(intensity);
            sparkTimer = Mathf.Lerp(0.15f, 0.05f, intensity);
        }

        // Burst flashes — random sudden expanding rings for chaotic flicker
        burstTimer -= Time.deltaTime;
        if (burstTimer <= 0f && intensity > 0.3f)
        {
            SpawnBurstFlash(intensity);
            burstTimer = Random.Range(0.25f, 0.55f);
        }

        // Camera shake builds with chaos
        CameraShake.Shake(0.07f, 0.012f + chaos * 0.07f);

        if (elapsed >= duration)
            Finish();
    }

    void Finish()
    {
        // Massive final flash + heavy shake
        CameraShake.Shake(0.45f, 0.2f);
        SpawnFinalFlash();
        // a couple of expanding rings for impact
        SpawnFinalRing(0.0f, 6f, 0.5f, Color.white);
        SpawnFinalRing(0.05f, 5f, 0.6f, new Color(1f, 0.3f, 0.1f, 0.85f));

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    // ── Particle spawners ────────────────────────────────────────────────

    void SpawnRisingFlame(float intensity)
    {
        GameObject obj = new GameObject("RisingFlame");
        obj.transform.position = transform.position
            + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 0.15f), 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetFlameSprite();
        sr.color        = new Color(1f, Random.Range(0.3f, 0.55f), 0.1f, 0.75f);
        sr.sortingOrder = 52;
        obj.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);

        RisingParticle p = obj.AddComponent<RisingParticle>();
        p.lifetime = Random.Range(0.45f, 0.75f);
        p.velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.6f, 2.8f), 0f);
        p.scaleDecay = 0.6f;
    }

    void SpawnShockRing(float intensity)
    {
        GameObject obj = new GameObject("ShockRing");
        obj.transform.position = transform.position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetRingSprite();
        sr.color        = new Color(1f, 0.25f, 0.1f, 0.75f);
        sr.sortingOrder = 48;
        obj.transform.localScale = Vector3.one * 0.6f;

        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime = 0.6f;
        r.startScale = 0.6f;
        r.endScale = 4.2f;
    }

    void SpawnFinalFlash()
    {
        GameObject obj = new GameObject("FinalFlash");
        obj.transform.position = transform.position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetGlowSprite();
        sr.color        = new Color(1f, 1f, 1f, 1f);
        sr.sortingOrder = 100;
        obj.transform.localScale = Vector3.one * 1f;

        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime = 0.45f;
        r.startScale = 1f;
        r.endScale = 8f;
    }

    void SpawnLightning(float intensity)
    {
        GameObject obj = new GameObject("Lightning");
        // Pick a random direction around the tower, spawn near the tower edge
        float angle = Random.Range(0f, 360f);
        float dist  = Random.Range(0.3f, 0.9f);
        Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
        obj.transform.position = transform.position + dir * dist;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle + Random.Range(-30f, 30f));

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetLightningSprite();
        sr.color        = Random.value < 0.4f
            ? new Color(1f, 1f, 0.8f, 0.95f)              // bright white-yellow
            : new Color(1f, 0.5f + Random.value * 0.3f, 0.2f, 0.9f); // orange-red
        sr.sortingOrder = 60;
        obj.transform.localScale = new Vector3(Random.Range(0.4f, 0.9f), Random.Range(0.8f, 1.6f), 1f);

        ShortLived sl = obj.AddComponent<ShortLived>();
        sl.lifetime = Random.Range(0.06f, 0.12f);
    }

    void SpawnSpark(float intensity)
    {
        GameObject obj = new GameObject("Spark");
        obj.transform.position = transform.position
            + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.3f), 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetGlowSprite();
        sr.color        = Random.value < 0.3f
            ? new Color(1f, 1f, 0.9f, 1f)                 // white-yellow spark
            : new Color(1f, 0.5f + Random.value * 0.4f, 0.15f, 1f);
        sr.sortingOrder = 55;
        obj.transform.localScale = Vector3.one * Random.Range(0.10f, 0.20f);

        RisingParticle p = obj.AddComponent<RisingParticle>();
        p.lifetime = Random.Range(0.35f, 0.55f);
        // Spark direction: random outward velocity
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = Random.Range(2.5f, 5f);
        p.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 0.5f, 0f);
        p.scaleDecay = 0.8f;
    }

    void SpawnBurstFlash(float intensity)
    {
        GameObject obj = new GameObject("BurstFlash");
        obj.transform.position = transform.position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetGlowSprite();
        sr.color        = new Color(1f, 0.6f, 0.3f, 0.7f);
        sr.sortingOrder = 53;
        obj.transform.localScale = Vector3.one * 0.8f;

        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime = 0.25f;
        r.startScale = 0.8f;
        r.endScale = 2.5f + intensity * 1.5f;
    }

    void SpawnFinalRing(float delaySec, float endScale, float lifetime, Color color)
    {
        GameObject obj = new GameObject("FinalRing");
        obj.transform.position = transform.position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetRingSprite();
        sr.color        = color;
        sr.sortingOrder = 99;
        obj.transform.localScale = Vector3.one * 0.5f;

        ExpandingRing r = obj.AddComponent<ExpandingRing>();
        r.lifetime = lifetime;
        r.startScale = 0.5f;
        r.endScale = endScale;
        r.startDelay = delaySec;
    }

    // ── Procedural sprites (cached statically — built once, reused) ─────

    static Sprite GetFlameSprite()
    {
        if (cachedFlame != null) return cachedFlame;
        int w = 32, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float yt = y / (float)(h - 1);
            float halfWidth = Mathf.Lerp(0.55f, 0.10f, yt);
            float vIntensity = 1f - Mathf.Pow(yt, 0.7f) * 0.7f;
            Color tip  = Color.Lerp(new Color(1f, 1f, 0.85f, 1f), new Color(1f, 0.3f, 0.05f, 1f), yt);

            for (int x = 0; x < w; x++)
            {
                float xt = (x - w / 2f) / (w / 2f);
                float xa = Mathf.Abs(xt);
                if (xa > halfWidth) { px[y * w + x] = Color.clear; continue; }
                float fade = 1f - (xa / halfWidth);
                fade = Mathf.Pow(fade, 1.4f) * vIntensity;
                Color c = tip; c.a = fade;
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedFlame = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), w);
        return cachedFlame;
    }

    static Sprite GetSpikeSprite()
    {
        if (cachedSpike != null) return cachedSpike;
        int w = 16, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float yt = y / (float)(h - 1);
            float halfW = (1f - yt) * 0.5f;
            for (int x = 0; x < w; x++)
            {
                float xt = (x - w / 2f) / (w / 2f);
                float xa = Mathf.Abs(xt);
                if (xa > halfW) { px[y * w + x] = Color.clear; continue; }
                float fade = 1f - xa / halfW;
                fade = Mathf.Pow(fade, 0.7f) * (1f - yt * 0.4f);
                px[y * w + x] = new Color(1f, 0.55f, 0.25f, fade);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedSpike = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
        return cachedSpike;
    }

    static Sprite GetRingSprite()
    {
        if (cachedRing != null) return cachedRing;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        float thickness = 3.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float dr = Mathf.Abs(d - radius);
            if (dr > thickness) { px[y * size + x] = Color.clear; continue; }
            float fade = 1f - dr / thickness;
            fade = Mathf.Pow(fade, 1.4f);
            px[y * size + x] = new Color(1f, 1f, 1f, fade);
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedRing;
    }

    static Sprite GetLightningSprite()
    {
        if (cachedLightning != null) return cachedLightning;
        int w = 16, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];

        // Draw a jagged zig-zag line down the middle
        int prevX = w / 2;
        for (int y = 0; y < h; y++)
        {
            // Jitter the x each row
            int jitter = (int)((Mathf.PerlinNoise(0f, y * 0.3f) - 0.5f) * 8f);
            int cx = Mathf.Clamp(w / 2 + jitter, 2, w - 3);

            // Draw between prevX and cx (small steps)
            int loX = Mathf.Min(prevX, cx);
            int hiX = Mathf.Max(prevX, cx);
            for (int x = 0; x < w; x++)
            {
                float dist;
                if (x < loX)       dist = loX - x;
                else if (x > hiX)  dist = x - hiX;
                else               dist = 0f;

                if (dist <= 2f)
                {
                    float core = 1f - dist / 2f;
                    px[y * w + x] = new Color(1f, 1f, 1f, core);
                }
                else
                {
                    px[y * w + x] = Color.clear;
                }
            }
            prevX = cx;
        }

        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedLightning = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        return cachedLightning;
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
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedGlow;
    }
}

// Small helper: drifts upward, fades, shrinks, self-destructs.
public class RisingParticle : MonoBehaviour
{
    public float lifetime = 0.5f;
    public Vector3 velocity;
    public float scaleDecay = 0.5f;

    private float elapsed;
    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 baseScale;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        baseScale = transform.localScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }

        transform.position += velocity * Time.deltaTime;
        velocity *= 0.95f;
        transform.localScale = baseScale * (1f - t * scaleDecay);

        if (sr != null)
        {
            Color c = baseColor;
            c.a = baseColor.a * (1f - t);
            sr.color = c;
        }
    }
}

// Tiny helper: shows for a short time then self-destructs (used for lightning flashes).
public class ShortLived : MonoBehaviour
{
    public float lifetime = 0.1f;
    private float elapsed;
    private SpriteRenderer sr;
    private Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }
        if (sr != null)
        {
            Color c = baseColor;
            // Fast fade-out — lightning lingers briefly then snaps out
            c.a = baseColor.a * Mathf.Pow(1f - t, 2f);
            sr.color = c;
        }
    }
}

// Small helper: scales out from start to end, fades, self-destructs.
public class ExpandingRing : MonoBehaviour
{
    public float lifetime  = 0.5f;
    public float startScale = 0.5f;
    public float endScale   = 4f;
    public float startDelay = 0f;

    private float elapsed;
    private SpriteRenderer sr;
    private Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        if (startDelay > 0f)
        {
            transform.localScale = Vector3.zero;
            if (sr != null) sr.enabled = false;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < startDelay) return;
        if (sr != null && !sr.enabled) sr.enabled = true;

        float t = (elapsed - startDelay) / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }

        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        if (sr != null)
        {
            Color c = baseColor;
            c.a = baseColor.a * (1f - t);
            sr.color = c;
        }
    }
}
