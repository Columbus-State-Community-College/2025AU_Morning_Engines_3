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

    // === TRACER ===
    private LineRenderer tracer;
    public float tracerLength = 0.5f;     // how long the streak looks
    public float tracerWidth = 0.03f;     // how thick the streak is

    public void Init(Vector3 dir, float speed, float damage, float lifeTime)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
        initialized = true;

        SetupTracer();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        // Move pellet
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            transform.position += direction * speed * Time.fixedDeltaTime;
        }

        // Update tracer
        if (tracer != null)
        {
            Vector3 endPos = transform.position - direction * tracerLength;

            tracer.SetPosition(0, transform.position);
            tracer.SetPosition(1, endPos);
        }

        // Lifetime
        timer += Time.fixedDeltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            return;

        ZombieHealth zombie = other.GetComponent<ZombieHealth>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    // ============================
    //       TRACER SETUP
    // ============================
    private void SetupTracer()
    {
        tracer = gameObject.AddComponent<LineRenderer>();

        tracer.positionCount = 2;
        tracer.startWidth = tracerWidth;
        tracer.endWidth = 0f;

        tracer.material = new Material(Shader.Find("Unlit/Color"));
        tracer.material.color = Color.yellow;   // tracer color

        tracer.numCapVertices = 0;
        tracer.numCornerVertices = 0;

        // Initial positions
        tracer.SetPosition(0, transform.position);
        tracer.SetPosition(1, transform.position - direction * tracerLength);
    }
}
