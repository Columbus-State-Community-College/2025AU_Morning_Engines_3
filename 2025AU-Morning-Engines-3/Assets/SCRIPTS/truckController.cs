using UnityEngine;

// Simple arcade-style car/truck controller.
// Works with Unity 6.0+ and both the old and new Input Systems.
// Hookup & tuning steps are below the script in my message.

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class CarController : MonoBehaviour
{
    [Header("SPEED & ACCELERATION")]
    [Tooltip("Forward top speed in meters/second (approx. 27 m/s ≈ 60 mph)")]
    public float maxForwardSpeed = 28f;            // ~62 mph
    [Tooltip("Reverse top speed in meters/second")]
    public float maxReverseSpeed = 10f;
    [Tooltip("How quickly the car accelerates")]
    public float acceleration = 16f;               // Accel force
    [Tooltip("Extra force applied when braking (S/Space)")]
    public float brakeStrength = 30f;

    [Header("STEERING")]
    [Tooltip("Maximum steering rate in degrees/second")]
    public float steerRate = 120f;
    [Tooltip("Reduces steering at high speed (0 = no reduction)")]
    [Range(0f, 1f)] public float highSpeedSteerReduction = 0.7f;

    [Header("GRIP & STABILITY")]
    [Tooltip("How strongly sideways sliding is damped (bigger = more grip)")]
    public float lateralFriction = 6.5f;
    [Tooltip("Downforce scales with speed to keep the truck planted")]
    public float downforce = 35f;
    [Tooltip("Extra gravity when airborne")]
    public float extraGravity = 20f;
    [Tooltip("Raycast length to consider the car grounded")]
    public float groundRayLength = 0.8f;
    [Tooltip("Offset center of mass downward to reduce rollovers")]
    public float centerOfMassYOffset = -0.4f;

    [Header("Quality of Life")]
    [Tooltip("Press R to upright and stop the vehicle")]
    public bool enableReset = true;

    Rigidbody rb;
    bool grounded;
    float steerInput;
    float throttleInput;
    float brakeInput;    // 0..1

    // ----------------------------- INPUT -----------------------------
    float GetAxis(string logical)
    {
        // Logical axes: "Horizontal" (-1 A / +1 D), "Vertical" (+1 W / -1 S), "Brake" (Space as 0..1)
        float value = 0f;

#if ENABLE_INPUT_SYSTEM
        // New Input System (no action asset required)
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (logical == "Horizontal")
        {
            if (k.aKey.isPressed) value -= 1f;
            if (k.dKey.isPressed) value += 1f;
        }
        else if (logical == "Vertical")
        {
            if (k.wKey.isPressed) value += 1f;
            if (k.sKey.isPressed) value -= 1f;
        }
        else if (logical == "Brake")
        {
            value = k.spaceKey.isPressed ? 1f : 0f;
        }
        else if (logical == "Reset")
        {
            value = (k.rKey.wasPressedThisFrame) ? 1f : 0f;
        }
#else
        // Old Input Manager fallback
        if (logical == "Horizontal") value = Input.GetAxisRaw("Horizontal");
        else if (logical == "Vertical") value = Input.GetAxisRaw("Vertical");
        else if (logical == "Brake") value = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        else if (logical == "Reset") value = Input.GetKeyDown(KeyCode.R) ? 1f : 0f;
#endif
        return Mathf.Clamp(value, -1f, 1f);
    }

    // ----------------------------- UNITY -----------------------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;
        rb.mass = Mathf.Max(800f, rb.mass);              // Trucks are heavy; makes motion stable
        rb.centerOfMass += new Vector3(0f, centerOfMassYOffset, 0f);
        rb.angularDamping = 0.8f;
        rb.linearDamping = 0.05f;
    }

    void Update()
    {
        steerInput = GetAxis("Horizontal");
        throttleInput = GetAxis("Vertical");
        brakeInput = Mathf.Max(0f, GetAxis("Brake"));

        if (enableReset && GetAxis("Reset") > 0.5f)
            ResetUprightAndStop();
    }

    void FixedUpdate()
    {
        // Ground check from the chassis center downward
        grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundRayLength, ~0, QueryTriggerInteraction.Ignore);

        // Convert world velocity to local space for easy manipulation
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float speed = rb.linearVelocity.magnitude;

        // ---------------- ACCEL / BRAKE ----------------
        float targetMaxSpeed = (throttleInput >= 0f) ? maxForwardSpeed : maxReverseSpeed;
        float desiredLongitudinal = throttleInput * targetMaxSpeed;

        // Simple longitudinal force towards desired speed
        float longError = Mathf.Clamp(desiredLongitudinal - localVel.z, -1f, 1f);
        float accelForce = longError * acceleration;

        // Apply extra braking if S or Space
        if (Mathf.Sign(localVel.z) != Mathf.Sign(throttleInput) && Mathf.Abs(throttleInput) > 0.1f)
            accelForce -= brakeStrength * Mathf.Sign(localVel.z);  // resist rolling the opposite way

        if (brakeInput > 0.1f)
            accelForce -= brakeStrength * Mathf.Sign(localVel.z);

        if (grounded)
            rb.AddForce(transform.forward * accelForce, ForceMode.Acceleration);

        // ---------------- STEERING ----------------
        // Steering gets less effective at higher speed
        float speedFactor = Mathf.InverseLerp(0f, maxForwardSpeed, speed);
        float steerLerp = Mathf.Lerp(1f, 1f - highSpeedSteerReduction, speedFactor);
        float yawDegrees = steerInput * steerRate * steerLerp * Time.fixedDeltaTime;
        Quaternion turn = Quaternion.Euler(0f, yawDegrees, 0f);
        rb.MoveRotation(rb.rotation * turn);

        // ---------------- LATERAL FRICTION (grip) ----------------
        // Kill some sideways velocity so it feels like tire grip.
        float side = localVel.x;
        float sideFriction = -side * lateralFriction;
        Vector3 sideForce = transform.right * sideFriction;
        if (grounded) rb.AddForce(sideForce, ForceMode.Acceleration);

        // ---------------- DOWNFORCE & EXTRA GRAVITY ----------------
        if (grounded)
        {
            rb.AddForce(-transform.up * (downforce * speedFactor), ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Physics.gravity * extraGravity, ForceMode.Acceleration);
        }

        // ---------------- AUTO-STABILIZE ROLL ----------------
        // Gently corrects excessive rolling so the truck doesn't tip too easily.
        Vector3 up = transform.up;
        Vector3 correctiveTorque = Vector3.Cross(up, Vector3.up) * 2.5f;
        rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
    }

    void ResetUprightAndStop()
    {
        // Place upright, keep current position, zero velocity.
        Vector3 pos = transform.position;
        Quaternion yawOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(pos + Vector3.up * 0.2f);
        rb.MoveRotation(yawOnly);
    }

#if UNITY_EDITOR
    // Tiny gizmo to visualize ground ray
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 start = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(start, start + Vector3.down * groundRayLength);
    }
#endif
}
