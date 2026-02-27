using UnityEngine;

public class BasicProjectile : MonoBehaviour
{
    public float spd;
    public float rng;

    private Vector2 dir;
    private Vector2 initPos;
    private int hits;
    private int prc;
    private void Awake()
    {
        // Ensure physics will detect collisions
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        // Make sure the collider is a trigger
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc == null)
        {
            bc = gameObject.AddComponent<BoxCollider2D>();
        }

        bc.isTrigger = true;
    }

    // Call this when spawning the projectile
    public void SetAttributes(float angleDegrees, float speed, float range, Vector2 startPosition, double damage, int pierce)
    {
        hits = 0;
        spd = speed;
        rng = range;
        initPos = startPosition;
        prc = pierce;

        float radians = angleDegrees * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

        transform.rotation = Quaternion.Euler(0, 0, angleDegrees);
    }

    void Update()
    {
        transform.position += (Vector3)dir * spd * Time.deltaTime;

        // Destroy if out of range
        if (Vector2.Distance(initPos, transform.position) >= rng)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only destroy when hitting enemies
        if (collision.CompareTag("Enemy") && hits >= prc)
        {
            Destroy(gameObject);
        }
    }
}