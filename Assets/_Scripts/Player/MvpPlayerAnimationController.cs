using UnityEngine;

[DefaultExecutionOrder(110)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MvpPlayerController), typeof(Rigidbody))]
public sealed class MvpPlayerAnimationController : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsZeroGravityHash = Animator.StringToHash("IsZeroGravity");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

    [Header("References")]
    [SerializeField] private MvpPlayerController playerController;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float movementDampTime = 0.1f;

    private void Awake()
    {
        playerController ??= GetComponent<MvpPlayerController>();
        body ??= GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (playerController != null && body != null && visualRoot != null && animator != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(MvpPlayerAnimationController)} on '{name}' requires Player Controller, Rigidbody, Visual Root, and Animator references.",
            this);
        enabled = false;
    }

    private void LateUpdate()
    {
        Vector3 up = playerController.GravityUp;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, up);
        Vector3 localPlanarVelocity = visualRoot.InverseTransformDirection(planarVelocity);
        float moveSpeed = playerController.MoveSpeed;
        float normalizationSpeed = moveSpeed > Mathf.Epsilon ? moveSpeed : 1f;

        float moveX = Mathf.Clamp(localPlanarVelocity.x / normalizationSpeed, -1f, 1f);
        float moveY = Mathf.Clamp(localPlanarVelocity.z / normalizationSpeed, -1f, 1f);
        float normalizedSpeed = Mathf.Clamp01(planarVelocity.magnitude / normalizationSpeed);

        animator.SetFloat(MoveXHash, moveX, movementDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, movementDampTime, Time.deltaTime);
        animator.SetFloat(MoveSpeedHash, normalizedSpeed, movementDampTime, Time.deltaTime);
        animator.SetBool(IsGroundedHash, playerController.MotionState == MvpPlayerMotionStateId.Grounded);
        animator.SetBool(IsZeroGravityHash, playerController.MotionState == MvpPlayerMotionStateId.ZeroGravity);
        animator.SetFloat(VerticalSpeedHash, Vector3.Dot(body.linearVelocity, up));
    }
}
