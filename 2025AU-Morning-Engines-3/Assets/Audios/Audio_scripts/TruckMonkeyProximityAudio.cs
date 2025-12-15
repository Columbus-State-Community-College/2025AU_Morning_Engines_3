using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class TruckMonkeyProximityAudio : MonoBehaviour
{
    [Header("Player Tag")]
    public string playerTag = "Player";

    private AudioSource source;
    private SphereCollider trigger;
    private Rigidbody rb;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        trigger = GetComponent<SphereCollider>();
        rb = GetComponent<Rigidbody>();

        // ---- Trigger setup ----
        trigger.isTrigger = true;

        // ---- Rigidbody setup (required for triggers) ----
        rb.isKinematic = true;
        rb.useGravity = false;

        // ---- Audio setup (DO NOT touch min/max distance) ----
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;

        // Match trigger radius to AudioSource max distance
        trigger.radius = Mathf.Max(0.1f, source.maxDistance);

        // Start silent
        if (source.isPlaying)
            source.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (source.clip == null) return;

        source.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        source.Stop();
    }
}
