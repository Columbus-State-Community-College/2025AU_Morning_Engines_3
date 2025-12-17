using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CarRadiusAudio : MonoBehaviour
{
    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Trigger Radius")]
    [SerializeField] private float triggerRadius = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip engineLoop;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Header("3D Falloff")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 20f;

    private AudioSource source;
    private SphereCollider trigger;
    private Rigidbody rb;

    private bool playerInside;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        trigger = GetComponent<SphereCollider>();
        rb = GetComponent<Rigidbody>();

        // Trigger setup
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;

        // Rigidbody (required for trigger reliability)
        rb.isKinematic = true;
        rb.useGravity = false;

        // AudioSource setup
        source.playOnAwake = false;
        source.loop = true;
        source.clip = engineLoop;
        source.volume = volume;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;

        if (!source.isPlaying && engineLoop != null)
            source.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;

        if (source.isPlaying)
            source.Stop();
    }

    private void OnValidate()
    {
        if (triggerRadius < 0.5f) triggerRadius = 0.5f;
        if (trigger != null) trigger.radius = triggerRadius;

        if (maxDistance < minDistance)
            maxDistance = minDistance + 1f;

        if (source != null)
        {
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
        }
    }
}
