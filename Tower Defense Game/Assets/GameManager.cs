using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public GameState gameState = GameState.Building;
    
    // other scripts can listen to state changes
    public delegate void OnGameStateChanged(GameState newState);
    public event OnGameStateChanged GameStateChanged;
    
    void Awake()
    {
        // singleton
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
        InitializeGame();
    }
    
    void InitializeGame()
    {
        SetGameState(GameState.Building);
        Debug.Log("Game Initialized - Ready to Build");
    }
    
    // game state management
    public void SetGameState(GameState newState)
    {
        gameState = newState;
        GameStateChanged?.Invoke(newState);
        Debug.Log($"Game State Changed: {newState}");
    }
    
    public bool IsBuilding()
    {
        return gameState == GameState.Building;
    }
    
    public bool IsWaveActive()
    {
        return gameState == GameState.WaveActive;
    }
    
    public bool IsGameOver()
    {
        return gameState == GameState.GameOver || gameState == GameState.Victory;
    }
    
    // win/lose conditions - refactored to reduce duplication
    public void TriggerGameOver()
    {
        EndGame(GameState.GameOver, "GAME OVER - You Lost!");
    }
    
    public void TriggerVictory()
    {
        EndGame(GameState.Victory, "VICTORY - You Won!");
    }
    
    private void EndGame(GameState endState, string message)
    {
        SetGameState(endState);
        Time.timeScale = 0f; // Freeze game
        Debug.Log(message);
    }
    
    // pause controls - refactored
    public void PauseGame()
    {
        if (!IsGameOver())
        {
            SetTimeScale(0f, GameState.Paused);
        }
    }
    
    public void ResumeGame()
    {
        if (gameState == GameState.Paused)
        {
            SetTimeScale(1f, GameState.Building);
        }
    }
    
    private void SetTimeScale(float scale, GameState newState)
    {
        Time.timeScale = scale;
        SetGameState(newState);
    }
    
    public void TogglePause()
    {
        if (gameState == GameState.Paused)
        {
            ResumeGame();
        }
        else if (!IsGameOver())
        {
            PauseGame();
        }
    }
    
    // scene management - refactored
    public void RestartGame()
    {
        ResetTimeAndLoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadMainMenu()
    {
        ResetTimeAndLoadScene("MainMenu"); // uncomment when you have a main menu scene
        // For now, this won't work without a MainMenu scene
    }
    
    private void ResetTimeAndLoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }
}

// enum for different game states
public enum GameState
{
    Building,      // Player places towers
    WaveActive,    // Wave is in progress, enemies spawning
    Paused,        // Game is paused
    GameOver,      // Player lost (0 lives)
    Victory        // Player won (survived all waves!!)
}
