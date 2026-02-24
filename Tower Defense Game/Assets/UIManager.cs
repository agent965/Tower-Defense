using UnityEngine;
using TMPro; // Use this if we use TextMeshPro
using UnityEngine.UI; // For Button component

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Text Elements")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI MoneyText;
    public TextMeshProUGUI ShieldText;
    public TextMeshProUGUI WaveText;
    public TextMeshProUGUI EnemiesText;
    
    [Header("UI Buttons")]
    public GameObject BuyShield; // Button or panel for buying shield
    public Button StartWaveButton; // Button to start wave
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Subscribe to health changes
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged += UpdateHealthUI;
            UpdateHealthUI(HealthManager.Instance.GetCurrentLives());
        }
        else
        {
            Debug.LogError("UIManager: HealthManager.Instance is null!");
        }
        
        // Subscribe to game state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogError("UIManager: GameManager.Instance is null!");
        }
        
        // Set up Start Wave button
        if (StartWaveButton != null)
        {
            StartWaveButton.onClick.AddListener(OnStartWaveButtonClicked);
        }
        
        // Initialize wave display
        UpdateWaveUI();
    }
    
    void UpdateHealthUI(int newHealth)
    {
        if (HealthText != null)
        {
            HealthText.text = $"Health: {newHealth}";
        }
    }
    
    void OnStartWaveButtonClicked()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StartWave();
        }
    }
    
    void OnGameStateChanged(GameState newState)
    {
        UpdateButtonVisibility(newState);
        UpdateWaveUI();
    }
    
    void UpdateButtonVisibility(GameState state)
    {
        if (StartWaveButton != null)
        {
            // Only show button during building phase
            StartWaveButton.gameObject.SetActive(state == GameState.Building);
        }
    }
    
    void UpdateWaveUI()
    {
        if (WaveManager.Instance == null) return;
        
        if (WaveText != null)
        {
            WaveText.text = $"Wave: {WaveManager.Instance.GetCurrentWave()}";
        }
        
        if (EnemiesText != null)
        {
            int alive = WaveManager.Instance.GetEnemiesAlive();
            int toSpawn = WaveManager.Instance.GetEnemiesLeftToSpawn();
            
            if (WaveManager.Instance.IsWaveInProgress())
            {
                EnemiesText.text = $"Enemies: {alive} alive, {toSpawn} remaining";
            }
            else
            {
                EnemiesText.text = "Ready to start wave";
            }
        }
    }
    
    void Update()
    {
        // Update enemy count during wave
        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveInProgress())
        {
            UpdateWaveUI();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged -= UpdateHealthUI;
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        }
        
        if (StartWaveButton != null)
        {
            StartWaveButton.onClick.RemoveListener(OnStartWaveButtonClicked);
        }
    }
}