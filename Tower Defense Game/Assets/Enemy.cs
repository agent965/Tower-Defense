using UnityEngine;
public class Enemy : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float maxHP = 100f;
    private float currentHP;
    
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    
    // Event for when enemy is destroyed
    public delegate void OnEnemyDestroyedDelegate();
    public event OnEnemyDestroyedDelegate OnEnemyDestroyed;
    
    void Awake()
    {
        // Ensure enemy has a collider so projectiles can hit it
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
        }

        // Ensure enemy has a Rigidbody2D for trigger detection
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Ensure tag is set
        gameObject.tag = "Enemy";
    }

    void Start()
    {
        currentHP = maxHP;

        // get waypoints from PathManager
        if (PathManager.Instance != null)
        {
            waypoints = PathManager.Instance.GetWaypoints();
        }
        else
        {
            Debug.LogError("PathManager not found in scene!");
        }
    }
    
    void Update()
    {
        Move();
    }
    
    void Move()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        // check if we have more waypoints to go to
        if (currentWaypointIndex < waypoints.Length)
        {
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            transform.position = Vector2.MoveTowards(transform.position, 
                                                      targetWaypoint.position, 
                                                      moveSpeed * Time.deltaTime);
            
            // Check if reached waypoint
            if (Vector2.Distance(transform.position, targetWaypoint.position) < 0.1f)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            ReachedEnd();
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        // Notify WaveManager that this enemy was destroyed
        OnEnemyDestroyed?.Invoke();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AwardKillGold();

        Destroy(gameObject);
    }

    public void ReachedEnd()
    {
        // Notify WaveManager that this enemy was destroyed
        OnEnemyDestroyed?.Invoke();

        if (HealthManager.Instance != null)
            HealthManager.Instance.LoseLife(1);

        Destroy(gameObject);
    }
}