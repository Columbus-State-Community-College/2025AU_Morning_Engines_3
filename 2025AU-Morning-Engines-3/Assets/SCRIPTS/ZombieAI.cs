using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ZombieAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f; // seconds between hits

    private Transform player;
    private PlayerHealth playerHealth;

    private float lastAttackTime = Mathf.NegativeInfinity;

    private Rigidbody rb;
    private bool isChasing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // prevents tipping over
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning("ZombieAI: No GameObject with tag 'Player' found in the scene.");
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Start or stop chasing based on distance
        isChasing = distance <= detectionRadius;

        if (isChasing)
        {
            ChasePlayer(distance);
        }
    }

    private void ChasePlayer(float distanceToPlayer)
    {
        // Look at player on the horizontal plane only
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
        }

        // Move towards player until within stopping distance
        if (distanceToPlayer > stoppingDistance)
        {
            Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + move);
        }
    }

    // Collision-based attacking
    private void OnCollisionStay(Collision collision)
    {
        if (!isChasing) return; // only attack when actually aggro'd

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (playerHealth == null)
                {
                    playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;
                    Debug.Log($"{name} attacked player for {attackDamage} damage.");
                }
            }
        }
    }

    // Just to visualize the detection radius in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
