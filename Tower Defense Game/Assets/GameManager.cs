using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public GameState gameState = GameState.Building;
    
    // other scripts can listen to state changes to do shit
    public delegate void OnGameStateChanged(GameState newState);
    public event OnGameStateChanged GameStateChanged;
    
    void Awake()
    {
        // singleton :)
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
    
    // win/lose conditions
    public void TriggerGameOver()
    {
        SetGameState(GameState.GameOver);
        Time.timeScale = 0f; // Freeze game
        Debug.Log("GAME OVER - You Lost!");
        // our UI manager can listen to this state change to show game over screen
    }
    
    public void TriggerVictory()
    {
        SetGameState(GameState.Victory);
        Time.timeScale = 0f; // Freeze game
        Debug.Log("VICTORY - You Won!");
        // our UI manager can listen to this state change to show victory screen
    }
    
    // pause controls
    public void PauseGame()
    {
        if (!IsGameOver())
        {
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
        }
    }
    
    public void ResumeGame()
    {
        if (gameState == GameState.Paused)
        {
            Time.timeScale = 1f;
            SetGameState(GameState.Building);
        }
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
    
    // scene management
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        // SceneManager.LoadScene("MainMenu"); // uncomment when we have a main menu scene
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
    Building,      // Player place towers
    WaveActive,    // Wave is in progress, enemies spawning
    Paused,        // Game is paused
    GameOver,      // Player lost (0 lives)
    Victory        // Player won (survived all waves!!)
}