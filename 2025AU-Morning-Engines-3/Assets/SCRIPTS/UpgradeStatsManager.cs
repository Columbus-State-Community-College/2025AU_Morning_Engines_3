using UnityEngine;
using System;

public class UpgradeStatsManager : MonoBehaviour
{
    public static UpgradeStatsManager Instance { get; private set; }

    public static event Action OnUpgradesChanged;

    private const string PREF_KEY_DMG = "UPG_DMG";
    private const string PREF_KEY_PELLETS = "UPG_PELLETS";
    private const string PREF_KEY_HP = "UPG_HP";

    public int DamageUpgrades { get; private set; } = 0;   // +0.5 per point
    public int PelletsUpgrades { get; private set; } = 0;  // +1 per point
    public int HealthUpgrades { get; private set; } = 0;   // +10 per point

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("UpgradeStatsManager");
        go.AddComponent<UpgradeStatsManager>();
    }

    private void Load()
    {
        DamageUpgrades = PlayerPrefs.GetInt(PREF_KEY_DMG, 0);
        PelletsUpgrades = PlayerPrefs.GetInt(PREF_KEY_PELLETS, 0);
        HealthUpgrades = PlayerPrefs.GetInt(PREF_KEY_HP, 0);

        OnUpgradesChanged?.Invoke();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(PREF_KEY_DMG, DamageUpgrades);
        PlayerPrefs.SetInt(PREF_KEY_PELLETS, PelletsUpgrades);
        PlayerPrefs.SetInt(PREF_KEY_HP, HealthUpgrades);
        PlayerPrefs.Save();

        OnUpgradesChanged?.Invoke();
    }

    public static float GetDamageBonus()
    {
        EnsureExists();
        return Instance.DamageUpgrades * 0.5f;
    }

    public static int GetPelletsBonus()
    {
        EnsureExists();
        return Instance.PelletsUpgrades;
    }

    public static float GetHealthBonus()
    {
        EnsureExists();
        return Instance.HealthUpgrades * 10f;
    }

    public static bool TryBuyDamageUpgrade(int cost = 1)
    {
        EnsureExists();
        if (!UpgradePointsManager.SpendPoints(cost)) return false;

        Instance.DamageUpgrades += 1;
        Instance.Save();
        return true;
    }

    public static bool TryBuyPelletsUpgrade(int cost = 1)
    {
        EnsureExists();
        if (!UpgradePointsManager.SpendPoints(cost)) return false;

        Instance.PelletsUpgrades += 1;
        Instance.Save();
        return true;
    }

    public static bool TryBuyHealthUpgrade(int cost = 1)
    {
        EnsureExists();
        if (!UpgradePointsManager.SpendPoints(cost)) return false;

        Instance.HealthUpgrades += 1;
        Instance.Save();
        return true;
    }
}
