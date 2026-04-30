using UnityEngine;

public class DebuffParticles : MonoBehaviour
{
    public enum Mode { None, Slow, Poison }

    private Mode currentMode = Mode.None;
    private float remaining;
    private float spawnTimer;

    private static Sprite cachedSnowflake;
    private static Sprite cachedBubble;

    public static void Apply(GameObject enemy, Mode mode, float duration)
    {
        DebuffParticles dp = enemy.GetComponent<DebuffParticles>();
        if (dp == null) dp = enemy.AddComponent<DebuffParticles>();
        dp.currentMode = mode;
        dp.remaining = duration;
        dp.spawnTimer = 0f;
    }

    void Update()
    {
        if (currentMode == Mode.None) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            currentMode = Mode.None;
            Destroy(this);
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnParticle();
            spawnTimer = currentMode == Mode.Slow ? 0.07f : 0.05f;
        }
    }

    void SpawnParticle()
    {
        GameObject p = new GameObject("DebuffParticle");
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.1f), 0f);
        p.transform.position = transform.position + offset;

        SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 150;

        DebuffParticle particle = p.AddComponent<DebuffParticle>();

        if (currentMode == Mode.Slow)
        {
            sr.sprite = GetSnowflakeSprite();
            sr.color = new Color(0.55f, 0.9f, 1f, 1f);
            particle.lifetime = 0.8f;
            particle.velocity = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 0.7f), 0f);
            particle.rotationSpeed = Random.Range(-180f, 180f);
            particle.startScale = 0.5f;
            particle.endScale = 0.3f;
        }
        else if (currentMode == Mode.Poison)
        {
            sr.sprite = GetBubbleSprite();
            sr.color = new Color(0.55f, 1f, 0.2f, 1f);
            particle.lifetime = 0.7f;
            particle.velocity = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.5f, 0.9f), 0f);
            particle.wobble = true;
            particle.startScale = 0.35f;
            particle.endScale = 0.65f;
        }
    }

    static Sprite GetSnowflakeSprite()
    {
        if (cachedSnowflake != null) return cachedSnowflake;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        Vector2 c = new Vector2(size / 2f, size / 2f);
        // 6-pointed star: three lines through center at 0°, 60°, 120°
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (float t = -size / 2f + 2f; t <= size / 2f - 2f; t += 0.5f)
            {
                int x = Mathf.RoundToInt(c.x + dir.x * t);
                int y = Mathf.RoundToInt(c.y + dir.y * t);
                if (x >= 0 && x < size && y >= 0 && y < size)
                    px[y * size + x] = Color.white;
                // thicken the line a bit
                if (x + 1 < size && y < size)
                    px[y * size + (x + 1)] = Color.white;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedSnowflake = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedSnowflake;
    }

    static Sprite GetBubbleSprite()
    {
        if (cachedBubble != null) return cachedBubble;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float maxR = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d > maxR)
                    px[y * size + x] = Color.clear;
                else
                {
                    // Soft falloff — bright center, fading edges
                    float a = 1f - (d / maxR);
                    a = a * a;
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedBubble = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedBubble;
    }
}

public class DebuffParticle : MonoBehaviour
{
    public float lifetime = 0.5f;
    public Vector3 velocity;
    public float rotationSpeed;
    public bool wobble;
    public float startScale = 0.2f;
    public float endScale = 0.2f;

    private float elapsed;
    private SpriteRenderer sr;
    private Color baseColor;
    private float wobbleSeed;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        wobbleSeed = Random.Range(0f, 100f);
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 v = velocity;
        if (wobble) v.x += Mathf.Sin((elapsed + wobbleSeed) * 8f) * 0.3f;
        transform.position += v * Time.deltaTime;

        if (rotationSpeed != 0f)
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

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
