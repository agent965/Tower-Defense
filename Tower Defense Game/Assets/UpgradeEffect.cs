using System.Collections.Generic;
using UnityEngine;

public class UpgradeEffect : MonoBehaviour
{
    private const float Duration = 1.4f;

    private static Sprite ringSprite;
    private static Sprite glowSprite;
    private static Sprite rayStripSprite;

    private float elapsed;
    private readonly List<RingPart> rings = new List<RingPart>();
    private readonly List<RayPart>  rays  = new List<RayPart>();
    private GlowPart glow;
    private GlowPart groundFlash;

    private struct RingPart
    {
        public Transform tf;
        public SpriteRenderer sr;
        public float startScale;
        public float endScale;
        public float startY;
        public float endY;
        public float delay;
        public Color tint;
    }

    private struct RayPart
    {
        public Transform tf;
        public SpriteRenderer sr;
        public float baseHeight;
        public float delay;
    }

    private struct GlowPart
    {
        public Transform tf;
        public SpriteRenderer sr;
        public float startScale;
        public float endScale;
        public Color tint;
    }

    public static void Play(Transform target)
    {
        if (target == null) return;

        GameObject obj = new GameObject("UpgradeEffect");
        obj.transform.position = target.position;
        UpgradeEffect fx = obj.AddComponent<UpgradeEffect>();
        fx.Build();
    }

    void Build()
    {
        EnsureSprites();

        // Bright golden ground flash that pops then fades
        groundFlash = MakeGlow(new Color(1f, 0.95f, 0.6f, 0.9f), 0.3f, 2.6f, sortingOrder: 95);

        // Halo glow behind the tower
        glow = MakeGlow(new Color(1f, 0.85f, 0.4f, 0.7f), 1.6f, 2.4f, sortingOrder: 102);

        // Ascending angelic rings — staggered for that "rising" cascade feel
        SpawnRing(0.00f, new Color(1f,    0.95f, 0.6f, 1f), 1.4f, 2.2f, -0.3f, 1.6f);
        SpawnRing(0.18f, new Color(1f,    1f,    0.85f, 1f), 1.2f, 1.9f, -0.2f, 1.7f);
        SpawnRing(0.36f, new Color(0.9f,  0.8f,  1f,    1f), 1.0f, 1.7f, -0.1f, 1.8f);
        SpawnRing(0.54f, new Color(1f,    0.95f, 0.7f,  1f), 0.8f, 1.5f,  0.0f, 1.9f);

        // Vertical light rays — like beams of light shooting up
        for (int i = 0; i < 6; i++)
            SpawnRay(i * 0.04f, Random.Range(-0.45f, 0.45f));
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / Duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Ground flash: quick pop in first 25%, then fade
        UpdateGroundFlash(t);

        // Halo glow: pulsing aura
        UpdateHalo(t);

        // Ascending rings
        for (int i = 0; i < rings.Count; i++)
            UpdateRing(rings[i], t);

        // Light rays
        for (int i = 0; i < rays.Count; i++)
            UpdateRay(rays[i], t);
    }

    void UpdateGroundFlash(float t)
    {
        if (groundFlash.tf == null) return;
        float k = t / 0.25f;
        if (k <= 1f)
        {
            float s = Mathf.Lerp(groundFlash.startScale, groundFlash.endScale, EaseOutCubic(k));
            groundFlash.tf.localScale = Vector3.one * s;
            Color c = groundFlash.tint;
            c.a = Mathf.Lerp(groundFlash.tint.a, 0f, k);
            groundFlash.sr.color = c;
        }
        else
        {
            groundFlash.sr.enabled = false;
        }
    }

    void UpdateHalo(float t)
    {
        if (glow.tf == null) return;
        float pulse = Mathf.Sin(t * Mathf.PI);
        float s = Mathf.Lerp(glow.startScale, glow.endScale, t) * (0.85f + 0.15f * pulse);
        glow.tf.localScale = Vector3.one * s;
        Color c = glow.tint;
        c.a = glow.tint.a * Mathf.Sin(t * Mathf.PI); // fade in then out
        glow.sr.color = c;
    }

    void UpdateRing(RingPart r, float globalT)
    {
        float local = (globalT - r.delay) / (1f - r.delay);
        if (local < 0f) { r.sr.enabled = false; return; }
        if (local > 1f) { r.sr.enabled = false; return; }
        r.sr.enabled = true;

        float eased = EaseOutCubic(local);
        float scale = Mathf.Lerp(r.startScale, r.endScale, eased);
        float y = Mathf.Lerp(r.startY, r.endY, eased);

        r.tf.localScale = Vector3.one * scale;
        r.tf.localPosition = new Vector3(0f, y, 0f);

        Color c = r.tint;
        // Fade in fast, then out
        float alpha = local < 0.2f
            ? Mathf.Lerp(0f, 1f, local / 0.2f)
            : Mathf.Lerp(1f, 0f, (local - 0.2f) / 0.8f);
        c.a *= alpha;
        r.sr.color = c;
    }

    void UpdateRay(RayPart ray, float globalT)
    {
        float local = (globalT - ray.delay) / (0.8f - ray.delay);
        if (local < 0f) { ray.sr.enabled = false; return; }
        if (local > 1f) { ray.sr.enabled = false; return; }
        ray.sr.enabled = true;

        float eased = EaseOutCubic(local);

        // Scale Y stretches up, X stays slim
        float h = ray.baseHeight * eased;
        ray.tf.localScale = new Vector3(0.08f, h, 1f);

        // Move pivot up so it grows from base
        ray.tf.localPosition = new Vector3(ray.tf.localPosition.x, h * 0.5f - 0.2f, 0f);

        // Fade in/out
        Color c = ray.sr.color;
        float alpha = local < 0.25f
            ? Mathf.Lerp(0f, 0.8f, local / 0.25f)
            : Mathf.Lerp(0.8f, 0f, (local - 0.25f) / 0.75f);
        c.a = alpha;
        ray.sr.color = c;
    }

    // ── Spawners ───────────────────────────────────────────────────────────

    void SpawnRing(float delay, Color tint, float startScale, float endScale, float startY, float endY)
    {
        GameObject obj = new GameObject("Ring");
        obj.transform.SetParent(transform, false);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ringSprite;
        sr.color = tint;
        sr.sortingOrder = 103;
        sr.enabled = false;

        rings.Add(new RingPart
        {
            tf = obj.transform,
            sr = sr,
            startScale = startScale,
            endScale = endScale,
            startY = startY,
            endY = endY,
            delay = delay,
            tint = tint,
        });
    }

    void SpawnRay(float delay, float xOffset)
    {
        GameObject obj = new GameObject("Ray");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(xOffset, 0f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = rayStripSprite;
        sr.color = new Color(1f, 0.95f, 0.7f, 0f);
        sr.sortingOrder = 104;
        sr.enabled = false;

        rays.Add(new RayPart
        {
            tf = obj.transform,
            sr = sr,
            baseHeight = Random.Range(1.3f, 2.2f),
            delay = delay,
        });
    }

    GlowPart MakeGlow(Color tint, float startScale, float endScale, int sortingOrder)
    {
        GameObject obj = new GameObject("Glow");
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = Vector3.one * startScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = glowSprite;
        sr.color = tint;
        sr.sortingOrder = sortingOrder;

        return new GlowPart
        {
            tf = obj.transform,
            sr = sr,
            startScale = startScale,
            endScale = endScale,
            tint = tint,
        };
    }

    // ── Procedural sprite generation (cached statically) ───────────────────

    static void EnsureSprites()
    {
        if (ringSprite == null)     ringSprite     = BuildRingSprite();
        if (glowSprite == null)     glowSprite     = BuildGlowSprite();
        if (rayStripSprite == null) rayStripSprite = BuildRaySprite();
    }

    static Sprite BuildRingSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float outerR = center - 1f;
        float innerR = center - 10f; // thicker ring
        float softR  = center - 16f; // soft inner falloff

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a;
                if (d > outerR)
                    a = 0f;
                else if (d > innerR)
                    a = Mathf.SmoothStep(0f, 1f, (outerR - d) / (outerR - innerR));
                else if (d > softR)
                    a = Mathf.SmoothStep(0f, 0.6f, (d - softR) / (innerR - softR));
                else
                    a = 0f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite BuildGlowSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float maxR = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float t = Mathf.Clamp01(1f - d / maxR);
                float a = t * t; // soft radial falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite BuildRaySprite()
    {
        // Vertical strip with soft horizontal falloff and top fade
        const int w = 16;
        const int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float cx = w / 2f;
        for (int y = 0; y < h; y++)
        {
            float yt = y / (float)(h - 1); // 0 at bottom, 1 at top
            float vertical = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(yt - 0.5f) * 2f); // peaks in middle
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx) / cx;
                float horizontal = 1f - dx;
                horizontal = horizontal * horizontal;
                float a = horizontal * vertical;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32);
    }

    static float EaseOutCubic(float x)
    {
        float v = 1f - x;
        return 1f - v * v * v;
    }
}
