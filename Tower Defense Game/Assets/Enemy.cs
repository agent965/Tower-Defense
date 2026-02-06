using UnityEngine;
public class Enemy : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float maxHP = 100f;
    private float currentHP;
    
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    
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
        // we can also add death effects, award currency, drop loot, whatever here after enemy dies
        Destroy(gameObject);
    }
    
    void ReachedEnd()
    {
        // we should add logic here for what happens when an enemy reaches the end here, so probably damaging the player base
        Destroy(gameObject);
    }
}