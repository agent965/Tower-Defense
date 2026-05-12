using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting Economy")]
    public int startingGold = 100;

    [Header("Wave Reward")]
    public int goldPerWaveCompletion = 20;

    [Header("Kill Rewards — set gold per enemy type here")]
    public EnemyKillReward[] killRewards = new EnemyKillReward[]
    {
        new EnemyKillReward { type = EnemyType.Basic, gold = 6  },
        new EnemyKillReward { type = EnemyType.Fast,  gold = 4  },
        new EnemyKillReward { type = EnemyType.Heavy, gold = 12 },
        new EnemyKillReward { type = EnemyType.Tank,  gold = 18 },
    };

    [Tooltip("Extra gold awarded on top of the type reward when the enemy is a boss.")]
    public int bossKillBonus = 30;

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

    // Called by Enemy.Die() — looks up the type in the kill reward table
    public void AwardKillGold(EnemyType type, bool isBoss)
    {
        int reward = GetKillReward(type);
        if (isBoss) reward += bossKillBonus;
        AddGold(reward);
    }

    private int GetKillReward(EnemyType type)
    {
        foreach (var entry in killRewards)
            if (entry.type == type) return Mathf.Max(1, entry.gold);
        return 1; // fallback if type not in table
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

[Serializable]
public class EnemyKillReward
{
    public EnemyType type;
    public int gold;
}