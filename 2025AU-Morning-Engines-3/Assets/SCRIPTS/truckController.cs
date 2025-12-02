using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TruckController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 1500f;
    public float maxSpeed = 20f;
    public float turnSpeed = 75f;
    public float brakeForce = 2500f;

    [Header("Setup")]
    public Transform seatPoint;   // Where player will sit / be parented
    public Transform exitPoint;   // Where player appears when exiting
    public Camera truckCamera;    // Camera used when driving

    [Header("State")]
    public bool isActive = false; // Controlled only when player is inside

    private Rigidbody rb;

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

        // Forward movement
        Vector3 velocity = rb.linearVelocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        // Limit forward/back speed
        float targetSpeed = throttle * maxSpeed;
        float speedDiff = targetSpeed - localVelocity.z;

        float accelForce = Mathf.Clamp(speedDiff * acceleration, -brakeForce, acceleration);
        Vector3 force = transform.forward * accelForce;
        rb.AddForce(force * Time.fixedDeltaTime, ForceMode.Acceleration);

        // Simple steering (only when moving a bit)
        if (Mathf.Abs(localVelocity.z) > 0.5f)
        {
            float turn = steer * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(localVelocity.z);
            Quaternion turnRot = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRot);
        }
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
