using UnityEngine;
using TMPro;

[RequireComponent(typeof(OnFootPlayerController))]
public class ZombieCarrier : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Radius around the player to search for downed zombies.")]
    public float pickupRadius = 2f;

    [Tooltip("Layer used by zombies. If left at 0, all layers are checked.")]
    public LayerMask zombieLayer;

    [Tooltip("Where on the player the carried zombie will be parented (e.g., on the back).")]
    public Transform carryPoint;

    [Range(0.1f, 1f)]
    [Tooltip("Movement speed multiplier while carrying a zombie.")]
    public float carrySpeedMultiplier = 0.65f;

    [Header("Debug (read-only at runtime)")]
    public bool isCarryingZombie = false;
    public ZombieHealth carriedZombie = null;

    private OnFootPlayerController controller;

    private float baseWalkSpeed;
    private float baseRunSpeed;
    private float baseBackwardWalkSpeed;
    private float baseBackwardRunSpeed;

    private void Awake()
    {
        controller = GetComponent<OnFootPlayerController>();

        baseWalkSpeed = controller.walkSpeed;
        baseRunSpeed = controller.runSpeed;
        baseBackwardWalkSpeed = controller.backwardWalkSpeed;
        baseBackwardRunSpeed = controller.backwardRunSpeed;
    }

    private void Update()
    {
        if (!controller.isActive)
            return;

        if (!isCarryingZombie)
        {
            TryPickupZombie();
        }

        ApplySpeedModifier();
    }

    private void ApplySpeedModifier()
    {
        if (isCarryingZombie)
        {
            controller.walkSpeed = baseWalkSpeed * carrySpeedMultiplier;
            controller.runSpeed = baseRunSpeed * carrySpeedMultiplier;
            controller.backwardWalkSpeed = baseBackwardWalkSpeed * carrySpeedMultiplier;
            controller.backwardRunSpeed = baseBackwardRunSpeed * carrySpeedMultiplier;
        }
        else
        {
            controller.walkSpeed = baseWalkSpeed;
            controller.runSpeed = baseRunSpeed;
            controller.backwardWalkSpeed = baseBackwardWalkSpeed;
            controller.backwardRunSpeed = baseBackwardRunSpeed;
        }
    }

    private void TryPickupZombie()
    {
        int mask = zombieLayer.value == 0 ? ~0 : zombieLayer.value;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            pickupRadius,
            mask
        );

        ZombieHealth candidate = null;
        foreach (Collider hit in hits)
        {
            ZombieHealth zh = hit.GetComponentInParent<ZombieHealth>();
            if (zh != null && zh.IsDown && !zh.IsCarried)
            {
                candidate = zh;
                break;
            }
        }

        TMP_Text prompt = controller.promptText;

        if (candidate != null)
        {
            if (prompt != null)
                prompt.text = "Press F to pick up zombie";

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (carryPoint == null)
                {
                    Debug.LogWarning("ZombieCarrier: carryPoint is not assigned.");
                    return;
                }

                carriedZombie = candidate;
                isCarryingZombie = true;

                carriedZombie.SetCarried(carryPoint);

                if (prompt != null && prompt.text == "Press F to pick up zombie")
                    prompt.text = "";
            }
        }
        else
        {
            if (prompt != null && prompt.text == "Press F to pick up zombie")
                prompt.text = "";
        }
    }

    public void ClearCarriedZombie()
    {
        isCarryingZombie = false;
        carriedZombie = null;
    }
}
