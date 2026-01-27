using UnityEngine;

public class EndZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        GameManager.Instance.TakeBaseHit(1);
        Destroy(other.gameObject);
    }
}
