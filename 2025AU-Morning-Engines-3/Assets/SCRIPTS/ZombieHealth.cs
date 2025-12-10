using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    private bool isDown = false;
    public bool IsDown => isDown;

    // Is this zombie currently being carried on the player's back?
    public bool IsCarried { get; private set; } = false;

    private ZombieAI zombieAI;
    private Collider zombieCollider;
    private Rigidbody rb;

    [Header("Tip Over")]
    [Tooltip("If true, zombie tips forward (face-down). If false, tips sideways.")]
    public bool tipForward = true;

    [Tooltip("How quickly the zombie rotates into its tipped pose.")]
    public float tipSpeed = 6f;

    private Quaternion targetRotation;
    private bool tipping = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        zombieAI = GetComponent<ZombieAI>();
        zombieCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (tipping)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * tipSpeed
            );
        }
    }

    public void TakeDamage(float amount)
    {
        // Don’t take more damage once down or while being carried
        if (isDown || IsCarried)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        Debug.Log(name + " took " + amount + " damage. Current health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            GoDown();
        }
    }

    private void GoDown()
    {
        if (isDown)
            return;

        isDown = true;
        Debug.Log(name + " is down!");

        // Stop AI movement
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // Let physics handle initial fall to the ground
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Decide which way to tip
        Vector3 tipEuler;
        if (tipForward)
        {
            // Face-down
            tipEuler = new Vector3(90f, transform.eulerAngles.y, 0f);
        }
        else
        {
            // Sideways
            tipEuler = new Vector3(0f, transform.eulerAngles.y, 90f);
        }

        targetRotation = Quaternion.Euler(tipEuler);
        tipping = true;

        // Optional: push slightly into floor so it looks grounded
        Vector3 pos = transform.position;
        pos.y -= 0.5f;
        transform.position = pos;
    }

    // ===========================================================
    // ================== CARRY / DEPOSIT ========================
    // ===========================================================

    /// <summary>
    /// Called by ZombieCarrier when the player picks this zombie up.
    /// Parents it to the carry point and disables physics collisions.
    /// </summary>
    public void SetCarried(Transform parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("ZombieHealth.SetCarried called with null parent.");
            return;
        }

        if (!isDown)
        {
            // Should only carry dead/down zombies, but we’ll allow it and just log.
            Debug.LogWarning("ZombieHealth.SetCarried called but zombie is not down yet.");
        }

        IsCarried = true;
        tipping = false;

        // Stop AI just in case
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // Turn off physics so it doesn't fight the player/truck
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable collider while carried so it doesn't shove the player/truck
        if (zombieCollider != null)
        {
            zombieCollider.enabled = false;
        }

        // Parent to the carry point on the player's back
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Called by TruckController when the zombie is dropped into the truck bed.
    /// Parents it to the cargo root (drop point), snaps exactly to that position, physics off.
    /// </summary>
    public void SetDeposited(Transform truckCargoRoot, Vector3 localOffset)
    {
        if (truckCargoRoot == null)
        {
            Debug.LogWarning("ZombieHealth.SetDeposited called with null truckCargoRoot.");
            return;
        }

        IsCarried = false;  // no longer on player's back
        isDown = true;      // definitely still a corpse
        tipping = false;

        // Parent to the truck drop point
        transform.SetParent(truckCargoRoot);

        // ALWAYS snap exactly to the cargo root position in local space.
        // localOffset is kept in the signature but ignored here for simplicity.
        transform.localPosition = Vector3.zero;

        // Lay the body flat in the truck bed, same every time.
        Vector3 tipEuler;
        if (tipForward)
        {
            tipEuler = new Vector3(90f, 0f, 0f);
        }
        else
        {
            tipEuler = new Vector3(0f, 0f, 90f);
        }
        transform.localRotation = Quaternion.Euler(tipEuler);

        // Keep it fully static in the truck
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Collider stays disabled in the truck so it can't mess with physics
        if (zombieCollider != null)
        {
            zombieCollider.enabled = false;
        }
    }
}
