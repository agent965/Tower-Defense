[System.Serializable]
public struct TowerUpgradeData
{
    public int    cost;
    public string description;

    public int   dmgAdd;        // flat damage added
    public float rangeMult;     // e.g. 1.1 = +10% range    (1 = no change)
    public float cooldownMult;  // e.g. 0.8 = -20% cooldown (1 = no change)
    public float splashAdd;     // mortar only — flat splash radius increase
}
