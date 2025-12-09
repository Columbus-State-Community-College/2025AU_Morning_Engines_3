using UnityEngine;

public class ShotgunPellet : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime;
    private Vector3 direction;
    private float timer;

    private Rigidbody rb;

    public void Init(Vector3 dir, float speed, float damage, float lifeTime)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
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
        // 🔥 CHANGE IS HERE ↓↓↓
        ZombieHealth zombie = other.GetComponent<ZombieHealth>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
