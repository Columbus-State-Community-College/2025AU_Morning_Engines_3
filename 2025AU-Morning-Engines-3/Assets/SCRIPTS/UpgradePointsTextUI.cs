using UnityEngine;
using TMPro;

public class UpgradePointsTextUI : MonoBehaviour
{
    public TMP_Text pointsText;

    private void OnEnable()
    {
        UpgradePointsManager.OnPointsChanged += HandlePointsChanged;
        HandlePointsChanged(UpgradePointsManager.GetPoints());
    }

    private void OnDisable()
    {
        UpgradePointsManager.OnPointsChanged -= HandlePointsChanged;
    }

    private void HandlePointsChanged(int points)
    {
        if (pointsText != null)
        {
            pointsText.text = "Upgrade Points: " + points;
        }
    }
}
