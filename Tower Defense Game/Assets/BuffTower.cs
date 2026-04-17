using System.Collections.Generic;
using UnityEngine;

public class BuffTower : MonoBehaviour
{
    public float range = 3f;
    public float damageMultiplier = 1.3f;
    public float cooldownMultiplier = 1.3f;
    public double buyValue;
    public double sellValue;

    private readonly HashSet<Tower> buffedTowers = new HashSet<Tower>();
    private readonly HashSet<MortarTower> buffedMortars = new HashSet<MortarTower>();

    public void Init(float dmgMult, float cdMult, float rng, double bVal, double sVal)
    {
        damageMultiplier = dmgMult;
        cooldownMultiplier = cdMult;
        range = rng;
        buyValue = bVal;
        sellValue = sVal;
    }

    public double GetSellValue() => sellValue;
    public double GetBuyValue()  => buyValue;

    void FixedUpdate()
    {
        // Clear buffs from last tick before re-applying
        foreach (Tower t in buffedTowers)
            if (t != null) t.ClearBuff();
        foreach (MortarTower m in buffedMortars)
            if (m != null) m.ClearBuff();
        buffedTowers.Clear();
        buffedMortars.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (Collider2D hit in hits)
        {
            Tower t = hit.GetComponent<Tower>();
            if (t != null)
            {
                t.SetBuff(damageMultiplier, cooldownMultiplier);
                buffedTowers.Add(t);
                continue;
            }

            MortarTower m = hit.GetComponent<MortarTower>();
            if (m != null)
            {
                m.SetBuff(damageMultiplier, cooldownMultiplier);
                buffedMortars.Add(m);
            }
        }
    }

    void OnDestroy()
    {
        foreach (Tower t in buffedTowers)
            if (t != null) t.ClearBuff();
        foreach (MortarTower m in buffedMortars)
            if (m != null) m.ClearBuff();
    }
}
