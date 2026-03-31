using UnityEngine;

public class TowerMenuUI : MonoBehaviour
{
    public GameObject towerBuyMenu;

    void Update()
    {
        // Hide tower menu during non-building phases
        if (towerBuyMenu != null && GameManager.Instance != null)
        {
            if (!GameManager.Instance.IsBuilding() && towerBuyMenu.activeSelf)
                towerBuyMenu.SetActive(false);
        }
    }

    public void ToggleTowerMenu()
    {
        if (towerBuyMenu == null) return;
        if (!GameManager.Instance.IsBuilding()) return;

        towerBuyMenu.SetActive(!towerBuyMenu.activeSelf);
    }

    public void OpenTowerMenu()
    {
        if (towerBuyMenu == null) return;
        if (!GameManager.Instance.IsBuilding()) return;
        towerBuyMenu.SetActive(true);
    }

    public void CloseTowerMenu()
    {
        if (towerBuyMenu == null) return;
        towerBuyMenu.SetActive(false);
    }
}
