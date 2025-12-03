using UnityEngine;

public class Pellet : MonoBehaviour
{
    [HideInInspector] public float damage = 10f;
    [HideInInspector] public float speed = 60f;

    private float lifeTime = 1.2f;
    private float timer = 0f;
    private LineRenderer tracer;

    private void Awake()
    {
        tracer = gameObject.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.startWidth = 0.03f;
        tracer.endWidth = 0.01f;
        tracer.material = new Material(Shader.Find("Unlit/Color"));
        tracer.material.color = Color.yellow;
    }

    void Update()
    {
        Vector3 startPoint = transform.position;
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        Vector3 endPoint = transform.position;

        tracer.SetPosition(0, startPoint);
        tracer.SetPosition(1, endPoint);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ZombieHealth z = other.GetComponent<ZombieHealth>();
        if (z != null)
        {
            z.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
