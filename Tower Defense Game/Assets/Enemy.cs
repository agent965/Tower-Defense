using UnityEngine;
public class Enemy : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float maxHP = 100f;
    public int goldValue = 10;
    public bool isBoss = false;
    private float currentHP;

    private EnemyHealthBar healthBar;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private string debuffed;
    private double debuffTimer = 0;
    private float slowFactor = 1f;
    private float slowDuration = 0f;
    
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

        healthBar = gameObject.AddComponent<EnemyHealthBar>();
    }

    void Start()
    {
        debuffed = "none";
        currentHP = maxHP;
        // Initialize here as a fallback for enemies not spawned through WaveManager
        if (!healthBar.IsInitialized())
            healthBar.Initialize(maxHP);

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

    private void FixedUpdate()
    {
        if (slowDuration > 0)
        {
            slowDuration -= Time.fixedDeltaTime;
            if (slowDuration <= 0)
            {
                slowFactor = 1f;
                debuffed = "none";
                debuffTimer = 0;
            }
        }

        if (debuffed == "Slow")
        {
            debuffTimer += 0.02;
            if (debuffTimer > 1)
            {
                debuffTimer = 0;
                TakeDamage(2);
            }
        }
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
                                                      moveSpeed * slowFactor * Time.deltaTime);
            
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
    
    public int   GetWaypointIndex() => currentWaypointIndex;
    public float GetCurrentHP()     => currentHP;

    public float GetDistanceToNextWaypoint()
    {
        if (waypoints == null || currentWaypointIndex >= waypoints.Length) return 0f;
        return Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position);
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        healthBar?.UpdateBar(currentHP, maxHP);

        // Floating damage number — golden-yellow for normal hits, red for big ones
        Color dmgColor = damage >= 50f
            ? new Color(1f, 0.4f, 0.3f, 1f)
            : new Color(1f, 0.9f, 0.3f, 1f);
        FloatingText.Spawn(transform.position, ((int)damage).ToString(), dmgColor);

        // Bosses shake the camera when they're hit
        if (isBoss)
            CameraShake.Shake(0.12f, 0.06f);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        OnEnemyDestroyed?.Invoke();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AddGold(goldValue);

        // Coin pop — bigger and brighter than damage numbers
        FloatingText.Spawn(
            transform.position,
            $"+{goldValue}g",
            new Color(1f, 0.85f, 0.15f, 1f),
            fontSize: 5f
        );

        // Hit-stop on every kill — short enough not to feel sluggish
        HitStop.Freeze(0.05f);

        Destroy(gameObject);
    }

    public void InitEnemy(EnemyType type, Sprite sprite)
    {
        // Stats (maxHP, moveSpeed, goldValue) are set on the prefab in the Inspector

        if (sprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
        }

        healthBar.Initialize(maxHP);
    }

    public void ReachedEnd()
    {
        // Notify WaveManager that this enemy was destroyed
        OnEnemyDestroyed?.Invoke();

        if (HealthManager.Instance != null)
            HealthManager.Instance.LoseLife(1);

        Destroy(gameObject);
    }

    public void Debuff(string db)
    {
        debuffed = db;
        if (db == "Slow")
        {
            slowFactor = 0.5f;
            slowDuration = 3f;
        }
    }
}

public enum EnemyType
{
    Basic,  // Standard enemy
    Fast,   // Low HP, high speed — unlocks wave 3
    Heavy,  // High HP, slow       — unlocks wave 5
    Tank,   // Very high HP, slow  — unlocks wave 7
}