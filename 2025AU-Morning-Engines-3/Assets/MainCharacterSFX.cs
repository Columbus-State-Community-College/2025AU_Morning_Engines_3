using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MainCharacterSFX : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip walkStep;   // walk.MP3
    public AudioClip runStep;    // run.MP3
    public AudioClip jump;
    public AudioClip reload;
    public AudioClip shoot;
    public AudioClip flinch;
    public AudioClip death;

    [Header("Volumes")]
    [Range(0f, 1f)] public float footstepVolume = 1f;
    [Range(0f, 1f)] public float oneShotVolume = 1f;

    [Header("Foot hit points (0..1 loop)")]
    public float walkHitA = 0.20f;
    public float walkHitB = 0.70f;
    public float runHitA = 0.15f;
    public float runHitB = 0.60f;

    [Header("Run Detection (matches your Animator params)")]
    public string isRunningParam = "IsRunning"; // bool in your Animator
    public string speedParam = "Speed";     // float in your Animator (backup)
    public float runSpeedThreshold = 0.60f;     // if Speed > this, treat as running (backup)

    [Header("Anti-spam")]
    public float minTimeBetweenSteps = 0.10f;

    private Animator anim;

    // 2 AudioSources:
    // - footSrc: footsteps (NO overlap, NO restart mid-clip)
    // - oneShotSrc: shoot/reload/flinch/etc
    private AudioSource footSrc;
    private AudioSource oneShotSrc;

    private float lastPhase = 0f;
    private float lastStepTime = -999f;

    private int lastBaseStateHash = 0;
    private int lastUpperStateHash = 0;

    private void Awake()
    {
        anim = GetComponent<Animator>();

        // Ensure 2 AudioSources exist on this object
        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 2) { footSrc = sources[0]; oneShotSrc = sources[1]; }
        else if (sources.Length == 1) { footSrc = sources[0]; oneShotSrc = gameObject.AddComponent<AudioSource>(); }
        else { footSrc = gameObject.AddComponent<AudioSource>(); oneShotSrc = gameObject.AddComponent<AudioSource>(); }

        Setup3D(footSrc);
        Setup3D(oneShotSrc);

        footSrc.loop = false;
        oneShotSrc.loop = false;
    }

    private void Setup3D(AudioSource s)
    {
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.dopplerLevel = 0f;
        s.reverbZoneMix = 0f;
    }

    private void Update()
    {
        // =========================
        // FOOTSTEPS (animation-cycle based, no overlap, correct run/walk)
        // =========================
        var baseInfo = anim.GetCurrentAnimatorStateInfo(0);

        bool inLocomotion =
            baseInfo.IsName("WalkForward") || baseInfo.IsName("WalkBackward") ||
            baseInfo.IsName("WalkLeft") || baseInfo.IsName("WalkRight") ||
            baseInfo.IsName("RunForward") || baseInfo.IsName("RunBackward");

        if (inLocomotion)
        {
            // Decide running by Animator parameters (NOT by state name)
            bool isRun = GetIsRunning();

            float phase = baseInfo.normalizedTime % 1f;

            bool crossed =
                isRun
                ? (Crossed(lastPhase, phase, runHitA) || Crossed(lastPhase, phase, runHitB))
                : (Crossed(lastPhase, phase, walkHitA) || Crossed(lastPhase, phase, walkHitB));

            if (crossed)
            {
                AudioClip clip = isRun ? runStep : walkStep;

                // Never restart mid-clip (prevents "first step looping")
                if (!footSrc.isPlaying && clip != null && Time.time - lastStepTime >= minTimeBetweenSteps)
                {
                    footSrc.clip = clip;
                    footSrc.volume = footstepVolume;
                    footSrc.Play();
                    lastStepTime = Time.time;
                }
            }

            lastPhase = phase;
        }
        else
        {
            if (footSrc.isPlaying) footSrc.Stop();
            lastPhase = 0f;
        }

        // =========================
        // OPTIONAL: one-shots on state enter
        // =========================
        int baseHash = baseInfo.fullPathHash;
        if (baseHash != lastBaseStateHash)
        {
            if (baseInfo.IsName("Jump")) PlayOneShotSafe(jump);
            if (baseInfo.IsName("Flinch")) PlayOneShotSafe(flinch);
            if (baseInfo.IsName("Death")) PlayOneShotSafe(death);
            lastBaseStateHash = baseHash;
        }

        // UpperBody layer (Reload/Shoot states may or may not be reliable)
        if (anim.layerCount > 1)
        {
            var upInfo = anim.GetCurrentAnimatorStateInfo(1);
            int upHash = upInfo.fullPathHash;

            if (upHash != lastUpperStateHash)
            {
                if (upInfo.IsName("Reload")) PlayOneShotSafe(reload);
                if (upInfo.IsName("ShootUpperBody")) PlayOneShotSafe(shoot);
                if (upInfo.IsName("Flinch")) PlayOneShotSafe(flinch);

                lastUpperStateHash = upHash;
            }
        }
    }

    private bool GetIsRunning()
    {
        // Prefer IsRunning bool if it exists
        if (HasParam(isRunningParam, AnimatorControllerParameterType.Bool))
            return anim.GetBool(isRunningParam);

        // Fallback: Speed float threshold
        if (HasParam(speedParam, AnimatorControllerParameterType.Float))
            return anim.GetFloat(speedParam) > runSpeedThreshold;

        return false;
    }

    private bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(name)) return false;

        foreach (var p in anim.parameters)
        {
            if (p.type == type && p.name == name)
                return true;
        }
        return false;
    }

    private bool Crossed(float prev, float now, float hit)
    {
        if (prev <= now) return prev < hit && now >= hit;
        return prev < hit || now >= hit; // wrap-around
    }

    private void PlayOneShotSafe(AudioClip clip)
    {
        if (!clip) return;
        oneShotSrc.PlayOneShot(clip, oneShotVolume);
    }

    // =========================
    // GUARANTEED calls (use these from your gameplay scripts)
    // =========================
    public void PlayShootSFX() => PlayOneShotSafe(shoot);
    public void PlayReloadSFX() => PlayOneShotSafe(reload);
    public void PlayFlinchSFX() => PlayOneShotSafe(flinch);
    public void PlayJumpSFX() => PlayOneShotSafe(jump);
    public void PlayDeathSFX() => PlayOneShotSafe(death);
}
