using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    public float speed = 5f;
    public float rotateSpeed = 720f;

    private Transform target;

    private void Awake()
    {
        // Ensure 2D physics works
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc == null)
        {
            bc = gameObject.AddComponent<BoxCollider2D>();
        }
        bc.isTrigger = true;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Direction toward target
        Vector2 dir = (Vector2)(target.position - transform.position);
        float distanceThisFrame = speed * Time.deltaTime;

        // Check if projectile will hit this frame
        if (dir.magnitude <= distanceThisFrame)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        transform.position += (Vector3)dir.normalized * distanceThisFrame;

        // Rotate for visuals
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            rotateSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only destroy on enemy collision
        if (collision.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}