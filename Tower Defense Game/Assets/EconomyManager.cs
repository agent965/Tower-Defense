using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting Economy")]
    public int startingGold = 100;

    [Header("Rewards")]
    public int goldPerKill = 10;
    public int goldPerWaveCompletion = 50;

    private int currentGold;

    // Event for UI to subscribe to
    public delegate void OnGoldChanged(int newGold);
    public event OnGoldChanged GoldChanged;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        currentGold = startingGold;
        GoldChanged?.Invoke(currentGold);

        // subscribe to wave completion
        if (WaveManager.Instance != null)
            WaveManager.Instance.WaveComplete += OnWaveComplete;
        else
            Debug.LogError("EconomyManager: WaveManager.Instance is null!");
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.WaveComplete -= OnWaveComplete;
    }

    // Called by Enemy.Die() when an enemy is killed
    public void AwardKillGold()
    {
        AddGold(goldPerKill);
    }

    void OnWaveComplete(int waveNumber)
    {
        AddGold(goldPerWaveCompletion);
        Debug.Log($"EconomyManager: Wave {waveNumber} bonus! +{goldPerWaveCompletion} gold");
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        GoldChanged?.Invoke(currentGold);
        Debug.Log($"EconomyManager: +{amount} gold. Total: {currentGold}");
    }

    // Returns false if not enough gold
    public bool SpendGold(int amount)
    {
        if (currentGold < amount)
        {
            Debug.Log("EconomyManager: Not enough gold!");
            return false;
        }

        currentGold -= amount;
        GoldChanged?.Invoke(currentGold);
        Debug.Log($"EconomyManager: Spent {amount} gold. Remaining: {currentGold}");
        return true;
    }

    public int GetCurrentGold() => currentGold;

    public bool CanAfford(int amount) => currentGold >= amount;
}