using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    private bool isDown = false;
    public bool IsDown => isDown;

    // NEW: carried flag so we don't pick up the same zombie twice
    public bool IsCarried { get; private set; } = false;

    private ZombieAI zombieAI;
    private Collider zombieCollider;
    private Rigidbody rb;

    // Tip-over settings
    [Header("Tip Over")]
    public bool tipForward = true;
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
        if (isDown) return;

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
        isDown = true;
        Debug.Log(name + " is down!");

        // Stop AI movement
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // Let physics handle the body when it first goes down
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
            // face-down
            tipEuler = new Vector3(90f, transform.eulerAngles.y, 0f);
        }
        else
        {
            // sideways
            tipEuler = new Vector3(0f, transform.eulerAngles.y, 90f);
        }

        targetRotation = Quaternion.Euler(tipEuler);
        tipping = true;

        // Push slightly into floor so it looks grounded
        Vector3 pos = transform.position;
        pos.y -= 0.5f;
        transform.position = pos;
    }

    // ===========================================================
    // =============== CARRY / DEPOSIT HELPERS ===================
    // ===========================================================

    /// <summary>
    /// Called when the player picks this zombie up.
    /// Parents to the carry point and disables physics.
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
            Debug.LogWarning("ZombieHealth.SetCarried called but zombie is not down yet.");
        }

        IsCarried = true;
        tipping = false;

        // Disable AI just in case
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // Stop physics while on the player's back
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Keep collider so triggers still work, but it's fine either way
        if (zombieCollider != null)
        {
            zombieCollider.enabled = true;
        }

        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Called when the zombie is dropped into the truck bed.
    /// Parents to the truck cargo root, snaps flat, and disables motion.
    /// </summary>
    public void SetDeposited(Transform truckCargoRoot, Vector3 localOffset)
    {
        if (truckCargoRoot == null)
        {
            Debug.LogWarning("ZombieHealth.SetDeposited called with null truckCargoRoot.");
            return;
        }

        IsCarried = false;
        tipping = false;

        transform.SetParent(truckCargoRoot);
        transform.localPosition = localOffset;

        // Lay the body flat again in the truck bed
        Vector3 tipEuler;
        if (tipForward)
        {
            tipEuler = new Vector3(90f, transform.eulerAngles.y, 0f);
        }
        else
        {
            tipEuler = new Vector3(0f, transform.eulerAngles.y, 90f);
        }
        transform.localRotation = Quaternion.Euler(tipEuler);

        // Keep it kinematic so it doesn't slide around
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (zombieCollider != null)
        {
            zombieCollider.enabled = true;
        }
    }
}
