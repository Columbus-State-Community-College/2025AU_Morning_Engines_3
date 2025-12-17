using UnityEngine;
using System;

public class UpgradePointsManager : MonoBehaviour
{
    public static UpgradePointsManager Instance { get; private set; }

    public int Points { get; private set; } = 0;

    public static event Action<int> OnPointsChanged;

    private const string PREF_KEY_POINTS = "UPGRADE_POINTS";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved points (so builds + restarts keep state)
        Points = PlayerPrefs.GetInt(PREF_KEY_POINTS, 0);
        OnPointsChanged?.Invoke(Points);
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;

        EnsureExists();

        Instance.Points += amount;
        Instance.Save();
        OnPointsChanged?.Invoke(Instance.Points);
    }

    public static bool SpendPoints(int amount)
    {
        if (amount <= 0) return true;

        EnsureExists();

        if (Instance.Points < amount)
            return false;

        Instance.Points -= amount;
        Instance.Save();
        OnPointsChanged?.Invoke(Instance.Points);
        return true;
    }

    public static int GetPoints()
    {
        EnsureExists();
        return Instance.Points;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(PREF_KEY_POINTS, Points);
        PlayerPrefs.Save();
    }

    private static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("UpgradePointsManager");
        go.AddComponent<UpgradePointsManager>();
    }
}
