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

    private void Awake()
    {
        currentHealth = maxHealth;
        zombieAI = GetComponent<ZombieAI>();
        zombieCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float amount)
    {
        if (isDown) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{name} took {amount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            GoDown();
        }
    }

    private void GoDown()
    {
        isDown = true;
        Debug.Log($"{name} is down!");

        // Stop moving
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // Optional: make them ragdoll, change color, or sink a bit
        // Example: tint them darker so the player knows they are capturable
        // GetComponent<Renderer>().material.color = Color.gray;

        // You probably want to keep the collider so you can "pick them up" later.
        // So we don't Destroy(gameObject) here.
    }
}
