using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    public string speedParam = "Speed";         // float in your Animator (backup)
    public float runSpeedThreshold = 0.60f;     // if Speed > this, treat as running (backup)

    [Header("Anti-spam")]
    public float minTimeBetweenSteps = 0.10f;

    private Animator anim;

    private AudioSource footSrc;
    private AudioSource oneShotSrc;

    private float lastPhase = 0f;
    private float lastStepTime = -999f;

    private int lastBaseStateHash = 0;
    private int lastUpperStateHash = 0;

    // NEW: track run/walk mode changes to force correct clip asap
    private bool lastIsRun = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();

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
        var baseInfo = anim.GetCurrentAnimatorStateInfo(0);

        bool inLocomotion =
            baseInfo.IsName("WalkForward") || baseInfo.IsName("WalkBackward") ||
            baseInfo.IsName("WalkLeft") || baseInfo.IsName("WalkRight") ||
            baseInfo.IsName("RunForward") || baseInfo.IsName("RunBackward");

        if (inLocomotion)
        {
            float phase = baseInfo.normalizedTime % 1f;

            // Decide running
            bool isRun = GetIsRunning();

            // NEW: if run/walk toggles (like W then Shift), stop current footstep so correct clip can kick in immediately
            if (isRun != lastIsRun)
            {
                if (footSrc.isPlaying) footSrc.Stop();
                // reset phase tracking so we don't miss the next hit after the toggle
                lastPhase = phase;
                lastIsRun = isRun;
            }

            bool crossed =
                isRun
                    ? (Crossed(lastPhase, phase, runHitA) || Crossed(lastPhase, phase, runHitB))
                    : (Crossed(lastPhase, phase, walkHitA) || Crossed(lastPhase, phase, walkHitB));

            if (crossed)
            {
                AudioClip clip = isRun ? runStep : walkStep;

                // If the wrong clip is playing (rare but possible), switch it
                if (footSrc.isPlaying && footSrc.clip != clip)
                    footSrc.Stop();

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
            lastIsRun = false;
        }

        // OPTIONAL: one-shots on state enter
        int baseHash = baseInfo.fullPathHash;
        if (baseHash != lastBaseStateHash)
        {
            if (baseInfo.IsName("Jump")) PlayOneShotSafe(jump);
            if (baseInfo.IsName("Flinch")) PlayOneShotSafe(flinch);
            if (baseInfo.IsName("Death")) PlayOneShotSafe(death);
            lastBaseStateHash = baseHash;
        }

        // UpperBody layer
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
        // ✅ Input-based: Shift + (W or S) should ALWAYS count as running audio
        bool shift = false, forward = false, backward = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            shift = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            forward = kb.wKey.isPressed;
            backward = kb.sKey.isPressed;
        }
#else
        shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        forward = Input.GetKey(KeyCode.W);
        backward = Input.GetKey(KeyCode.S);
#endif

        if (shift && (forward || backward))
            return true;

        // Prefer Animator bool if it exists (backup)
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
            if (p.type == type && p.name == name)
                return true;

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

    public void PlayShootSFX() => PlayOneShotSafe(shoot);
    public void PlayReloadSFX() => PlayOneShotSafe(reload);
    public void PlayFlinchSFX() => PlayOneShotSafe(flinch);
    public void PlayJumpSFX() => PlayOneShotSafe(jump);
    public void PlayDeathSFX() => PlayOneShotSafe(death);
}
