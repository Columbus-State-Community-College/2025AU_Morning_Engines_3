using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ZombieProximityAudio : MonoBehaviour
{
    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Hearing Radius")]
    [SerializeField] private float radius = 12f;

    [Header("Loop Clip (REQUIRED)")]
    [SerializeField] private AudioClip loopClip;

    [Header("Loop Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float loopVolume = 0.65f;

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 12f;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private AudioSource source;
    private SphereCollider trigger;
    private Rigidbody rb;
    private int insideCount = 0;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        trigger = GetComponent<SphereCollider>();
        rb = GetComponent<Rigidbody>();

        // Trigger setup
        trigger.isTrigger = true;
        trigger.radius = radius;

        // Rigidbody setup (reliable triggers)
        rb.isKinematic = true;
        rb.useGravity = false;

        // Audio setup
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = (maxDistance > 0.01f) ? maxDistance : radius;

        source.loop = true;
        source.volume = loopVolume;
        source.clip = loopClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        insideCount++;

        if (loopClip == null)
        {
            if (log) Debug.LogWarning("[ZombieAudio] No loopClip assigned.", this);
            return;
        }

        // Start loop once when player enters
        if (!source.isPlaying)
        {
            source.loop = true;
            source.clip = loopClip;
            source.volume = loopVolume;
            source.Play();

            if (log) Debug.Log("[ZombieAudio] Loop started.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        insideCount = Mathf.Max(0, insideCount - 1);

        // Stop only when fully outside
        if (insideCount == 0 && source.isPlaying)
        {
            source.Stop();
            if (log) Debug.Log("[ZombieAudio] Loop stopped.", this);
        }
    }

    private void OnValidate()
    {
        if (radius < 0.1f) radius = 0.1f;

        if (trigger != null) trigger.radius = radius;

        if (maxDistance < 0.1f) maxDistance = radius;
        if (source != null) source.maxDistance = maxDistance;
    }
}
