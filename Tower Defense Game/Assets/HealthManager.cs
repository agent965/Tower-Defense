using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }
    
    [Header("Lives Settings")]
    public int startingLives = 20;
    private int currentLives;
    
    // event for when lives change (UI can subscribe to this)
    public delegate void OnLivesChanged(int newLives);
    public event OnLivesChanged LivesChanged;
    
    void Awake()
    {
        // singleton!
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeLives();
    }
    
    void InitializeLives()
    {
        currentLives = startingLives;
        LivesChanged?.Invoke(currentLives);
        Debug.Log($"Lives Initialized: {currentLives}");
    }

    // lives management
    public void LoseLife(int amount = 1)
    {
        currentLives -= amount;
        LivesChanged?.Invoke(currentLives);
        
        Debug.Log($"Lost {amount} life! Remaining Lives: {currentLives}");
        
        // Check if game over
        if (currentLives <= 0)
        {
            currentLives = 0;
            GameManager.Instance.TriggerGameOver();
        }
    }
    
    public void AddLife(int amount = 1)
    {
        currentLives += amount;
        LivesChanged?.Invoke(currentLives);
        Debug.Log($"Gained {amount} life! Current Lives: {currentLives}");
    }
    
    void TriggerGameOver()
    {
        Debug.Log("Lives reached 0 - Game Over!");
        GameManager.Instance.TriggerGameOver();
    }
    
    // getters for current and max lives (for UI display)
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    public int GetMaxLives()
    {
        return startingLives;
    }
}