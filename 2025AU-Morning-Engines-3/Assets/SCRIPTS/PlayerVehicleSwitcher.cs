using UnityEngine;

public class PlayerVehicleSwitcher : MonoBehaviour
{
    [Header("References")]
    public OnFootPlayerController onFootController;
    public CharacterController characterController;
    public Camera onFootCamera;

    [Tooltip("Root transform for the player's visual mesh (do NOT assign the whole Player root).")]
    public Transform playerVisualRoot;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionRadius = 3f;
    public LayerMask truckLayer; // can be 0 (Everything) and rely on tag

    [Header("Truck Camera")]
    public Vector3 truckCameraOffset = new Vector3(0f, 2.0f, -5.0f); // behind + above seat

    [Header("State")]
    public bool inVehicle = false;

    private TruckController currentTruck;
    private Renderer[] playerRenderers;

    // Camera original state
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    private void Awake()
    {
        if (onFootController == null)
            onFootController = GetComponent<OnFootPlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        // Cache renderers so we can hide/show the player model
        if (playerVisualRoot != null)
        {
            playerRenderers = playerVisualRoot.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            Debug.LogWarning("PlayerVehicleSwitcher: playerVisualRoot is not assigned. Player will stay visible in vehicle.");
        }

        // Cache the original camera parent + local transform so we can restore it exactly
        if (onFootCamera != null)
        {
            Transform camTransform = onFootCamera.transform;
            originalCameraParent = camTransform.parent;
            originalCameraLocalPos = camTransform.localPosition;
            originalCameraLocalRot = camTransform.localRotation;
        }
        else
        {
            Debug.LogWarning("PlayerVehicleSwitcher: onFootCamera is not assigned.");
        }
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

        // Look for a truck near the player
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            truckLayer.value == 0 ? ~0 : truckLayer
        );

        TruckController truck = null;

        for (int i = 0; i < hits.Length; i++)
        {
            truck = hits[i].GetComponentInParent<TruckController>();
            if (truck != null)
                break;
        }

        // Fallback by tag if no truck via layer
        if (truck == null)
        {
            hits = Physics.OverlapSphere(transform.position, interactionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Truck"))
                {
                    truck = hits[i].GetComponentInParent<TruckController>();
                    if (truck != null)
                        break;
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
        else
        {
            Debug.LogWarning("InVehicle is true but currentTruck is null.");
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

        // Disable on-foot movement and look
        if (onFootController != null)
            onFootController.isActive = false;

        if (characterController != null)
            characterController.enabled = false;

        // Hide the player mesh while in the vehicle (but NOT the whole GameObject)
        SetPlayerVisible(false);

        // Parent player root to seatPoint and snap to it
        transform.SetParent(truck.seatPoint, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Move camera to seatPoint with offset
        if (onFootCamera != null)
        {
            Transform camTransform = onFootCamera.transform;
            camTransform.SetParent(truck.seatPoint, worldPositionStays: false);
            camTransform.localPosition = truckCameraOffset;
            camTransform.localRotation = Quaternion.identity;
        }

        // Enable truck control
        truck.SetActive(true);

        Debug.Log("Entered vehicle.");
    }

    private void ExitVehicle()
    {
        if (currentTruck == null)
        {
            Debug.LogWarning("Tried to exit vehicle but currentTruck is null.");
            return;
        }

        Debug.Log("Exiting vehicle...");

        inVehicle = false;

        // Unparent player from truck
        transform.SetParent(null, worldPositionStays: true);

        // Decide where to place the player
        Vector3 targetPos;
        Quaternion targetRot;

        if (currentTruck.exitPoint != null)
        {
            targetPos = currentTruck.exitPoint.position;
            targetRot = currentTruck.exitPoint.rotation;
            Debug.Log("Using exitPoint on truck.");
        }
        else
        {
            // Fallback: left side of truck + a bit up
            targetPos = currentTruck.transform.position
                        + currentTruck.transform.right * -2f
                        + Vector3.up * 1f;

            targetRot = Quaternion.LookRotation(currentTruck.transform.forward);
            Debug.LogWarning("No exitPoint on truck, using fallback exit position.");
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        // Restore camera to its original parent and local transform
        if (onFootCamera != null)
        {
            Transform camTransform = onFootCamera.transform;

            // Restore parent
            camTransform.SetParent(originalCameraParent, worldPositionStays: false);

            // Restore original position/rotation relative to that parent
            camTransform.localPosition = originalCameraLocalPos;
            camTransform.localRotation = originalCameraLocalRot;
        }

        // Show player again
        SetPlayerVisible(true);

        // Reactivate on-foot movement
        if (characterController != null)
            characterController.enabled = true;

        if (onFootController != null)
            onFootController.isActive = true;

        // Disable truck control
        currentTruck.SetActive(false);
        currentTruck = null;

        Debug.Log("Finished exiting vehicle.");
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
            return;

        foreach (var rend in playerRenderers)
        {
            if (rend != null)
                rend.enabled = visible;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
