using System.Collections.Generic;
using UnityEngine;

// Holds the player's chosen towers between scenes. Persists via PlayerPrefs.
public static class TowerLoadout
{
    private const string PREF_KEY = "tower_loadout_v1";

    public static int maxSlots = 5;

    private static List<TowerPlacer.TowerType> _selected;

    public static List<TowerPlacer.TowerType> Selected
    {
        get
        {
            if (_selected == null) Load();
            return _selected;
        }
    }

    public static bool IsSelected(TowerPlacer.TowerType type)
    {
        return Selected.Contains(type);
    }

    public static bool IsFull => Selected.Count >= maxSlots;

    public static bool Add(TowerPlacer.TowerType type)
    {
        if (IsFull || Selected.Contains(type)) return false;
        Selected.Add(type);
        return true;
    }

    public static void Remove(TowerPlacer.TowerType type)
    {
        Selected.Remove(type);
    }

    public static void Clear()
    {
        Selected.Clear();
    }

    public static void Save()
    {
        string s = string.Join(",", Selected.ConvertAll(t => t.ToString()).ToArray());
        PlayerPrefs.SetString(PREF_KEY, s);
        PlayerPrefs.Save();
    }

    private static void Load()
    {
        _selected = new List<TowerPlacer.TowerType>();
        // Default loadout if nothing saved yet
        string s = PlayerPrefs.GetString(PREF_KEY, "Basic,Sniper,Spray,Rapid,Mortar");
        if (string.IsNullOrEmpty(s)) return;

        foreach (string part in s.Split(','))
        {
            if (System.Enum.TryParse<TowerPlacer.TowerType>(part.Trim(), out var t))
                _selected.Add(t);
        }
    }
}
