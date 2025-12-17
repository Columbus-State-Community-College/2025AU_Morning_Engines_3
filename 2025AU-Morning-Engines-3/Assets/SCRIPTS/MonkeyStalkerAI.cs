using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class MonkeyStalkerAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player; // optional; auto-finds by tag
    [SerializeField] private string playerTag = "Player";

    [Header("Optional: Visual Root (mesh pivot)")]
    [Tooltip("If your model faces the wrong way, assign the mesh/armature root here. If left null, rotates this GameObject.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Yaw offset in degrees if your model's forward axis is wrong. Try 90 or -90.")]
    [SerializeField] private float visualYawOffset = 0f;

    [Header("Animator Params")]
    [SerializeField] private string speedParam = "Speed";            // float 0..1
    [SerializeField] private string idleTypeParam = "IdleType";      // int 0 sit, 1 stand
    [SerializeField] private string standUpTriggerParam = "StandUp"; // trigger

    [Header("Distances")]
    [SerializeField] private float awarenessRadius = 14f;            // start stalking
    [SerializeField] private float stareRadius = 9f;                 // more stare
    [SerializeField] private float approachRadius = 7f;              // creep in
    [SerializeField] private float tooCloseRadius = 2.2f;            // freeze/flee
    [SerializeField] private float preferredFollowDistance = 4.5f;   // stalk distance

    [Header("Roam")]
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private Vector2 roamWaitRange = new Vector2(1.0f, 3.5f);

    [Header("Behavior Weights (when aware of player)")]
    [Range(0f, 1f)][SerializeField] private float approachChance = 0.55f;
    [Range(0f, 1f)][SerializeField] private float circleChance = 0.25f;
    [Range(0f, 1f)][SerializeField] private float stareChance = 0.35f;

    [Header("Idle Variety")]
    [Range(0f, 1f)][SerializeField] private float sitWhenIdleChanceFar = 0.55f;
    [Range(0f, 1f)][SerializeField] private float sitWhenIdleChanceNear = 0.10f;

    [Header("Too Close Reaction")]
    [Range(0f, 1f)][SerializeField] private float freezeChanceWhenTooClose = 0.50f;

    [Header("Speeds")]
    [SerializeField] private float walkAgentSpeed = 1.6f;
    [SerializeField] private float runAgentSpeed = 4.0f;

    [Header("Rotation")]
    [SerializeField] private float moveTurnSpeed = 14f;
    [SerializeField] private float lookTurnSpeed = 18f;

    [Header("Look At Player")]
    [SerializeField] private float lookAtRadius = 9f;
    [SerializeField] private bool lookAtOnlyWhenStopped = true;
    [Range(0f, 1f)][SerializeField] private float lookAtChanceNear = 0.65f;

    [Header("Aim Fear (Right-click makes it run)")]
    [SerializeField] private float fearAimRadius = 12f;
    [SerializeField] private float fleeOnAimDuration = 1.6f;
    [SerializeField] private float aimFleeCooldown = 0.6f;

    [Header("Instant Kill On Touch")]
    [SerializeField] private float instantKillDamage = 9999f;

    [Header("Debug")]
    [SerializeField] private bool logDecisions = false;

    private Animator animator;
    private NavMeshAgent agent;

    private int speedHash;
    private int idleTypeHash;
    private int standUpHash;

    private Vector3 roamAnchor;

    private float stateTimer;
    private float nextAimFleeAllowedTime = 0f;

    private bool isSitting = true;

    private enum BrainState { Roam, Stare, Approach, Follow, Circle, Flee, Freeze }
    private BrainState state = BrainState.Roam;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        speedHash = Animator.StringToHash(speedParam);
        idleTypeHash = Animator.StringToHash(idleTypeParam);
        standUpHash = Animator.StringToHash(standUpTriggerParam);

        roamAnchor = transform.position;

        animator.applyRootMotion = false;

        agent.updateRotation = false;   // we rotate manually
        agent.speed = walkAgentSpeed;

        if (visualRoot == null) visualRoot = transform;

        // make sure collider is trigger so it doesn't fight navmesh movement
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        // start sitting idle
        SetIdle(sit: true);
        SetAnimSpeed(0f);

        // start roaming soon
        EnterState(BrainState.Roam, Random.Range(roamWaitRange.x, roamWaitRange.y));
    }

    private void Update()
    {
        if (player == null)
        {
            RoamLogic();
            UpdateAnimationFromVelocity();
            RotateLogic(float.PositiveInfinity);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        bool isPlayerAiming = Input.GetMouseButton(1);

        // 1) Aim fear has top priority (burst flee)
        if (isPlayerAiming && dist <= fearAimRadius && Time.time >= nextAimFleeAllowedTime)
        {
            EnterState(BrainState.Flee, fleeOnAimDuration);
            nextAimFleeAllowedTime = Time.time + aimFleeCooldown;
        }

        // 2) Too close: freeze or flee (unless already fleeing from aim)
        if (dist <= tooCloseRadius && state != BrainState.Flee)
        {
            if (state != BrainState.Freeze)
            {
                bool freeze = Random.value < freezeChanceWhenTooClose;
                EnterState(freeze ? BrainState.Freeze : BrainState.Flee, Random.Range(0.9f, 1.8f));
            }
        }
        else
        {
            // 3) Main brain: if player is within awareness radius, STOP roaming and stalk
            if (dist <= awarenessRadius)
            {
                // if we're roaming, instantly switch into stalking
                if (state == BrainState.Roam)
                    PickAwareState(dist);

                // if timer is done, pick a new stalking action
                if (stateTimer <= 0f && state != BrainState.Flee && state != BrainState.Freeze)
                    PickAwareState(dist);
            }
            else
            {
                // player far: roam (unless fleeing)
                if (state != BrainState.Roam && state != BrainState.Flee)
                    EnterState(BrainState.Roam, Random.Range(roamWaitRange.x, roamWaitRange.y));
            }
        }

        // tick timer
        stateTimer -= Time.deltaTime;

        // run state
        switch (state)
        {
            case BrainState.Roam: RoamLogic(); break;
            case BrainState.Stare: StareLogic(); break;
            case BrainState.Approach: ApproachLogic(); break;
            case BrainState.Follow: FollowLogic(); break;
            case BrainState.Circle: CircleLogic(); break;
            case BrainState.Flee: FleeLogic(); break;
            case BrainState.Freeze: FreezeLogic(); break;
        }

        UpdateAnimationFromVelocity();
        RotateLogic(dist);
    }

    // ---------------- AWARE DECISIONS ----------------

    private void PickAwareState(float distToPlayer)
    {
        // near player: mostly stand, less sitting
        SetIdle(sit: Random.value < sitWhenIdleChanceNear);

        // boost staring when close
        float stareBoost = (distToPlayer <= stareRadius) ? 0.20f : 0f;

        float r = Random.value;

        if (r < Mathf.Clamp01(stareChance + stareBoost))
        {
            EnterState(BrainState.Stare, Random.Range(0.8f, 2.2f));
        }
        else if (r < Mathf.Clamp01(stareChance + stareBoost + circleChance))
        {
            EnterState(BrainState.Circle, Random.Range(1.0f, 2.4f));
        }
        else if (r < Mathf.Clamp01(stareChance + stareBoost + circleChance + approachChance))
        {
            // if already close, follow; if farther, approach
            EnterState(distToPlayer <= approachRadius ? BrainState.Follow : BrainState.Approach, Random.Range(1.0f, 2.6f));
        }
        else
        {
            EnterState(BrainState.Follow, Random.Range(1.2f, 3.0f));
        }
    }

    // ---------------- STATE LOGIC ----------------

    private void RoamLogic()
    {
        agent.speed = walkAgentSpeed;

        // roam uses timer as "wait before picking next destination"
        if (stateTimer <= 0f || !agent.hasPath || agent.remainingDistance <= 0.35f)
        {
            Vector3 dest = RandomNavmeshPoint(roamAnchor, roamRadius);
            agent.SetDestination(dest);

            SetIdle(sit: Random.value < sitWhenIdleChanceFar);
            stateTimer = Random.Range(roamWaitRange.x, roamWaitRange.y);
        }

        // if barely moving, don't jitter
        if (agent.velocity.sqrMagnitude < 0.01f && agent.hasPath && agent.remainingDistance <= 0.35f)
            agent.ResetPath();
    }

    private void StareLogic()
    {
        agent.ResetPath();
        agent.speed = 0f;

        SetIdle(sit: false);

        // nothing else — rotation will handle looking at player sometimes
        if (stateTimer <= 0f) EnterState(BrainState.Follow, Random.Range(1.0f, 2.0f));
    }

    private void ApproachLogic()
    {
        agent.speed = walkAgentSpeed;
        SetIdle(sit: false);

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        // offset so it feels like creeping, not a straight homing missile
        Vector3 side = Vector3.Cross(toPlayer.normalized, Vector3.up) * Random.Range(-1.6f, 1.6f);
        Vector3 target = player.position - toPlayer.normalized * preferredFollowDistance + side;

        agent.SetDestination(ClosestOnNavmesh(target));

        if (stateTimer <= 0f) EnterState(BrainState.Stare, Random.Range(0.8f, 1.7f));
    }

    private void FollowLogic()
    {
        agent.speed = walkAgentSpeed;
        SetIdle(sit: false);

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        // IMPORTANT: always update destination while following (this is what fixes “it won’t stalk me”)
        if (dist > preferredFollowDistance + 0.6f)
        {
            Vector3 target = player.position - toPlayer.normalized * preferredFollowDistance;
            agent.SetDestination(ClosestOnNavmesh(target));
        }
        else
        {
            // close enough: stop and stare sometimes
            agent.ResetPath();
        }

        if (stateTimer <= 0f) EnterState(BrainState.Stare, Random.Range(0.8f, 2.0f));
    }

    private void CircleLogic()
    {
        agent.speed = walkAgentSpeed;
        SetIdle(sit: false);

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
        if (Random.value < 0.5f) side = -side;

        Vector3 circlePoint = player.position + side * preferredFollowDistance;
        agent.SetDestination(ClosestOnNavmesh(circlePoint));

        if (stateTimer <= 0f) EnterState(BrainState.Follow, Random.Range(1.0f, 2.5f));
    }

    private void FleeLogic()
    {
        agent.speed = runAgentSpeed;
        SetIdle(sit: false);

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = transform.forward;

        Vector3 fleeTarget = transform.position + away.normalized * Random.Range(7f, 11f);
        agent.SetDestination(ClosestOnNavmesh(fleeTarget));

        if (stateTimer <= 0f) EnterState(BrainState.Stare, Random.Range(0.8f, 1.8f));
    }

    private void FreezeLogic()
    {
        agent.ResetPath();
        agent.speed = 0f;

        SetIdle(sit: false);

        if (stateTimer <= 0f)
            EnterState(Random.value < 0.55f ? BrainState.Stare : BrainState.Follow, Random.Range(0.8f, 2.0f));
    }

    // ---------------- ANIMATION ----------------

    private void UpdateAnimationFromVelocity()
    {
        float vel = agent.velocity.magnitude;
        float normalized = Mathf.InverseLerp(0f, runAgentSpeed, vel);
        SetAnimSpeed(normalized);

        if (vel > 0.1f && isSitting)
        {
            animator.ResetTrigger(standUpHash);
            animator.SetTrigger(standUpHash);
            SetIdle(sit: false);
        }
    }

    // ---------------- ROTATION ----------------

    private void RotateLogic(float distToPlayer)
    {
        // If moving: face movement direction
        Vector3 move = agent.desiredVelocity;
        move.y = 0f;

        if (move.sqrMagnitude > 0.02f)
        {
            FaceDirection(move.normalized, moveTurnSpeed);
            return;
        }

        // Not moving: sometimes look at player in creepy states
        if (player == null) return;
        if (distToPlayer > lookAtRadius) return;
        if (lookAtOnlyWhenStopped && agent.velocity.magnitude > 0.15f) return;

        bool stateAllowsLook =
            state == BrainState.Stare ||
            state == BrainState.Freeze ||
            state == BrainState.Circle ||
            state == BrainState.Follow;

        if (!stateAllowsLook) return;
        if (Random.value > lookAtChanceNear) return;

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        FaceDirection(dir.normalized, lookTurnSpeed);
    }

    private void FaceDirection(Vector3 dirNormalized, float turnSpeed)
    {
        Quaternion target = Quaternion.LookRotation(dirNormalized);
        if (Mathf.Abs(visualYawOffset) > 0.001f)
            target *= Quaternion.Euler(0f, visualYawOffset, 0f);

        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, target, Time.deltaTime * turnSpeed);
    }

    // ---------------- INSTANT KILL ----------------

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        ph.TakeDamage(instantKillDamage);
    }

    // ---------------- HELPERS ----------------

    private void EnterState(BrainState newState, float duration)
    {
        state = newState;
        stateTimer = duration;

        if (logDecisions) Debug.Log($"[MonkeyAI] -> {state} for {stateTimer:0.00}s", this);
    }

    private void SetIdle(bool sit)
    {
        animator.SetInteger(idleTypeHash, sit ? 0 : 1);
        isSitting = sit;
    }

    private void SetAnimSpeed(float normalized01)
    {
        animator.SetFloat(speedHash, Mathf.Clamp01(normalized01));
    }

    private Vector3 RandomNavmeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 16; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * radius;
            Vector3 sample = new Vector3(center.x + rnd.x, center.y + 1f, center.z + rnd.y);

            if (NavMesh.SamplePosition(sample, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                return hit.position;
        }
        return center;
    }

    private Vector3 ClosestOnNavmesh(Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }
}
