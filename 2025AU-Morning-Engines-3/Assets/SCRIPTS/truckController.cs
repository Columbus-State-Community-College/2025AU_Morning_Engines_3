using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TruckController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 20f;          // how fast we accelerate forward/back
    public float maxForwardSpeed = 20f;       // max speed going forward (m/s)
    public float maxReverseSpeed = 10f;       // max speed going backward (m/s)
    public float turnSpeed = 75f;             // degrees per second at full steer
    public float naturalDecel = 5f;           // how fast truck slows when no throttle
    public float lateralFriction = 12f;       // how strongly we kill sideways sliding

    [Header("Handbrake")]
    public KeyCode handbrakeKey = KeyCode.Space;
    public float handbrakeDecel = 40f;             // how fast forward speed drops with handbrake
    public float handbrakeLateralFriction = 30f;   // how fast sideways speed drops with handbrake

    [Header("Setup")]
    public Transform seatPoint;   // Where player will sit / be parented
    public Transform exitPoint;   // Where player appears when exiting
    public Camera truckCamera;    // Camera used when driving

    [Header("Zombie Cargo")]
    [Tooltip("This should be your 'zombiePoint' child in the truck bed. All zombies snap here.")]
    public Transform zombieCargoRoot;

    [Header("State")]
    public bool isActive = false; // Controlled only when player is inside

    private Rigidbody rb;

    // Track which zombies we've loaded (for scoring / later logic if needed)
    private readonly List<ZombieHealth> loadedZombies = new List<ZombieHealth>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (truckCamera != null)
        {
            truckCamera.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (!isActive)
            return;

        float throttle = Input.GetAxis("Vertical");   // W/S or Up/Down keys
        float steer = Input.GetAxis("Horizontal");    // A/D or Left/Right keys
        bool handbrake = Input.GetKey(handbrakeKey);

        // Get current velocity in local space
        Vector3 worldVelocity = rb.linearVelocity;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
        float forwardSpeed = localVelocity.z;

        if (handbrake)
        {
            // Handbrake: aggressively kill forward and sideways velocity
            localVelocity.z = Mathf.MoveTowards(
                localVelocity.z,
                0f,
                handbrakeDecel * Time.fixedDeltaTime
            );

            localVelocity.x = Mathf.MoveTowards(
                localVelocity.x,
                0f,
                handbrakeLateralFriction * Time.fixedDeltaTime
            );
        }
        else
        {
            // Forward/back acceleration
            if (Mathf.Abs(throttle) > 0.01f)
            {
                float targetMax = (throttle > 0f) ? maxForwardSpeed : maxReverseSpeed;
                float targetSpeed = targetMax * Mathf.Sign(throttle);

                float speedDiff = targetSpeed - localVelocity.z;
                float accelStep = acceleration * Time.fixedDeltaTime;
                float change = Mathf.Clamp(speedDiff, -accelStep, accelStep);

                localVelocity.z += change;
            }
            else
            {
                // No throttle: gently slow down forward/back motion
                localVelocity.z = Mathf.MoveTowards(
                    localVelocity.z,
                    0f,
                    naturalDecel * Time.fixedDeltaTime
                );
            }

            // Lateral friction: always pull sideways speed toward zero
            localVelocity.x = Mathf.MoveTowards(
                localVelocity.x,
                0f,
                lateralFriction * Time.fixedDeltaTime
            );
        }

        // Clamp forward/back speed separately for forward vs reverse
        if (localVelocity.z > 0f && localVelocity.z > maxForwardSpeed)
        {
            localVelocity.z = maxForwardSpeed;
        }
        else if (localVelocity.z < 0f && Mathf.Abs(localVelocity.z) > maxReverseSpeed)
        {
            localVelocity.z = -maxReverseSpeed;
        }

        // Apply velocity back to world space
        rb.linearVelocity = transform.TransformDirection(localVelocity);

        // Steering: only when moving a bit
        forwardSpeed = localVelocity.z;
        if (Mathf.Abs(forwardSpeed) > 0.5f && Mathf.Abs(steer) > 0.01f)
        {
            float speedFactor = Mathf.InverseLerp(0f, maxForwardSpeed, Mathf.Abs(forwardSpeed));
            float turn = steer * turnSpeed * speedFactor * Time.fixedDeltaTime * Mathf.Sign(forwardSpeed);

            Quaternion turnRot = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRot);
        }
    }

    // ===========================================================
    // ================== ZOMBIE CARGO API =======================
    // ===========================================================

    /// <summary>
    /// Drop a zombie at the exact zombieCargoRoot position/origin every time.
    /// </summary>
    public bool TryDepositZombie(ZombieHealth zombie)
    {
        if (zombie == null)
            return false;

        if (zombieCargoRoot == null)
        {
            Debug.LogWarning("TruckController: zombieCargoRoot is not assigned. Drag your 'zombiePoint' here.");
            return false;
        }

        // We keep the localOffset parameter in the signature for compatibility,
        // but always pass Vector3.zero so all zombies snap to the same spot.
        Vector3 localOffset = Vector3.zero;

        zombie.SetDeposited(zombieCargoRoot, localOffset);
        loadedZombies.Add(zombie);

        return true;
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (truckCamera != null)
        {
            truckCamera.gameObject.SetActive(active);
        }
    }
}
