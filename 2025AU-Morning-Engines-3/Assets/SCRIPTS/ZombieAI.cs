using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ZombieAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private float attackFlashDuration = 0.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animCrossFade = 0.12f;

    [SerializeField] private string idleState = "Zom_Idle_Anim01";
    [SerializeField] private string walkState = "Zom_walkCycle_01";
    [SerializeField] private string chaseState = "Zom_Chase_Anim01";
    [SerializeField] private string attackState = "Zom_Attack_Anim01";
    [SerializeField] private string hitState = "Zom_Hit_Anim01"; // optional

    private Transform player;
    private PlayerHealth playerHealth;

    private float lastAttackTime = Mathf.NegativeInfinity;

    private Rigidbody rb;
    private bool isChasing = false;

    private Renderer zombieRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    private bool isAttacking = false;
    private string currentAnimState = "";

    private int idleHash;
    private int walkHash;
    private int chaseHash;
    private int attackHash;
    private int hitHash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (!animator) animator = GetComponentInChildren<Animator>();

        idleHash = Animator.StringToHash(idleState);
        walkHash = Animator.StringToHash(walkState);
        chaseHash = Animator.StringToHash(chaseState);
        attackHash = Animator.StringToHash(attackState);
        hitHash = Animator.StringToHash(hitState);

        zombieRenderer = GetComponentInChildren<Renderer>();
        if (zombieRenderer != null)
        {
            originalColor = zombieRenderer.material.color;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning("ZombieAI: No GameObject with tag 'Player' found in the scene.");
        }

        ForceState(idleState);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        isChasing = distance <= detectionRadius;

        if (!isChasing)
        {
            if (!isAttacking) PlayState(idleState);
            return;
        }

        ChasePlayer(distance);
    }

    private void ChasePlayer(float distanceToPlayer)
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
        }

        if (distanceToPlayer > stoppingDistance)
        {
            Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + move);

            if (!isAttacking)
            {
                if (HasState(chaseHash)) PlayState(chaseState);
                else PlayState(walkState);
            }
        }
        else
        {
            if (!isAttacking) PlayState(idleState);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!isChasing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (playerHealth == null)
                {
                    playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;

                    TriggerAttackEffects();
                    TriggerAttackAnim();
                }
            }
        }
    }

    private void TriggerAttackAnim()
    {
        if (!animator) return;
        if (!HasState(attackHash)) return;

        isAttacking = true;

        // Important: update cached state so we don't "think" we're still in chase/walk.
        currentAnimState = attackState;

        animator.CrossFade(attackHash, animCrossFade);

        StopCoroutine(nameof(EndAttackRoutine));
        StartCoroutine(nameof(EndAttackRoutine));
    }

    private IEnumerator EndAttackRoutine()
    {
        // If your attack clip is longer, bump this value a bit.
        yield return new WaitForSeconds(0.55f);

        isAttacking = false;

        // Important: invalidate cached state so locomotion can re-crossfade immediately.
        currentAnimState = "";

        // Force a sane state right away so it doesn't freeze on the last attack frame.
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (!isChasing) ForceState(idleState);
            else if (dist > stoppingDistance)
            {
                if (HasState(chaseHash)) ForceState(chaseState);
                else ForceState(walkState);
            }
            else
            {
                ForceState(idleState);
            }
        }
        else
        {
            ForceState(idleState);
        }
    }

    public void PlayHitAnimIfExists()
    {
        if (!animator) return;
        if (isAttacking) return;

        if (HasState(hitHash))
        {
            // Invalidate cache so we can cleanly return to locomotion afterward
            currentAnimState = "";
            animator.CrossFade(hitHash, animCrossFade);
        }
    }

    private void PlayState(string stateName)
    {
        if (!animator) return;
        if (isAttacking) return;

        if (currentAnimState == stateName) return;

        int h = Animator.StringToHash(stateName);
        if (!HasState(h)) return;

        currentAnimState = stateName;
        animator.CrossFade(h, animCrossFade);
    }

    private void ForceState(string stateName)
    {
        if (!animator) return;

        int h = Animator.StringToHash(stateName);
        if (!HasState(h)) return;

        currentAnimState = stateName;
        animator.CrossFade(h, animCrossFade);
    }

    private bool HasState(int stateHash)
    {
        return animator && animator.HasState(0, stateHash);
    }

    private void TriggerAttackEffects()
    {
        if (zombieRenderer == null) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRedCoroutine());
    }

    private IEnumerator FlashRedCoroutine()
    {
        zombieRenderer.material.color = Color.red;
        yield return new WaitForSeconds(attackFlashDuration);
        zombieRenderer.material.color = originalColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
