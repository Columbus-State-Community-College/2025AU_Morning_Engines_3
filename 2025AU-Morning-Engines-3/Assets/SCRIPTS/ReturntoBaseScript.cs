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
        // Grab the player's prompt text (same UI used by other interactions)
        OnFootPlayerController onFoot = FindObjectOfType<OnFootPlayerController>();
        if (onFoot != null)
        {
            prompt = onFoot.promptText;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Only react to the TRUCK being in the zone
        TruckController tc = other.GetComponentInParent<TruckController>();
        if (tc == null || tc != truck)
            return;

        // Require at least one zombie in the truck
        if (!HasCargo())
            return;

        if (prompt != null)
            prompt.text = "Press Q to return to base";

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Unlock cursor BEFORE loading the menu scene
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TruckController tc = other.GetComponentInParent<TruckController>();
        if (tc == null || tc != truck)
            return;

        if (prompt != null && prompt.text == "Press Q to return to base")
        {
            prompt.text = "";
        }
    }

    private bool HasCargo()
    {
        if (truck == null || truck.zombieCargoRoot == null)
            return false;

        // Any child under zombieCargoRoot = at least one deposited zombie
        return truck.zombieCargoRoot.childCount > 0;
    }
}
