using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    private bool isDown = false;
    public bool IsDown => isDown;

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
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * tipSpeed);
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

        // We keep collider so player can still interact
        // Rotation: tip forward or sideways
        Vector3 tipEuler;

        if (tipForward)
        {
            tipEuler = new Vector3(90f, transform.eulerAngles.y, 0f); // face-down
        }
        else
        {
            tipEuler = new Vector3(0f, transform.eulerAngles.y, 90f); // tip sideways
        }

        targetRotation = Quaternion.Euler(tipEuler);
        tipping = true;

        // Optional: push slightly into floor so it looks grounded
        Vector3 pos = transform.position;
        pos.y -= 0.5f;
        transform.position = pos;
    }
}
