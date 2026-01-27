using UnityEngine;
using UnityEngine.UI;

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
    public int shieldBuyCost = 100;

    [Header("Shield Recharge")]
    public bool rechargeToFullBetweenWaves = true;
    public int rechargeAmountPerWave = 2;

    [Header("UI")]
    public Text livesText;
    public Text moneyText;
    public Text shieldText;

    private bool gameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    // Called when an enemy reaches the EndZone
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
            {
                GameOver();
            }
        }

        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        UpdateUI();
        return true;
    }

    public void BuyShieldCapacity()
    {
        if (!SpendMoney(shieldBuyCost)) return;

        shieldMax += 1;
        shieldCurrent = Mathf.Min(shieldCurrent + 1, shieldMax);
        shieldBuyCost = Mathf.RoundToInt(shieldBuyCost + 15f);

        UpdateUI();
    }

    public void RechargeShieldBetweenWaves()
    {
        if (shieldMax <= 0) return;

        if (rechargeToFullBetweenWaves)
        {
            shieldCurrent = shieldMax;
        }
        else
        {
            shieldCurrent = Mathf.Min(shieldCurrent + rechargeAmountPerWave, shieldMax);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (livesText != null) livesText.text = "Lives: " + lives;
        if (moneyText != null) moneyText.text = "Money: " + money;
        if (shieldText != null) shieldText.text = "Shield: " + shieldCurrent + "/" + shieldMax + "  Cost: " + shieldBuyCost;
    }

    void GameOver()
    {
        gameOver = true;
        Debug.Log("GAME OVER");
        Time.timeScale = 0f;
    }
}
