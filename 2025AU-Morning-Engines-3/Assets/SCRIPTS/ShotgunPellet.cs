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

        Debug.Log("ShotgunPellet initialized with speed " + speed + " and damage " + damage);
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
            rb.velocity = direction * speed;
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
        Debug.Log("Pellet hit: " + other.name);

        ZombieHealth zombie = other.GetComponent<ZombieHealth>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
