using UnityEngine;

// Simple 3rd-person follow camera for the truck.
// Place this on the Main Camera object.

public class FollowCamera : MonoBehaviour
{
    [Header("FOLLOW TARGET")]
    public Transform target;             // Assign your PlayerVehicle here in Inspector

    [Header("OFFSET SETTINGS")]
    [Tooltip("How far behind the target the camera sits")]
    public Vector3 offset = new Vector3(0f, 3f, -8f);

    [Header("SMOOTHING")]
    [Tooltip("How quickly the camera moves toward target position")]
    [Range(0.01f, 1f)] public float smoothSpeed = 0.15f;
    [Tooltip("How quickly the camera rotates to face the target")]
    [Range(0.01f, 1f)] public float rotationSmooth = 0.1f;

    [Header("LOOK OPTIONS")]
    [Tooltip("If true, camera will always look at target center")]
    public bool lookAtTarget = true;

    Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!target) return;

        // Desired position = target + rotated offset
        Vector3 desiredPosition = target.TransformPoint(offset);
        // Smooth move
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);

        if (lookAtTarget)
        {
            // Smooth rotation toward target
            Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmooth);
        }
    }
}
