using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class TruckMonkeyProximityAudio : MonoBehaviour
{
    [Header("Hearing Radius")]
    public float radius = 12f;

    [Header("Player Tag")]
    public string playerTag = "Player";

    [Header("Audio")]
    public float minDistance = 2f;

    private AudioSource source;
    private SphereCollider trigger;
    private Rigidbody rb;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        trigger = GetComponent<SphereCollider>();
        rb = GetComponent<Rigidbody>();

        // Trigger collider
        trigger.isTrigger = true;
        trigger.radius = radius;

        // Rigidbody (so trigger events work reliably)
        rb.isKinematic = true;
        rb.useGravity = false;

        // Audio settings
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Clamp(minDistance, 0f, radius);
        source.maxDistance = radius;

        source.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (source.clip == null) return;

        if (!source.isPlaying)
            source.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (source.isPlaying)
            source.Stop();
    }

    private void OnValidate()
    {
        if (radius < 0.1f) radius = 0.1f;
        if (minDistance < 0f) minDistance = 0f;

        var sc = GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
            sc.radius = radius;
        }

        var a = GetComponent<AudioSource>();
        if (a != null)
        {
            a.playOnAwake = false;
            a.loop = true;
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = Mathf.Clamp(minDistance, 0f, radius);
            a.maxDistance = radius;
        }

        var r = GetComponent<Rigidbody>();
        if (r != null)
        {
            r.isKinematic = true;
            r.useGravity = false;
        }
    }
}
