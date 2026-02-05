using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Lives")]
    public int lives = 20;

    [Header("Money")]
    public int money = 200;

    [Header("Shield")]
    public int shieldMax = 0;
    public int shieldCurrent = 0;

    [Header("Shield Cost")]
    public int shieldBuyCost = 100;
    public float shieldCostMultiplier = 1.25f;   // change this to control scaling (ex: 1.15, 1.3, etc.)
    public int shieldCostFlatIncrease = 0;        // optional: add a flat increase too (ex: 10). Leave 0 if not used.

    [Header("UI (TextMeshPro)")]
    public TMP_Text livesText;
    public TMP_Text moneyText;
    public TMP_Text shieldText;

    private bool gameOver = false;

    private void Awake()
    {
        // Safe singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    // Enemy reached the end
    public void TakeBaseHit(int amount = 1)
    {
        if (gameOver) return;

        if (shieldCurrent > 0)
        {
            shieldCurrent -= amount;
            if (shieldCurrent < 0) shieldCurrent = 0;
        }
        else
        {
            lives -= amount;
            if (lives < 0) lives = 0;

            if (lives <= 0)
                GameOver();
        }

        RefreshUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        RefreshUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        RefreshUI();
        return true;
    }

    // Button calls this
    public void BuyShieldCapacity()
    {
        Debug.Log("BuyShieldCapacity() clicked");

        if (!SpendMoney(shieldBuyCost))
        {
            Debug.Log("Not enough money to buy shield!");
            return;
        }

        shieldMax += 1;

        // Give 1 charge immediately when you buy capacity
        shieldCurrent = Mathf.Min(shieldCurrent + 1, shieldMax);

        // Increase cost for next time
        shieldBuyCost = Mathf.RoundToInt(shieldBuyCost * shieldCostMultiplier) + shieldCostFlatIncrease;

        RefreshUI();
    }

    // Call this at end of each wave
    public void RechargeShieldBetweenWaves(bool fullRecharge = true, int rechargeAmount = 2)
    {
        if (shieldMax <= 0) return;

        if (fullRecharge)
            shieldCurrent = shieldMax;
        else
            shieldCurrent = Mathf.Min(shieldCurrent + rechargeAmount, shieldMax);

        RefreshUI();
    }

    private void RefreshUI()
    {
        // These guards prevent crashes if you forgot to drag a reference
        if (livesText != null) livesText.text = "Lives: " + lives;
        if (moneyText != null) moneyText.text = "Money: " + money;
        if (shieldText != null) shieldText.text = "Shield: " + shieldCurrent + "/" + shieldMax + "   Cost: " + shieldBuyCost;
    }

    private void GameOver()
    {
        gameOver = true;
        Debug.Log("GAME OVER");
        Time.timeScale = 0f;
    }
}
