using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    [Header("Wave Settings")]
    public int currentWave = 0;
    public int totalWaves = 10;
    public int enemiesPerWave = 5;
    public float enemiesIncreasePerWave = 2f; // how many more enemies each wave
    public float timeBetweenSpawns = 1f;
    
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Enemy Sprites (leave empty to use prefab sprite)")]
    public Sprite basicEnemySprite;
    public Sprite fastEnemySprite;
    public Sprite heavyEnemySprite;
    public Sprite tankEnemySprite;

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
        
        // Calculate enemies for this wave
        enemiesLeftToSpawn = Mathf.RoundToInt(enemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave);
        enemiesAlive = 0;
        
        Debug.Log($"Starting Wave {currentWave} with {enemiesLeftToSpawn} enemies");
        
        // Change game state to wave active
        GameManager.Instance.SetGameState(GameState.WaveActive);
        
        // Start spawning
        StartCoroutine(SpawnWave());
    }
    
    IEnumerator SpawnWave()
    {
        // Spawn all enemies for this wave
        while (enemiesLeftToSpawn > 0)
        {
            SpawnEnemy();
            enemiesLeftToSpawn--;
            
            // Wait before spawning next enemy
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
        
        Debug.Log($"All enemies spawned for wave {currentWave}. Waiting for them to be defeated...");
    }
    
    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("No enemy prefab assigned to WaveManager!");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point assigned to WaveManager!");
            return;
        }
        
        EnemyType type = PickEnemyType();

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemiesAlive++;

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.InitEnemy(type, GetSpriteForType(type));
            enemyScript.OnEnemyDestroyed += OnEnemyDestroyed;
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
        var pool = new List<EnemyType> { EnemyType.Basic };
        if (currentWave >= fastUnlockWave)  pool.Add(EnemyType.Fast);
        if (currentWave >= heavyUnlockWave) pool.Add(EnemyType.Heavy);
        if (currentWave >= tankUnlockWave)  pool.Add(EnemyType.Tank);
        return pool[Random.Range(0, pool.Count)];
    }

    private Sprite GetSpriteForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Basic:  return basicEnemySprite;
            case EnemyType.Fast:   return fastEnemySprite;
            case EnemyType.Heavy:  return heavyEnemySprite;
            case EnemyType.Tank:   return tankEnemySprite;
            default: return basicEnemySprite;
        }
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
