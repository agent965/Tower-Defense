using UnityEngine;

public class TowerMenuUI : MonoBehaviour
{
    public GameObject towerBuyMenu;

    public void ToggleTowerMenu()
    {
        if (towerBuyMenu == null) return;

        towerBuyMenu.SetActive(!towerBuyMenu.activeSelf);
    }

    public void OpenTowerMenu()
    {
        if (towerBuyMenu == null) return;
        towerBuyMenu.SetActive(true);
    }

    public void CloseTowerMenu()
    {
        if (towerBuyMenu == null) return;
        towerBuyMenu.SetActive(false);
    }
}
