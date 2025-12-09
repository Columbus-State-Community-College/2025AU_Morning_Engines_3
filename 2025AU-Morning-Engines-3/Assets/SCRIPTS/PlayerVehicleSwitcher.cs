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
    public Vector3 truckCameraOffset = new Vector3(0f, 2.0f, -5.0f); // kept for potential future use

    [Header("State")]
    public bool inVehicle = false;

    private TruckController currentTruck;
    private Renderer[] playerRenderers;

    // Camera original state
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    // Remember original player scale so we can always restore it
    private Vector3 originalPlayerLocalScale;

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

        // Cache the original camera parent + local transform so we can restore it on exit
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

        // Store original player scale (as placed in the scene)
        originalPlayerLocalScale = transform.localScale;
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
        int mask = (truckLayer.value == 0) ? Physics.AllLayers : truckLayer.value;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            mask
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

        // Make sure our scale starts at the original value
        transform.localScale = originalPlayerLocalScale;

        // Parent player to seatPoint BUT keep world transform the same
        transform.SetParent(truck.seatPoint, worldPositionStays: true);

        // Now explicitly snap position/rotation to the seatPoint
        transform.position = truck.seatPoint.position;
        transform.rotation = truck.seatPoint.rotation;

        // Camera handling:
        //  - Disable on-foot camera while driving
        //  - Truck controller will enable its own camera
        if (onFootCamera != null)
        {
            onFootCamera.gameObject.SetActive(false);
        }

        // Enable truck control (also enables truck camera inside TruckController)
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

        // Unparent player but keep world position/rotation/scale the same
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

        // Hard reset the player's scale to what it was in the scene
        transform.localScale = originalPlayerLocalScale;

        // Restore on-foot camera
        if (onFootCamera != null)
        {
            Transform camTransform = onFootCamera.transform;

            camTransform.SetParent(originalCameraParent, worldPositionStays: false);
            camTransform.localPosition = originalCameraLocalPos;
            camTransform.localRotation = originalCameraLocalRot;

            onFootCamera.gameObject.SetActive(true);
        }

        // Show player again
        SetPlayerVisible(true);

        // Reactivate on-foot movement
        if (characterController != null)
            characterController.enabled = true;

        if (onFootController != null)
            onFootController.isActive = true;

        // Disable truck control (also disables its camera)
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
