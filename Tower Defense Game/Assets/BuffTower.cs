using System.Collections.Generic;
using UnityEngine;

public class BuffTower : MonoBehaviour
{
    public float range = 3f;
    public float damageMultiplier = 1.3f;
    public float cooldownMultiplier = 1.3f;
    public double buyValue;
    public double sellValue;

    private HashSet<Tower> buffedTowers = new HashSet<Tower>();
    private HashSet<MortarTower> buffedMortars = new HashSet<MortarTower>();

    // Final-form sprite swap on max upgrade (e.g. archdruid staff)
    private Sprite finalSprite;
    private bool   isCharging = false;
    private Vector2 staffPulseOffset = new Vector2(0f, 0.5f);

    public void Init(float dmgMult, float cdMult, float rng, double bVal, double sVal)
    {
        damageMultiplier = dmgMult;
        cooldownMultiplier = cdMult;
        range = rng;
        buyValue = bVal;
        sellValue = sVal;
    }

    // Optional: when set AND the tower hits max upgrade level, plays a nature-themed
    // charge-up animation, then swaps the sprite to this one.
    public void SetFinalSprite(Sprite sprite)
    {
        finalSprite = sprite;
    }

    // World-space offset (x,y) from the tower center to the staff tip — used as
    // the origin for the periodic pulses in ArchdruidAura.
    public void SetStaffPulseOffset(Vector2 offset)
    {
        staffPulseOffset = offset;
    }

    public double GetSellValue()  => sellValue;
    public double GetBuyValue()   => buyValue;
    public int    GetBuffedCount() => buffedTowers.Count + buffedMortars.Count;

    void FixedUpdate()
    {
        // Build new in-range sets
        var towersInRange   = new HashSet<Tower>();
        var mortarsInRange  = new HashSet<MortarTower>();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            Tower t = hit.GetComponent<Tower>();
            if (t != null) { towersInRange.Add(t); continue; }
            MortarTower m = hit.GetComponent<MortarTower>();
            if (m != null) mortarsInRange.Add(m);
        }

        // Remove buff from towers that left range
        foreach (Tower t in buffedTowers)
            if (t != null && !towersInRange.Contains(t)) t.RemoveBuff(this);
        foreach (MortarTower m in buffedMortars)
            if (m != null && !mortarsInRange.Contains(m)) m.RemoveBuff(this);

        // Apply/refresh buff for every tower currently in range
        foreach (Tower t in towersInRange)
            if (t != null) t.ApplyBuff(this, damageMultiplier, cooldownMultiplier);
        foreach (MortarTower m in mortarsInRange)
            if (m != null) m.ApplyBuff(this, damageMultiplier, cooldownMultiplier);

        buffedTowers  = towersInRange;
        buffedMortars = mortarsInRange;
    }

    void OnDestroy()
    {
        foreach (Tower t in buffedTowers)
            if (t != null) t.RemoveBuff(this);
        foreach (MortarTower m in buffedMortars)
            if (m != null) m.RemoveBuff(this);
    }

    // ── Upgrade system ─────────────────────────────────────────────────────

    private int upgradeLevel = 0;
    private BuffUpgradeData[] upgrades;

    public void SetUpgrades(BuffUpgradeData[] data) { upgrades = data; }

    public int  GetUpgradeLevel() => upgradeLevel;
    public bool IsMaxLevel()      => upgrades == null || upgradeLevel >= upgrades.Length;

    public int GetUpgradeCost()
    {
        if (IsMaxLevel()) return 0;
        return upgrades[upgradeLevel].cost;
    }

    public string GetUpgradeDescription()
    {
        if (IsMaxLevel()) return "MAX LEVEL";
        return upgrades[upgradeLevel].description;
    }

    public void Upgrade()
    {
        if (IsMaxLevel()) return;
        BuffUpgradeData u = upgrades[upgradeLevel];
        upgradeLevel++;

        damageMultiplier   += u.dmgMultBonus;
        cooldownMultiplier += u.cdMultBonus;
        sellValue          += u.cost * 0.5;

        // Immediately re-apply the stronger buff to already-buffed towers
        foreach (Tower t in buffedTowers)
            if (t != null) t.ApplyBuff(this, damageMultiplier, cooldownMultiplier);
        foreach (MortarTower m in buffedMortars)
            if (m != null) m.ApplyBuff(this, damageMultiplier, cooldownMultiplier);

        UpgradeEffect.Play(transform);

        // Final upgrade triggers the nature charge-up + sprite swap
        if (IsMaxLevel() && finalSprite != null && !isCharging)
            StartFinalChargeUp();
    }

    void StartFinalChargeUp()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        isCharging = true;
        NatureChargeUpEffect.Play(transform, sr, 2.5f, () =>
        {
            sr.sprite  = finalSprite;
            isCharging = false;

            // Attach the persistent ARCHDRUID aura
            ArchdruidAura aura = gameObject.AddComponent<ArchdruidAura>();
            aura.staffPulseOffset = staffPulseOffset;
        });
    }
}

[System.Serializable]
public class BuffUpgradeData
{
    public int    cost;
    public float  dmgMultBonus; // added to damageMultiplier  (e.g. 0.05 = +5%)
    public float  cdMultBonus;  // added to cooldownMultiplier (e.g. 0.05 = +5% speed)
    public string description;
}
