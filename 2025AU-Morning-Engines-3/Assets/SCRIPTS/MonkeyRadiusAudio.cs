using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
public class MonkeyRadiusAudio : MonoBehaviour
{
    public float radius = 12f;
    public string playerTag = "Player";

    private AudioSource source;
    private SphereCollider trigger;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        trigger = GetComponent<SphereCollider>();

        trigger.isTrigger = true;
        trigger.radius = radius;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = radius;

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        source.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (source.clip == null) return;
        if (!source.isPlaying) source.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (source.isPlaying) source.Stop();
    }

    private void OnValidate()
    {
        if (radius < 0.1f) radius = 0.1f;

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
            a.maxDistance = radius;
        }
    }
}
