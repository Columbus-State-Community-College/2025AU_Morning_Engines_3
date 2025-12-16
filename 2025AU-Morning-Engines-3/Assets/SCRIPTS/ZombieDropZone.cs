using UnityEngine;
using TMPro;

public class ZombieDropZone : MonoBehaviour
{
    [Tooltip("Truck that will receive deposited zombies.")]
    public TruckController truck;

    private void OnTriggerStay(Collider other)
    {
        ZombieCarrier carrier = other.GetComponent<ZombieCarrier>();
        if (carrier == null || !carrier.isCarryingZombie)
            return;

        OnFootPlayerController onFoot = other.GetComponent<OnFootPlayerController>();
        if (onFoot == null || !onFoot.isActive)
            return;

        TMP_Text prompt = onFoot.promptText;
        if (prompt != null)
        {
            prompt.text = "Press F to drop zombie in truck";
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ZombieHealth zombieToDeposit = carrier.carriedZombie;

            if (truck != null && truck.TryDepositZombie(zombieToDeposit))
            {
                // Award upgrade point ONLY if this zombie is the glowing variant
                if (zombieToDeposit != null && zombieToDeposit.TryClaimUpgradePoint())
                {
                    UpgradePointsManager.AddPoints(1);
                }

                carrier.ClearCarriedZombie();

                if (prompt != null && prompt.text == "Press F to drop zombie in truck")
                {
                    prompt.text = "";
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ZombieCarrier carrier = other.GetComponent<ZombieCarrier>();
        if (carrier == null)
            return;

        OnFootPlayerController onFoot = other.GetComponent<OnFootPlayerController>();
        if (onFoot == null || onFoot.promptText == null)
            return;

        if (onFoot.promptText.text == "Press F to drop zombie in truck")
        {
            onFoot.promptText.text = "";
        }
    }
}
