using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ReturnToBaseZone : MonoBehaviour
{
    [Tooltip("Assign the truck in the inspector.")]
    public TruckController truck;

    [Tooltip("Exact name of your main menu scene in Build Settings.")]
    public string mainMenuSceneName = "mainMenuScene";

    private TMP_Text prompt;

    private void Start()
    {
        // Try to find player prompt text (same system as drop-off zones)
        OnFootPlayerController onFoot = FindObjectOfType<OnFootPlayerController>();
        if (onFoot != null)
        {
            prompt = onFoot.promptText;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Only react to the TRUCK entering the zone
        TruckController tc = other.GetComponentInParent<TruckController>();
        if (tc == null || tc != truck)
            return;

        // Must have at least one zombie loaded
        if (!HasCargo())
            return;

        if (prompt != null)
            prompt.text = "Press Q to return to base";

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Load menu
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TruckController tc = other.GetComponentInParent<TruckController>();
        if (tc == null || tc != truck)
            return;

        if (prompt != null && prompt.text == "Press Q to return to base")
            prompt.text = "";
    }

    private bool HasCargo()
    {
        // Access the private list from TruckController
        // We can't access loadedZombies directly since it's private,
        // but your deposit logic always parents zombies into the cargo root.
        if (truck == null || truck.zombieCargoRoot == null)
            return false;

        // Any children under zombieCargoRoot = at least one deposited zombie
        return truck.zombieCargoRoot.childCount > 0;
    }
}
