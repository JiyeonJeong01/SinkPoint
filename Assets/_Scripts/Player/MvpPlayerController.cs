using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(MvpPlayerInput))]
public sealed class MvpPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MvpPlayerInput input;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private MvpGravityState gravityState;
    [SerializeField] private Transform visualRoot;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5f;
    [SerializeField, Min(0f)] private float crouchSpeed = 1.5f;
    [SerializeField, Min(0f)] private float jumpSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpInputBufferDuration = 0.1f;

    [Header("Crouch")]
    [SerializeField, Range(0.5f, 1f)] private float crouchHeightRatio = 0.65f;
    [SerializeField] private LayerMask stanceCollisionMask = ~0;

    [Header("Rotation")]
    [FormerlySerializedAs("rotationSpeed")]
    [SerializeField, Min(0f)] private float gravityAlignmentSpeed = 720f;

    [Header("Grounding")]
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 50f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.15f;

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private readonly RaycastHit[] stanceHits = new RaycastHit[16];

    private Rigidbody body;
    private CapsuleCollider capsule;
    private MvpPlayerStateMachine stateMachine;
    private bool hasBufferedJump;
    private double jumpRequestExpiresAtRealtime;
    private float standingCapsuleHeight;
    private Vector3 standingCapsuleCenter;
    private bool didWarnAboutStanceBuffer;

    internal MvpPlayerMotionStateId MotionState => stateMachine.CurrentId;
    internal Vector3 GravityUp => -gravityState.Direction;
    internal float MoveSpeed => moveSpeed;
    internal float CurrentMoveSpeed { get; private set; }
    internal bool IsSprinting { get; private set; }
    internal bool IsCrouching { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        input ??= GetComponent<MvpPlayerInput>();
        body.useGravity = false;
        stateMachine = new MvpPlayerStateMachine();
        standingCapsuleHeight = capsule.height;
        standingCapsuleCenter = capsule.center;
        CurrentMoveSpeed = moveSpeed;
    }

    private void Start()
    {
        if (input != null && cameraTransform != null && gravityState != null && visualRoot != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(MvpPlayerController)} on '{name}' requires Input, Camera Transform, Gravity State, and Visual Root references.",
            this);
        enabled = false;
    }

    private void OnDisable()
    {
        ClearBufferedJump();
        IsSprinting = false;
        SetCrouching(false);
    }

    private void LateUpdate()
    {
        Vector3 up = -gravityState.Direction;
        Vector3 facingForward = Vector3.ProjectOnPlane(cameraTransform.forward, up);
        if (facingForward.sqrMagnitude < Mathf.Epsilon)
        {
            facingForward = Vector3.ProjectOnPlane(visualRoot.forward, up);
        }

        if (facingForward.sqrMagnitude < Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(facingForward.normalized, up);
        visualRoot.rotation = targetRotation;
    }

    private void FixedUpdate()
    {
        Vector3 gravityDirection = gravityState.Direction;
        Vector3 up = -gravityDirection;
        bool isGrounded = TryGetGroundNormal(gravityDirection, up, out Vector3 groundNormal);

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
        if (cameraForward.sqrMagnitude < Mathf.Epsilon)
        {
            cameraForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        }

        Vector3 cameraRight = Vector3.Cross(up, cameraForward).normalized;
        Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        bool hasGravity = gravityState.Strength > 0f;
        UpdateStance(isGrounded, hasGravity, up);
        UpdateSprint(isGrounded, hasGravity, moveInput);
        bool jumpRequested = UpdateBufferedJump(hasGravity);

        MvpPlayerFixedContext context = new MvpPlayerFixedContext(
            gravityDirection,
            up,
            groundNormal,
            moveDirection,
            hasGravity,
            isGrounded,
            jumpRequested,
            Vector3.Dot(body.linearVelocity, up));

        bool jumpExecuted = stateMachine.FixedTick(this, context);
        if (jumpExecuted)
        {
            ClearBufferedJump();
        }

        AlignWithGravity(up);
    }

    private bool UpdateBufferedJump(bool hasGravity)
    {
        bool receivedJump = input.TryConsumeJumpPressed(out double pressedAtRealtime);
        if (!input.AllowMovement || !hasGravity || IsCrouching)
        {
            ClearBufferedJump();
            return false;
        }

        if (receivedJump)
        {
            hasBufferedJump = true;
            jumpRequestExpiresAtRealtime = pressedAtRealtime + jumpInputBufferDuration;
        }

        if (!hasBufferedJump)
        {
            return false;
        }

        if (Time.realtimeSinceStartupAsDouble <= jumpRequestExpiresAtRealtime)
        {
            return true;
        }

        ClearBufferedJump();
        return false;
    }

    private void ClearBufferedJump()
    {
        hasBufferedJump = false;
        jumpRequestExpiresAtRealtime = 0d;
    }

    internal void ApplyGroundedMotion(MvpPlayerFixedContext context)
    {
        Vector3 moveDirection = context.MoveDirection;
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, context.GroundNormal).normalized;
        }

        Vector3 gravityVelocity = Vector3.Project(body.linearVelocity, context.GravityDirection);
        if (Vector3.Dot(gravityVelocity, context.GravityDirection) > 0f)
        {
            gravityVelocity = Vector3.zero;
        }

        body.linearVelocity = moveDirection * CurrentMoveSpeed + gravityVelocity;
        body.AddForce(-context.GroundNormal * gravityState.Strength, ForceMode.Acceleration);
    }

    internal void ApplyAirborneMotion(MvpPlayerFixedContext context)
    {
        Vector3 gravityVelocity = Vector3.Project(body.linearVelocity, context.GravityDirection);
        body.linearVelocity = context.MoveDirection * moveSpeed + gravityVelocity;
        body.AddForce(gravityState.Gravity, ForceMode.Acceleration);
    }

    private void UpdateSprint(bool isGrounded, bool hasGravity, Vector2 moveInput)
    {
        IsSprinting = input.AllowMovement
            && hasGravity
            && isGrounded
            && !IsCrouching
            && input.SprintHeld
            && moveInput.y > 0.1f;

        CurrentMoveSpeed = IsCrouching
            ? crouchSpeed
            : IsSprinting
                ? sprintSpeed
                : moveSpeed;
    }

    private void UpdateStance(bool isGrounded, bool hasGravity, Vector3 up)
    {
        if (IsCrouching)
        {
            if (!input.CrouchHeld && CanUseStandingCapsule(up))
            {
                SetCrouching(false);
            }

            return;
        }

        if (input.AllowMovement && hasGravity && isGrounded && input.CrouchHeld)
        {
            SetCrouching(true);
        }
    }

    private void SetCrouching(bool crouching)
    {
        IsCrouching = crouching;

        if (capsule == null)
        {
            return;
        }

        if (!crouching)
        {
            capsule.height = standingCapsuleHeight;
            capsule.center = standingCapsuleCenter;
            return;
        }

        float minimumHeight = capsule.radius * 2f;
        float crouchingHeight = Mathf.Max(minimumHeight, standingCapsuleHeight * crouchHeightRatio);
        float centerOffset = (standingCapsuleHeight - crouchingHeight) * 0.5f;
        capsule.height = crouchingHeight;
        capsule.center = standingCapsuleCenter - Vector3.up * centerOffset;
    }

    private bool CanUseStandingCapsule(Vector3 up)
    {
        Vector3 scale = transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = capsule.radius * radiusScale * 0.95f;
        float heightScale = Mathf.Abs(scale.y);
        float crouchingHeight = Mathf.Max(capsule.height * heightScale, radius * 2f);
        float standingHeight = Mathf.Max(standingCapsuleHeight * heightScale, radius * 2f);
        Vector3 crouchingCenter = transform.TransformPoint(capsule.center);
        Vector3 standingCenter = transform.TransformPoint(standingCapsuleCenter);
        Vector3 crouchingTop = crouchingCenter + up * (crouchingHeight * 0.5f - radius);
        Vector3 standingTop = standingCenter + up * (standingHeight * 0.5f - radius);
        float clearanceDistance = Vector3.Dot(standingTop - crouchingTop, up);

        if (clearanceDistance <= Mathf.Epsilon)
        {
            return true;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            crouchingTop,
            radius,
            up,
            stanceHits,
            clearanceDistance,
            stanceCollisionMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == stanceHits.Length && !didWarnAboutStanceBuffer)
        {
            Debug.LogWarning(
                $"{nameof(MvpPlayerController)} on '{name}' filled its stance clearance buffer.",
                this);
            didWarnAboutStanceBuffer = true;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = stanceHits[i].collider;
            if (hitCollider == null || hitCollider == capsule || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    internal void ApplyJump(MvpPlayerFixedContext context)
    {
        Vector3 moveDirection = context.MoveDirection;
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, context.GroundNormal).normalized;
        }

        body.linearVelocity = moveDirection * moveSpeed + context.Up * jumpSpeed;
        body.AddForce(gravityState.Gravity, ForceMode.Acceleration);
    }

    private void AlignWithGravity(Vector3 up)
    {
        Vector3 currentUp = body.rotation * Vector3.up;
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, up) * body.rotation;
        Quaternion nextRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            gravityAlignmentSpeed * Time.fixedDeltaTime);
        body.MoveRotation(nextRotation);
    }

    private bool TryGetGroundNormal(
        Vector3 gravityDirection,
        Vector3 up,
        out Vector3 groundNormal)
    {
        Vector3 scale = transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = capsule.radius * radiusScale * 0.9f;
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
        float castDistance = halfHeight - radius + groundProbeDistance;
        Vector3 origin = transform.TransformPoint(capsule.center);

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            gravityDirection,
            groundHits,
            castDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        groundNormal = up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == capsule || hit.distance >= nearestDistance)
            {
                continue;
            }

            float groundAngle = Vector3.Angle(hit.normal, up);
            if (groundAngle > maxGroundAngle)
            {
                continue;
            }

            nearestDistance = hit.distance;
            groundNormal = hit.normal;
        }

        return nearestDistance < float.PositiveInfinity;
    }
}
