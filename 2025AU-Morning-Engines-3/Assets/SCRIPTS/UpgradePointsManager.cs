using UnityEngine;
using System;

public class UpgradePointsManager : MonoBehaviour
{
    public static UpgradePointsManager Instance { get; private set; }

    public int Points { get; private set; } = 0;

    public static event Action<int> OnPointsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;

        EnsureExists();

        Instance.Points += amount;
        OnPointsChanged?.Invoke(Instance.Points);
    }

    public static int GetPoints()
    {
        EnsureExists();
        return Instance.Points;
    }

    private static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("UpgradePointsManager");
        go.AddComponent<UpgradePointsManager>();
    }
}
