using System.Collections;
using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite airSprite;
    public Sprite groundSprite;

    [Header("Arc Settings")]
    public float arcHeight = 2f;
    public float travelTime = 1f;
    public float impactDuration = 0.3f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;

    private SpriteRenderer spriteRenderer;
    private bool hasLanded = false;

    public void Initialize(Vector3 target)
    {
        targetPosition = target;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = airSprite;

        startPosition = transform.position;
        timer = 0f;
    }

    void Update()
    {
        if (hasLanded) return;

        timer += Time.deltaTime;
        float t = timer / travelTime;

        if (t >= 1f)
        {
            Land();
            return;
        }

        Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, t);

        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;

        transform.position = linearPos + Vector3.up * height;
    }

    void Land()
    {
        hasLanded = true;

        transform.position = targetPosition;
        spriteRenderer.sprite = groundSprite;

        StartCoroutine(DestroyAfterImpact());
    }

    IEnumerator DestroyAfterImpact()
    {
        yield return new WaitForSeconds(impactDuration);
        Destroy(gameObject);
    }
}