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
    
    [Header("Enemy Prefabs")]
    public GameObject enemyPrefab;
    
    [Header("Spawn Settings")]
    public Transform spawnPoint; // where enemies spawn
    
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
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemiesAlive++;
        
        // Subscribe to enemy death (we'll modify Enemy.cs to call this)
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
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
        
        // Return to building phase
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Building);
        }
        
        // You can add wave completion rewards here
        // MoneyManager.Instance.AddMoney(100 * currentWave);
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
