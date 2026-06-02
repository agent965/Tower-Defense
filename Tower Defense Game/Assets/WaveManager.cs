using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [Header("Wave Settings")]
    public int currentWave = 0;
    public int totalWaves = 10;
    public int enemiesPerWave = 10;
    public float enemiesIncreasePerWave = 3f; // how many more enemies each wave
    public float timeBetweenSpawns = 1f;

    [Header("Difficulty Scaling")]
    public float hpScalingPerWave    = 0.15f;  // +15% HP per wave
    public float speedScalingPerWave = 0.05f;  // +5% speed per wave
    public float goldScalingPerWave  = 0.07f;  // +7% gold per wave
    public float spawnIntervalMin    = 0.4f;   // fastest possible spawn rate
    public float spawnIntervalDecrease = 0.06f; // seconds faster per wave

    [Header("Boss Settings")]
    public int   bossWaveInterval    = 5;      // boss spawns every N waves
    public float bossBaseHpMult      = 4f;     // multiplied by (wave/bossWaveInterval)
    public float bossSpeedMult       = 0.65f;
    public int   bossGoldMult        = 3;
    
    [Header("Enemy Prefabs")]
    public GameObject basicEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject heavyEnemyPrefab;
    public GameObject tankEnemyPrefab;

    [Header("Enemy Type Unlock Waves")]
    public int fastUnlockWave  = 3;
    public int heavyUnlockWave = 5;
    public int tankUnlockWave  = 7;

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    
    // tracking
    private int enemiesAlive = 0;
    private int enemiesLeftToSpawn = 0;
    private bool waveInProgress = false;
    
    // events
    public delegate void OnWaveComplete(int waveNumber);
    public event OnWaveComplete WaveComplete;
    
    public delegate void OnAllWavesComplete();
    public event OnAllWavesComplete AllWavesComplete;
    
    void Awake()
    {
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
        // Make sure we're in building mode at start
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Building);
        }
    }
    
    public void StartWave()
    {
        if (waveInProgress)
        {
            Debug.LogWarning("Wave already in progress!");
            return;
        }
        
        if (GameManager.Instance == null || HealthManager.Instance == null)
        {
            Debug.LogError("GameManager or HealthManager not found!");
            return;
        }
        
        currentWave++;
        waveInProgress = true;

        // Spawn interval shrinks each wave, floored at minimum
        timeBetweenSpawns = Mathf.Max(spawnIntervalMin, 1.0f - (currentWave - 1) * spawnIntervalDecrease);

        // Calculate enemies for this wave (+1 slot reserved for boss when applicable)
        int regularCount = Mathf.RoundToInt(enemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave);
        bool isBossWave = currentWave % bossWaveInterval == 0;
        enemiesLeftToSpawn = regularCount;
        enemiesAlive = isBossWave ? regularCount + 1 : regularCount;

        Debug.Log($"Starting Wave {currentWave} with {regularCount} enemies{(isBossWave ? " + BOSS" : "")} (spawn interval {timeBetweenSpawns:F2}s)");

        // Change game state to wave active
        GameManager.Instance.SetGameState(GameState.WaveActive);

        AudioManager.Instance?.PlayWaveStart();

        // Start spawning
        StartCoroutine(SpawnWave());
    }
    
    IEnumerator SpawnWave()
    {
        while (enemiesLeftToSpawn > 0)
        {
            SpawnEnemy();
            enemiesLeftToSpawn--;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        // Boss spawns at the end of every boss wave with a dramatic pause
        if (currentWave % bossWaveInterval == 0)
        {
            yield return new WaitForSeconds(timeBetweenSpawns * 3f);
            SpawnBoss();
        }

        Debug.Log($"All enemies spawned for wave {currentWave}. Waiting for them to be defeated...");
    }
    
    void SpawnEnemy()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point assigned to WaveManager!");
            return;
        }

        EnemyType type   = PickEnemyType();
        GameObject prefab = GetPrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for enemy type {type}!");
            return;
        }

        GameObject enemy       = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        Enemy      enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.InitEnemy(type, null);
            enemyScript.ScaleStats(
                1f + (currentWave - 1) * hpScalingPerWave,
                1f + (currentWave - 1) * speedScalingPerWave,
                1f + (currentWave - 1) * goldScalingPerWave
            );
            enemyScript.OnEnemyDestroyed += OnEnemyDestroyed;
        }
    }

    void SpawnBoss()
    {
        if (spawnPoint == null) return;

        // Use the heaviest available prefab for the boss
        GameObject prefab = tankEnemyPrefab != null ? tankEnemyPrefab : heavyEnemyPrefab != null ? heavyEnemyPrefab : basicEnemyPrefab;
        if (prefab == null) return;

        GameObject boss       = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        Enemy      bossScript = boss.GetComponent<Enemy>();
        if (bossScript != null)
        {
            bossScript.InitEnemy(EnemyType.Tank, null);
            float bossHpMult = bossBaseHpMult * (currentWave / bossWaveInterval);
            bossScript.ScaleStats(bossHpMult, bossSpeedMult, bossGoldMult);
            bossScript.isBoss = true;
            bossScript.OnEnemyDestroyed += OnEnemyDestroyed;
        }

        AudioManager.Instance?.PlayBossSpawn();
        Debug.Log($"BOSS spawned for wave {currentWave}!");
    }

    private GameObject GetPrefabForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Basic:  return basicEnemyPrefab;
            case EnemyType.Fast:   return fastEnemyPrefab;
            case EnemyType.Heavy:  return heavyEnemyPrefab;
            case EnemyType.Tank:   return tankEnemyPrefab;
            default: return basicEnemyPrefab;
        }
    }
    
    public void OnEnemyDestroyed()
    {
        enemiesAlive--;
        Debug.Log($"Enemy destroyed. Remaining: {enemiesAlive}");
        
        // Check if wave is complete
        if (enemiesAlive <= 0 && enemiesLeftToSpawn <= 0)
        {
            CompleteWave();
        }
    }
    
    void CompleteWave()
    {
        waveInProgress = false;
        Debug.Log($"Wave {currentWave} Complete!");

        WaveComplete?.Invoke(currentWave);
        AudioManager.Instance?.PlayWaveComplete();

        if (currentWave >= totalWaves)
        {
            AllWavesComplete?.Invoke();
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVictory();
            return;
        }

        // Return to building phase
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Building);
        }
    }
    
    private EnemyType PickEnemyType()
    {
        // Weights grow linearly from unlock wave so harder enemies become progressively dominant
        float basicW = Mathf.Max(0.05f, 1.0f - currentWave * 0.09f);
        float fastW  = currentWave >= fastUnlockWave  ? Mathf.Min(0.30f, (currentWave - fastUnlockWave  + 1) * 0.08f) : 0f;
        float heavyW = currentWave >= heavyUnlockWave ? Mathf.Min(0.40f, (currentWave - heavyUnlockWave + 1) * 0.10f) : 0f;
        float tankW  = currentWave >= tankUnlockWave  ? Mathf.Min(0.50f, (currentWave - tankUnlockWave  + 1) * 0.15f) : 0f;

        float total = basicW + fastW + heavyW + tankW;
        float roll  = Random.Range(0f, total);

        if (roll < basicW) return EnemyType.Basic;
        roll -= basicW;
        if (roll < fastW)  return EnemyType.Fast;
        roll -= fastW;
        if (roll < heavyW) return EnemyType.Heavy;
        return EnemyType.Tank;
    }


    // Public getters for UI
    public bool IsWaveInProgress()
    {
        return waveInProgress;
    }
    
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    public int GetEnemiesAlive()
    {
        return enemiesAlive;
    }
    
    public int GetEnemiesLeftToSpawn()
    {
        return enemiesLeftToSpawn;
    }
}
