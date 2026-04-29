using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float lifetime = 0.7f;

    private TextMeshPro tmp;
    private float elapsed;
    private Vector3 velocity;
    private Color baseColor;

    public static void Spawn(Vector3 worldPos, string text, Color color, float fontSize = 4f)
    {
        GameObject obj = new GameObject("FloatingText");
        obj.transform.position = worldPos + new Vector3(Random.Range(-0.15f, 0.15f), 0.3f, 0f);

        FloatingText ft = obj.AddComponent<FloatingText>();
        ft.Setup(text, color, fontSize);
    }

    void Setup(string text, Color color, float fontSize)
    {
        baseColor = color;

        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text         = text;
        tmp.fontSize     = fontSize;
        tmp.color        = color;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 200;

        // Keep the text rect small and centered so it doesn't push around
        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(2.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        velocity = new Vector3(Random.Range(-0.4f, 0.4f), 1.6f, 0f);
    }

    void Update()
    {
        // Use unscaled time so it animates during hit-stop
        elapsed += Time.unscaledDeltaTime;
        float t = elapsed / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.unscaledDeltaTime;
        velocity *= 0.94f;

        // Pop in, then fade out
        float scale = t < 0.15f
            ? Mathf.Lerp(0.4f, 1.15f, t / 0.15f)
            : Mathf.Lerp(1.15f, 0.9f, (t - 0.15f) / 0.85f);
        transform.localScale = Vector3.one * scale;

        Color c = baseColor;
        c.a = 1f - (t * t);
        if (tmp != null) tmp.color = c;
    }
}
