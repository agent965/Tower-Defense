using UnityEngine;

public class EndZone : MonoBehaviour
{
    // player loses life when enemy reaches end zone
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        HealthManager.Instance.LoseLife(1);
        Destroy(other.gameObject);
    }
}
