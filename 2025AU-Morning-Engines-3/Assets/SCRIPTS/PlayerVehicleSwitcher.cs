using UnityEngine;

public class PlayerVehicleSwitcher : MonoBehaviour
{
    [Header("References")]
    public OnFootPlayerController onFootController;
    public CharacterController characterController;
    public Camera onFootCamera;
    public GameObject playerVisualRoot; // <--- NEW

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionRadius = 3f;
    public LayerMask truckLayer;

    [Header("Camera")]
    public Vector3 truckCameraOffset = new Vector3(0f, 2.0f, -5.0f);

    [Header("State")]
    public bool inVehicle = false;

    private TruckController currentTruck;

    private void Reset()
    {
        onFootController = GetComponent<OnFootPlayerController>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!inVehicle)
        {
            HandleEnterVehicle();
        }
        else
        {
            HandleExitVehicle();
        }
    }

    private void HandleEnterVehicle()
    {
        if (!Input.GetKeyDown(interactKey))
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius,
            truckLayer.value == 0 ? ~0 : truckLayer);

        TruckController truck = null;

        for (int i = 0; i < hits.Length; i++)
        {
            truck = hits[i].GetComponentInParent<TruckController>();
            if (truck != null) break;
        }

        // Fallback by tag
        if (truck == null)
        {
            hits = Physics.OverlapSphere(transform.position, interactionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Truck"))
                {
                    truck = hits[i].GetComponentInParent<TruckController>();
                    if (truck != null) break;
                }
            }
        }

        if (truck != null)
        {
            EnterVehicle(truck);
        }
    }

    private void HandleExitVehicle()
    {
        if (!Input.GetKeyDown(interactKey))
            return;

        if (currentTruck != null)
        {
            ExitVehicle();
        }
    }

    private void EnterVehicle(TruckController truck)
    {
        if (truck.seatPoint == null)
        {
            Debug.LogWarning("Truck has no seatPoint assigned!");
            return;
        }

        inVehicle = true;
        currentTruck = truck;

        // Disable on-foot control
        onFootController.isActive = false;
        if (characterController != null)
            characterController.enabled = false;

        // OPTIONAL: hide the player mesh while in vehicle
        if (playerVisualRoot != null)
        {
            playerVisualRoot.SetActive(false);
        }

        // Move player root to seat
        transform.SetParent(truck.seatPoint, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Move camera to seatPoint with offset
        if (onFootCamera != null)
        {
            onFootCamera.transform.SetParent(truck.seatPoint, worldPositionStays: false);
            onFootCamera.transform.localPosition = truckCameraOffset;
            onFootCamera.transform.localRotation = Quaternion.identity;
        }

        // Activate truck control
        truck.SetActive(true);
    }

    private void ExitVehicle()
    {
        inVehicle = false;

        // Unparent player from truck
        transform.SetParent(null, worldPositionStays: true);

        // Place player at exit point
        if (currentTruck.exitPoint != null)
        {
            transform.position = currentTruck.exitPoint.position;
            transform.rotation = currentTruck.exitPoint.rotation;
        }

        // Move camera back to player
        if (onFootCamera != null)
        {
            onFootCamera.transform.SetParent(onFootController.transform, worldPositionStays: false);
            onFootCamera.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            onFootCamera.transform.localRotation = Quaternion.identity;
        }

        // Turn player mesh back on
        if (playerVisualRoot != null)
        {
            playerVisualRoot.SetActive(true);
        }

        // Reactivate on-foot control
        if (characterController != null)
            characterController.enabled = true;
        onFootController.isActive = true;

        // Disable truck control
        currentTruck.SetActive(false);
        currentTruck = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
