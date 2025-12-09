using UnityEngine;

public class ShotgunPellet : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime;
    private Vector3 direction;
    private float timer;

    private Rigidbody rb;
    private bool initialized = false;

    public void Init(Vector3 dir, float speed, float damage, float lifeTime)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
        initialized = true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            transform.position += direction * speed * Time.fixedDeltaTime;
        }

        timer += Time.fixedDeltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore trigger-only colliders (like detection zones, triggers, etc.)
        if (other.isTrigger)
        {
            return;
        }

        // Ignore the player so pellets do not vanish on firing
        // Change "Player" here if your player uses a different tag.
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            return;
        }

        // Apply damage to zombies
        ZombieHealth zombie = other.GetComponent<ZombieHealth>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
        }

        // Destroy pellet on any non-player, non-trigger hit
        Destroy(gameObject);
    }
}
