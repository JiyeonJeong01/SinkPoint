using UnityEngine;

[DefaultExecutionOrder(110)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsZeroGravityHash = Animator.StringToHash("IsZeroGravity");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsFiringHash = Animator.StringToHash("IsFiring");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");
    private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombatController combatController;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float movementDampTime = 0.1f;

    [Header("Aim")]
    [SerializeField, Range(0f, 1f)] private float spineAimWeight = 1f;

    private Transform spine;

    private void Awake()
    {
        playerController ??= GetComponent<PlayerController>();
        body ??= GetComponent<Rigidbody>();
        combatController ??= GetComponent<PlayerCombatController>();
    }

    private void Start()
    {
        if (playerController != null && body != null && visualRoot != null && animator != null && combatController != null)
        {
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            return;
        }

        Debug.LogError(
            $"{nameof(PlayerAnimationController)} on '{name}' requires Player Controller, Combat Controller, Rigidbody, Visual Root, and Animator references.",
            this);
        enabled = false;
    }

    private void Update()
    {
        Vector3 up = playerController.GravityUp;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, up);
        Vector3 localPlanarVelocity = visualRoot.InverseTransformDirection(planarVelocity);
        float moveSpeed = playerController.CurrentMoveSpeed;
        float normalizationSpeed = moveSpeed > Mathf.Epsilon ? moveSpeed : 1f;

        float moveX = Mathf.Clamp(localPlanarVelocity.x / normalizationSpeed, -1f, 1f);
        float moveY = Mathf.Clamp(localPlanarVelocity.z / normalizationSpeed, -1f, 1f);
        float normalizedSpeed = Mathf.Clamp01(planarVelocity.magnitude / normalizationSpeed);

        animator.SetFloat(MoveXHash, moveX, movementDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, movementDampTime, Time.deltaTime);
        animator.SetFloat(MoveSpeedHash, normalizedSpeed, movementDampTime, Time.deltaTime);
        animator.SetBool(IsGroundedHash, playerController.MotionState == PlayerMotionStateId.Grounded);
        animator.SetBool(IsZeroGravityHash, playerController.MotionState == PlayerMotionStateId.ZeroGravity);
        animator.SetFloat(VerticalSpeedHash, Vector3.Dot(body.linearVelocity, up));
        animator.SetBool(IsSprintingHash, playerController.IsSprinting);
        animator.SetBool(IsCrouchingHash, playerController.IsCrouching);
    }

    private void LateUpdate()
    {
        animator.SetBool(IsFiringHash, combatController.IsFiring);
        animator.SetFloat(AimPitchHash, combatController.AimPitchDegrees);
        if (combatController.ReloadStartedThisFrame)
        {
            animator.SetTrigger(ReloadHash);
        }

        ApplyAimPitch(combatController.AimPitchDegrees);
    }

    private void ApplyAimPitch(float pitchDegrees)
    {
        Vector3 rotationAxis = visualRoot.right;
        ApplyBonePitch(spine, rotationAxis, pitchDegrees * spineAimWeight);
    }

    private static void ApplyBonePitch(Transform bone, Vector3 axis, float angle)
    {
        if (bone == null || Mathf.Approximately(angle, 0f))
        {
            return;
        }

        bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
    }
}
