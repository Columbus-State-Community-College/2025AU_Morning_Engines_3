using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TruckController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 20f;
    public float maxForwardSpeed = 20f;
    public float maxReverseSpeed = 10f;
    public float turnSpeed = 75f;
    public float naturalDecel = 5f;
    public float lateralFriction = 12f;

    [Header("Handbrake")]
    public KeyCode handbrakeKey = KeyCode.Space;
    public float handbrakeDecel = 40f;
    public float handbrakeLateralFriction = 30f;

    [Header("Collision Stabilization")]
    public float collisionAngularDamping = 4f;
    public float blockedSteerThreshold = 0.3f;
    public float collisionLateralKill = 20f;

    [Header("Setup")]
    public Transform seatPoint;
    public Transform exitPoint;
    public Camera truckCamera;

    [Header("Zombie Cargo")]
    public Transform zombieCargoRoot;

    [Header("State")]
    public bool isActive = false;

    private Rigidbody rb;
    private bool isBlockedByWall = false;

    private readonly List<ZombieHealth> loadedZombies = new List<ZombieHealth>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.angularDamping = 0.5f;
    }

    private void Start()
    {
        if (truckCamera != null)
            truckCamera.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!isActive)
            return;

        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        bool handbrake = Input.GetKey(handbrakeKey);

        Vector3 worldVelocity = rb.linearVelocity;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float forwardSpeed = localVelocity.z;

        if (handbrake)
        {
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
                localVelocity.z = Mathf.MoveTowards(
                    localVelocity.z,
                    0f,
                    naturalDecel * Time.fixedDeltaTime
                );
            }

            localVelocity.x = Mathf.MoveTowards(
                localVelocity.x,
                0f,
                lateralFriction * Time.fixedDeltaTime
            );
        }

        if (localVelocity.z > maxForwardSpeed)
            localVelocity.z = maxForwardSpeed;
        else if (localVelocity.z < -maxReverseSpeed)
            localVelocity.z = -maxReverseSpeed;

        rb.linearVelocity = transform.TransformDirection(localVelocity);

        // ----------------- STEERING (FIXED) -----------------
        float absForwardSpeed = Mathf.Abs(localVelocity.z);

        bool canSteer =
            absForwardSpeed > blockedSteerThreshold &&
            !isBlockedByWall;

        if (canSteer && Mathf.Abs(steer) > 0.01f)
        {
            float speedFactor = Mathf.InverseLerp(0f, maxForwardSpeed, absForwardSpeed);
            float turn = steer * turnSpeed * speedFactor * Time.fixedDeltaTime * Mathf.Sign(localVelocity.z);

            Quaternion turnRot = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRot);
        }

        isBlockedByWall = false; // reset each physics frame
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Ignore ground collisions (normal mostly up)
            if (contact.normal.y < 0.5f)
            {
                isBlockedByWall = true;

                // Kill sideways slide
                Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
                localVel.x = Mathf.MoveTowards(
                    localVel.x,
                    0f,
                    collisionLateralKill * Time.fixedDeltaTime
                );
                rb.linearVelocity = transform.TransformDirection(localVel);

                // Kill spin
                rb.angularVelocity = Vector3.Lerp(
                    rb.angularVelocity,
                    Vector3.zero,
                    collisionAngularDamping * Time.fixedDeltaTime
                );

                break;
            }
        }
    }

    // ===================================================
    // ================== ZOMBIE CARGO ===================
    // ===================================================

    public bool TryDepositZombie(ZombieHealth zombie)
    {
        if (zombie == null)
            return false;

        if (zombieCargoRoot == null)
        {
            Debug.LogWarning("TruckController: zombieCargoRoot is not assigned.");
            return false;
        }

        zombie.SetDeposited(zombieCargoRoot, Vector3.zero);
        loadedZombies.Add(zombie);
        return true;
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (truckCamera != null)
            truckCamera.gameObject.SetActive(active);
    }
}
