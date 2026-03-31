using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    public Button BuyShieldButton;
    public Button StartWaveButton;

    [Header("Tower Buy Buttons")]
    public Button BuyBasicTowerButton;
    public Button BuySniperTowerButton;
    public Button BuySprayTowerButton;
    public Button BuyRapidTowerButton;

    [Header("Tower Info")]
    public TextMeshProUGUI TowerInfoText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        Invoke(nameof(SubscribeToManagers), 0f);
    }

    void SubscribeToManagers()
    {
        // Subscribe to health changes
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged += UpdateHealthUI;
            UpdateHealthUI(HealthManager.Instance.GetCurrentLives());
        }
        else
            Debug.LogError("UIManager: HealthManager.Instance is null!");

        // Subscribe to gold changes
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.GoldChanged += UpdateMoneyUI;
            UpdateMoneyUI(EconomyManager.Instance.GetCurrentGold());
        }
        else
            Debug.LogError("UIManager: EconomyManager.Instance is null!");

        // Subscribe to shield changes
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.ShieldChanged += UpdateShieldUI;
            UpdateShieldUI(HealthManager.Instance.GetCurrentShields());
        }

        if (BuyShieldButton != null)
            BuyShieldButton.onClick.AddListener(OnBuyShieldButtonClicked);

        // Subscribe to game state changes
        if (GameManager.Instance != null)
            GameManager.Instance.GameStateChanged += OnGameStateChanged;

        // Set up Start Wave button
        if (StartWaveButton != null)
            StartWaveButton.onClick.AddListener(OnStartWaveButtonClicked);

        // Set up tower buy buttons
        if (BuyBasicTowerButton != null)
            BuyBasicTowerButton.onClick.AddListener(() => OnBuyTowerClicked(TowerPlacer.TowerType.Basic));
        if (BuySniperTowerButton != null)
            BuySniperTowerButton.onClick.AddListener(() => OnBuyTowerClicked(TowerPlacer.TowerType.Sniper));
        if (BuySprayTowerButton != null)
            BuySprayTowerButton.onClick.AddListener(() => OnBuyTowerClicked(TowerPlacer.TowerType.Spray));
        if (BuyRapidTowerButton != null)
            BuyRapidTowerButton.onClick.AddListener(() => OnBuyTowerClicked(TowerPlacer.TowerType.Rapid));

        // Initialize wave display
        UpdateWaveUI();
        UpdateTowerButtonText();
    }

    void UpdateHealthUI(int newHealth)
    {
        if (HealthText != null)
            HealthText.text = $"Health: {newHealth}";
    }

    void UpdateMoneyUI(int newGold)
    {
        if (MoneyText != null)
            MoneyText.text = $"Gold: {newGold}";
        UpdateTowerButtonAffordability();
    }

    void UpdateShieldUI(int newShields)
    {
        if (ShieldText != null)
            ShieldText.text = $"Shields: {newShields}";
    }

    // --- Tower buttons ---

    void OnBuyTowerClicked(TowerPlacer.TowerType type)
    {
        if (TowerPlacer.Instance != null)
            TowerPlacer.Instance.StartPlacing(type);
    }

    void UpdateTowerButtonText()
    {
        SetButtonText(BuyBasicTowerButton, $"Basic ({TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Basic)}g)");
        SetButtonText(BuySniperTowerButton, $"Sniper ({TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Sniper)}g)");
        SetButtonText(BuySprayTowerButton, $"Spray ({TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Spray)}g)");
        SetButtonText(BuyRapidTowerButton, $"Rapid ({TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Rapid)}g)");
    }

    void UpdateTowerButtonAffordability()
    {
        if (EconomyManager.Instance == null) return;
        int gold = EconomyManager.Instance.GetCurrentGold();

        SetButtonInteractable(BuyBasicTowerButton, gold >= TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Basic));
        SetButtonInteractable(BuySniperTowerButton, gold >= TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Sniper));
        SetButtonInteractable(BuySprayTowerButton, gold >= TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Spray));
        SetButtonInteractable(BuyRapidTowerButton, gold >= TowerPlacer.GetTowerCost(TowerPlacer.TowerType.Rapid));
    }

    void SetButtonText(Button btn, string text)
    {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    // --- Existing buttons ---

    void OnStartWaveButtonClicked()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.StartWave();
    }

    void OnBuyShieldButtonClicked()
    {
        HealthManager.Instance.BuyShield();
    }

    void OnGameStateChanged(GameState newState)
    {
        UpdateButtonVisibility(newState);
        UpdateWaveUI();
    }

    void UpdateButtonVisibility(GameState state)
    {
        bool building = state == GameState.Building;

        if (StartWaveButton != null)
            StartWaveButton.gameObject.SetActive(building);

        // Tower buttons only visible during building phase
        if (BuyBasicTowerButton != null)  BuyBasicTowerButton.gameObject.SetActive(building);
        if (BuySniperTowerButton != null) BuySniperTowerButton.gameObject.SetActive(building);
        if (BuySprayTowerButton != null)  BuySprayTowerButton.gameObject.SetActive(building);
        if (BuyRapidTowerButton != null)  BuyRapidTowerButton.gameObject.SetActive(building);
        if (BuyShieldButton != null)      BuyShieldButton.gameObject.SetActive(building);

        if (building)
            UpdateTowerButtonAffordability();
    }

    void UpdateWaveUI()
    {
        if (WaveManager.Instance == null) return;

        if (WaveText != null)
            WaveText.text = $"Wave: {WaveManager.Instance.GetCurrentWave()}";

        if (EnemiesText != null)
        {
            if (WaveManager.Instance.IsWaveInProgress())
            {
                int alive = WaveManager.Instance.GetEnemiesAlive();
                int toSpawn = WaveManager.Instance.GetEnemiesLeftToSpawn();
                EnemiesText.text = $"Enemies: {alive} alive, {toSpawn} remaining";
            }
            else
            {
                EnemiesText.text = "Ready to start wave";
            }
        }
    }

    // --- Tower info display (for hover/click on placed towers) ---

    public void ShowTowerInfo(string info)
    {
        if (TowerInfoText != null)
        {
            TowerInfoText.gameObject.SetActive(true);
            TowerInfoText.text = info;
        }
    }

    public void HideTowerInfo()
    {
        if (TowerInfoText != null)
            TowerInfoText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveInProgress())
            UpdateWaveUI();
    }

    void OnDestroy()
    {
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged -= UpdateHealthUI;
            HealthManager.Instance.ShieldChanged -= UpdateShieldUI;
        }

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.GoldChanged -= UpdateMoneyUI;

        if (GameManager.Instance != null)
            GameManager.Instance.GameStateChanged -= OnGameStateChanged;

        if (StartWaveButton != null)
            StartWaveButton.onClick.RemoveAllListeners();
        if (BuyShieldButton != null)
            BuyShieldButton.onClick.RemoveAllListeners();
        if (BuyBasicTowerButton != null)
            BuyBasicTowerButton.onClick.RemoveAllListeners();
        if (BuySniperTowerButton != null)
            BuySniperTowerButton.onClick.RemoveAllListeners();
        if (BuySprayTowerButton != null)
            BuySprayTowerButton.onClick.RemoveAllListeners();
        if (BuyRapidTowerButton != null)
            BuyRapidTowerButton.onClick.RemoveAllListeners();
    }
}
