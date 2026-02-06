using UnityEngine;
using TMPro; // Use this if we use TextMeshPro
// using UnityEngine.UI; // Use this if we use the old Text (pls dont)

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Text Elements")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI MoneyText;
    public TextMeshProUGUI ShieldText;
    
    [Header("UI Buttons")]
    public GameObject BuyShield; // Button or panel for buying shield
    
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
        HealthManager.Instance.LivesChanged += UpdateHealthUI;
        
        // Initialize health display
        UpdateHealthUI(HealthManager.Instance.GetCurrentLives());
    }
    
    void UpdateHealthUI(int newHealth)
    {
        if (HealthText != null)
        {
            HealthText.text = $"Health: {newHealth}";
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (HealthManager.Instance != null)
        {
            HealthManager.Instance.LivesChanged -= UpdateHealthUI;
        }
    }
}