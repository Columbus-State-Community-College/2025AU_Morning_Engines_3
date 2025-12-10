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

    [Header("Tracer Settings")]
    public bool enableTracer = true;
    public float tracerLength = 0.5f;
    public float tracerWidth = 0.03f;
    public Color tracerColor = Color.yellow;

    [Tooltip("Optional: assign a material here (e.g. Sprites/Default with yellow color).")]
    public Material tracerMaterial;

    private LineRenderer tracer;

    public void Init(Vector3 dir, float speed, float damage, float lifeTime)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
        initialized = true;

        if (enableTracer)
        {
            SetupTracer();
        }
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

        // Update tracer positions
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
        tracer = GetComponent<LineRenderer>();
        if (tracer == null)
        {
            tracer = gameObject.AddComponent<LineRenderer>();
        }

        tracer.positionCount = 2;
        tracer.useWorldSpace = true;

        tracer.startWidth = tracerWidth;
        tracer.endWidth = 0f;

        // MATERIAL HANDLING
        if (tracerMaterial != null)
        {
            tracer.material = tracerMaterial;
        }
        else
        {
            // Fallback: try a safe built-in shader
            Shader s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                tracer.material = new Material(s);
            }
            else
            {
                Debug.LogWarning("ShotgunPellet: Could not find Sprites/Default shader. Assign a tracerMaterial in the inspector.");
            }
        }

        // Color gradient (start solid, end faded)
        tracer.startColor = tracerColor;
        tracer.endColor = new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0f);

        tracer.numCapVertices = 0;
        tracer.numCornerVertices = 0;

        // Initial positions
        tracer.SetPosition(0, transform.position);
        tracer.SetPosition(1, transform.position - direction * tracerLength);
    }
}
