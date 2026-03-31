using UnityEngine;

public class EndZone : MonoBehaviour
{
    // player loses life when enemy reaches end zone
private void OnTriggerEnter2D(Collider2D other)
{
    if (!other.CompareTag("Enemy")) return;
    
    
    
    Enemy enemy = other.GetComponent<Enemy>();
    if (enemy != null)
        enemy.ReachedEnd(); // fires OnEnemyDestroyed before destroying
    else
        Destroy(other.gameObject);
}
}
