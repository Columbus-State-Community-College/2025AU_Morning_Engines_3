using UnityEngine;

public class AimCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player controller script that knows when we are aiming.")]
    public OnFootPlayerController playerController;

    [Tooltip("Transform for normal (no-aim) camera pose.")]
    public Transform normalPose;

    [Tooltip("Transform for aim camera pose.")]
    public Transform aimPose;

    [Header("Transition")]
    [Tooltip("How fast the camera blends between poses.")]
    public float transitionSpeed = 8f;

    private void LateUpdate()
    {
        if (playerController == null || normalPose == null || aimPose == null)
            return;

        // Are we aiming according to the player script?
        bool isAiming = playerController.IsAiming;

        // Pick which pose we should move toward
        Transform target = isAiming ? aimPose : normalPose;

        // Smoothly move + rotate camera toward that pose
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            Time.deltaTime * transitionSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target.rotation,
            Time.deltaTime * transitionSpeed
        );
    }
}
