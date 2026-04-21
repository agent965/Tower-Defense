using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    // Desired size in world units — unaffected by enemy scale
    public float worldBarWidth  = 0.8f;
    public float worldBarHeight = 0.08f;
    public float worldYOffset   = 0.325f;

    private Transform fill;
    private SpriteRenderer fillRenderer;
    private bool initialized = false;
    private float localW, localH, localY;

    public void Initialize(float maxHP)
    {
        Transform old = transform.Find("HB_BG");
        if (old != null) Destroy(old.gameObject);
        old = transform.Find("HB_Fill");
        if (old != null) Destroy(old.gameObject);

        Vector3 ls = transform.lossyScale;
        localW = worldBarWidth  / Mathf.Abs(ls.x);
        localH = worldBarHeight / Mathf.Abs(ls.y);
        localY = worldYOffset   / Mathf.Abs(ls.y);

        SpriteRenderer enemySR = GetComponent<SpriteRenderer>();
        string layerName       = enemySR != null ? enemySR.sortingLayerName : "Default";

        GameObject bg = CreateQuad("HB_BG", new Color(0.15f, 0.15f, 0.15f, 0.85f), layerName, 200);
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, localY, 0f);
        bg.transform.localScale    = new Vector3(localW, localH, 1f);

        GameObject fillObj = CreateQuad("HB_Fill", Color.green, layerName, 201);
        fillObj.transform.SetParent(transform, false);
        fillObj.transform.localPosition = new Vector3(0f, localY, 0f);
        fillObj.transform.localScale    = new Vector3(localW, localH, 1f);

        fill         = fillObj.transform;
        fillRenderer = fillObj.GetComponent<SpriteRenderer>();
        initialized  = true;
    }

    public bool IsInitialized() => initialized;

    public void UpdateBar(float current, float max)
    {
        if (!initialized || fill == null) return;

        float pct = Mathf.Clamp01(current / max);

        fill.localScale    = new Vector3(localW * pct, localH, 1f);
        fill.localPosition = new Vector3(localW * (pct - 1f) / 2f, localY, 0f);

        if (fillRenderer != null)
            fillRenderer.color = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;
    }

    private static GameObject CreateQuad(string objName, Color color, string sortingLayer, int sortOrder)
    {
        GameObject obj = new GameObject(objName);

        int size = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite           = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sr.color            = color;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder     = sortOrder;

        return obj;
    }
}
