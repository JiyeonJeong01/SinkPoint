using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInput))]
public sealed class PlayerController : MonoBehaviour
{
    private const float MoveInputDeadZone = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GravityState gravityState;
    [SerializeField] private GravityManager gravityManager;
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
    [SerializeField, Min(0f)] private float groundSnapDistance = 0.3f;
    [SerializeField, Min(0f)] private float maxGroundSnapSpeed = 5f;
    [SerializeField, Min(0f)] private float maxGroundSnapUpwardSpeed = 0.5f;

    [Header("Zero Gravity Recoil")]
    [SerializeField] private bool enableZeroGravityRecoil = true;
    [SerializeField, Min(0f)] private float zeroGravityRecoilVelocityChange = 0.3f;
    [SerializeField, Min(0f)] private float maxZeroGravityRecoilSpeed = 4f;

    [Header("Grappling Hook")]
    [SerializeField, Min(0f)] private float grapplePullAcceleration = 30f;
    [SerializeField, Min(0f)] private float maxGrapplePullSpeed = 12f;
    [SerializeField, Min(0f)] private float grappleStopDistance = 1.1f;

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private readonly RaycastHit[] stanceHits = new RaycastHit[16];

    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerMotionStateMachine stateMachine;
    private bool hasBufferedJump;
    private double jumpRequestExpiresAtRealtime;
    private float standingCapsuleHeight;
    private Vector3 standingCapsuleCenter;
    private bool didWarnAboutStanceBuffer;
    private bool ownsTransitionPositionLock;
    private Vector3 transitionAnchorPosition;
    private RigidbodyConstraints constraintsBeforeGravityTransition;
    private Vector3 grappleAnchorPoint;
    private Vector3 grappleSurfaceNormal;
    private float grapplePullSpeed;

    [Header("Runtime State")]
    [SerializeField] private bool gravityTransitionActive;
    [Tooltip("현재 물리 프레임의 지면 Probe 결과입니다.")]
    [SerializeField] private bool runtimeGrounded;
    [Tooltip("현재 지면과 중력 Up 사이의 각도입니다.")]
    [SerializeField] private float runtimeGroundAngle;
    [Tooltip("캡슐 하단과 현재 지면 사이의 거리입니다. 지면이 없으면 -1입니다.")]
    [SerializeField] private float runtimeGroundDistance = -1f;
    [Tooltip("Grounded 이동을 적용하기 직전의 중력 Up 기준 속도입니다.")]
    [SerializeField] private float runtimeVerticalSpeedBeforeMotion;
    [Tooltip("이번 물리 프레임에 실제 점프 속도를 적용했는지 표시합니다.")]
    [SerializeField] private bool runtimeJumpExecuted;
    [Tooltip("이번 물리 프레임에 Ground Snap이 작동했는지 표시합니다.")]
    [SerializeField] private bool runtimeGroundSnapActive;
    [Tooltip("마지막 무중력 반작용 요청이 Rigidbody에 적용되었는지 표시합니다.")]
    [SerializeField] private bool lastZeroGravityRecoilApplied;
    [Tooltip("플레이어 Rigidbody의 현재 전체 속력입니다.")]
    [SerializeField] private float currentZeroGravityRecoilSpeed;
    [Tooltip("현재 전체 속력이 무중력 반작용 속도 상한에 도달했는지 표시합니다.")]
    [SerializeField] private bool zeroGravityRecoilSpeedLimitReached;
    [Tooltip("그래플 당김이 PlayerController의 Rigidbody 경로에서 활성인지 표시합니다.")]
    [SerializeField] private bool grapplePullActive;
    [Tooltip("그래플이 이번 연결에서 만든 목표 방향 속도입니다.")]
    [SerializeField] private float currentGrapplePullSpeed;
    [Tooltip("현재 그래플 도착 지점까지의 거리입니다.")]
    [SerializeField] private float grappleArrivalDistance = -1f;
    [Tooltip("마지막 그래플 종료가 도착 판정으로 끝났는지 표시합니다.")]
    [SerializeField] private bool lastGrappleArrived;

    internal PlayerMotionStateId MotionState => stateMachine.CurrentId;
    internal Vector3 GravityUp => gravityTransitionActive && gravityManager != null
        ? gravityManager.PresentationUp
        : -gravityState.Direction;
    internal float MoveSpeed => moveSpeed;
    internal float CurrentMoveSpeed { get; private set; }
    internal bool IsSprinting { get; private set; }
    internal bool IsCrouching { get; private set; }
    internal bool IsGravityTransitioning => gravityTransitionActive;
    internal bool HasMoveIntent => input != null
        && input.AllowMovement
        && !gravityTransitionActive
        && input.Move.sqrMagnitude > MoveInputDeadZone * MoveInputDeadZone;
    internal bool LastZeroGravityRecoilApplied => lastZeroGravityRecoilApplied;
    internal bool IsGrapplePullActive => grapplePullActive;
    internal bool LastGrappleArrived => lastGrappleArrived;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        input ??= GetComponent<PlayerInput>();
        ResolveSceneReferences();
        body.useGravity = false;
        stateMachine = new PlayerMotionStateMachine();
        standingCapsuleHeight = capsule.height;
        standingCapsuleCenter = capsule.center;
        CurrentMoveSpeed = moveSpeed;
    }

    private void OnEnable()
    {
        if (gravityManager != null)
        {
            gravityManager.TransitionStarted += BeginGravityTransition;
            gravityManager.TransitionCompleted += EndGravityTransition;
        }
    }

    private void Start()
    {
        if (input != null && cameraTransform != null && gravityState != null && visualRoot != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(PlayerController)} on '{name}' requires Input, Camera Transform, Gravity State, and Visual Root references.",
            this);
        enabled = false;
    }
    private void OnDisable()
    {
        if (gravityManager != null)
        {
            gravityManager.TransitionStarted -= BeginGravityTransition;
            gravityManager.TransitionCompleted -= EndGravityTransition;
        }

        EndGravityTransition();
        ClearBufferedJump();
        IsSprinting = false;
        SetCrouching(false);
    }

    private void LateUpdate()
    {
        Vector3 up = GravityUp;
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

    /// <summary>
    /// 테스트 씬에서 플레이어 프리팹만 배치해도 실행될 수 있도록 비어 있는 씬 참조를 자동으로 찾습니다.
    /// Inspector에 이미 연결된 값은 덮어쓰지 않습니다.
    /// </summary>
    private void ResolveSceneReferences()
    {
        if (gravityState == null)
        {
            gravityState = FindFirstObjectByType<GravityState>();
        }

        if (gravityManager == null)
        {
            gravityManager = FindFirstObjectByType<GravityManager>();
        }

        if (cameraTransform != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            return;
        }

        ThirdPersonCameraController thirdPersonCamera = FindFirstObjectByType<ThirdPersonCameraController>();
        if (thirdPersonCamera != null)
        {
            cameraTransform = thirdPersonCamera.transform;
        }
    }

    private void FixedUpdate()
    {
        if (gravityTransitionActive)
        {
            MaintainGravityTransition();
            UpdateGroundingRuntimeState(false, Vector3.zero, Vector3.zero, -1f, 0f, false, false);
            UpdateZeroGravityRecoilRuntimeState();
            return;
        }

        Vector3 gravityDirection = gravityState.Direction;
        Vector3 up = -gravityDirection;
        bool hasGravity = gravityState.Strength > 0f;
        float verticalSpeedBeforeMotion = Vector3.Dot(body.linearVelocity, up);
        bool groundSnapActive = false;
        bool isGrounded = TryGetGroundSupport(
            gravityDirection,
            up,
            groundProbeDistance,
            out Vector3 groundNormal,
            out float groundDistance);

        if (!isGrounded
            && CanSnapToGround(hasGravity, verticalSpeedBeforeMotion)
            && TryGetGroundSupport(
                gravityDirection,
                up,
                groundSnapDistance,
                out groundNormal,
                out groundDistance))
        {
            isGrounded = true;
            groundSnapActive = true;
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
        if (cameraForward.sqrMagnitude < Mathf.Epsilon)
        {
            cameraForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        }

        Vector3 cameraRight = Vector3.Cross(up, cameraForward).normalized;
        Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        UpdateStance(isGrounded, hasGravity, up);
        UpdateSprint(isGrounded, hasGravity, moveInput);
        bool jumpRequested = UpdateBufferedJump(hasGravity);

        PlayerFixedContext context = new PlayerFixedContext(
            gravityDirection,
            up,
            groundNormal,
            moveDirection,
            hasGravity,
            isGrounded,
            jumpRequested,
            verticalSpeedBeforeMotion);

        bool jumpExecuted = stateMachine.FixedTick(this, context);
        if (jumpExecuted)
        {
            ClearBufferedJump();
        }

        if (groundSnapActive && !jumpExecuted)
        {
            ApplyGroundSnap(gravityDirection, groundDistance);
        }

        ApplyGrapplePull();

        UpdateGroundingRuntimeState(
            isGrounded,
            groundNormal,
            up,
            groundDistance,
            verticalSpeedBeforeMotion,
            jumpExecuted,
            groundSnapActive && !jumpExecuted);
        AlignWithGravity(up);
        UpdateZeroGravityRecoilRuntimeState();
    }

    private bool CanSnapToGround(bool hasGravity, float verticalSpeedBeforeMotion)
    {
        return hasGravity
            && stateMachine.CurrentId == PlayerMotionStateId.Grounded
            && groundSnapDistance > groundProbeDistance
            && maxGroundSnapSpeed > 0f
            && verticalSpeedBeforeMotion <= maxGroundSnapUpwardSpeed;
    }

    private void ApplyGroundSnap(Vector3 gravityDirection, float groundDistance)
    {
        if (groundDistance <= 0f || Time.fixedDeltaTime <= 0f)
        {
            return;
        }

        float requiredSnapSpeed = groundDistance / Time.fixedDeltaTime;
        float snapSpeed = Mathf.Min(requiredSnapSpeed, maxGroundSnapSpeed);
        float currentGravitySpeed = Vector3.Dot(body.linearVelocity, gravityDirection);
        float additionalSnapSpeed = Mathf.Max(0f, snapSpeed - currentGravitySpeed);
        body.linearVelocity += gravityDirection * additionalSnapSpeed;
    }

    private void UpdateGroundingRuntimeState(
        bool isGrounded,
        Vector3 groundNormal,
        Vector3 up,
        float groundDistance,
        float verticalSpeedBeforeMotion,
        bool jumpExecuted,
        bool groundSnapActive)
    {
        runtimeGrounded = isGrounded;
        runtimeGroundAngle = isGrounded ? Vector3.Angle(groundNormal, up) : 0f;
        runtimeGroundDistance = isGrounded ? groundDistance : -1f;
        runtimeVerticalSpeedBeforeMotion = verticalSpeedBeforeMotion;
        runtimeJumpExecuted = jumpExecuted;
        runtimeGroundSnapActive = groundSnapActive;
    }

    public bool TryApplyZeroGravityRecoil(Vector3 recoilDirection)
    {
        lastZeroGravityRecoilApplied = false;
        UpdateZeroGravityRecoilRuntimeState();

        if (!enableZeroGravityRecoil
            || body == null
            || stateMachine is not { CurrentId: PlayerMotionStateId.ZeroGravity }
            || gravityTransitionActive
            || !IsFinite(recoilDirection)
            || recoilDirection.sqrMagnitude <= Mathf.Epsilon
            || !IsFinite(zeroGravityRecoilVelocityChange)
            || zeroGravityRecoilVelocityChange <= 0f
            || !IsFinite(maxZeroGravityRecoilSpeed)
            || maxZeroGravityRecoilSpeed <= 0f)
        {
            return false;
        }

        Vector3 currentVelocity = body.linearVelocity;
        if (!IsFinite(currentVelocity))
        {
            return false;
        }

        float currentSpeed = currentVelocity.magnitude;
        Vector3 requestedDelta = recoilDirection.normalized * zeroGravityRecoilVelocityChange;
        Vector3 candidateVelocity = currentVelocity + requestedDelta;
        if (!IsFinite(candidateVelocity))
        {
            return false;
        }

        Vector3 resolvedVelocity;
        if (currentSpeed <= maxZeroGravityRecoilSpeed)
        {
            resolvedVelocity = Vector3.ClampMagnitude(candidateVelocity, maxZeroGravityRecoilSpeed);
        }
        else if (candidateVelocity.magnitude <= currentSpeed)
        {
            resolvedVelocity = candidateVelocity;
        }
        else
        {
            return false;
        }

        Vector3 appliedDelta = resolvedVelocity - currentVelocity;
        if (appliedDelta.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        body.AddForce(appliedDelta, ForceMode.VelocityChange);
        lastZeroGravityRecoilApplied = true;
        currentZeroGravityRecoilSpeed = resolvedVelocity.magnitude;
        zeroGravityRecoilSpeedLimitReached =
            currentZeroGravityRecoilSpeed >= maxZeroGravityRecoilSpeed;
        return true;
    }

    private void UpdateZeroGravityRecoilRuntimeState()
    {
        if (body == null || !IsFinite(body.linearVelocity))
        {
            currentZeroGravityRecoilSpeed = 0f;
            zeroGravityRecoilSpeedLimitReached = false;
            return;
        }

        currentZeroGravityRecoilSpeed = body.linearVelocity.magnitude;
        zeroGravityRecoilSpeedLimitReached = IsFinite(maxZeroGravityRecoilSpeed)
            && maxZeroGravityRecoilSpeed > 0f
            && currentZeroGravityRecoilSpeed >= maxZeroGravityRecoilSpeed;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void BeginGravityTransition()
    {
        if (gravityTransitionActive || body == null)
        {
            return;
        }

        CancelGrapplePull();
        gravityTransitionActive = true;
        transitionAnchorPosition = body.position;
        constraintsBeforeGravityTransition = body.constraints;
        body.constraints = constraintsBeforeGravityTransition | RigidbodyConstraints.FreezePosition;
        ownsTransitionPositionLock = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        ClearBufferedJump();
        IsSprinting = false;
        CurrentMoveSpeed = IsCrouching ? crouchSpeed : moveSpeed;
    }

    private void MaintainGravityTransition()
    {
        input.TryConsumeJumpPressed(out _);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        AlignBodyUp(gravityManager.PresentationUp, useMoveRotation: true);
    }

    private void EndGravityTransition()
    {
        if (!gravityTransitionActive || body == null)
        {
            return;
        }

        Vector3 finalUp = gravityManager != null
            ? gravityManager.PresentationUp
            : -gravityState.Direction;

        AlignBodyUp(finalUp, useMoveRotation: false);
        body.position = transitionAnchorPosition;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        if (ownsTransitionPositionLock)
        {
            body.constraints = constraintsBeforeGravityTransition;
            ownsTransitionPositionLock = false;
        }

        gravityTransitionActive = false;
    }

    private void AlignBodyUp(Vector3 up, bool useMoveRotation)
    {
        Vector3 normalizedUp = up.normalized;
        Vector3 currentUp = body.rotation * Vector3.up;
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, normalizedUp) * body.rotation;

        if (useMoveRotation)
        {
            body.MoveRotation(targetRotation);
            return;
        }

        body.rotation = targetRotation;
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

    internal void ApplyGroundedMotion(PlayerFixedContext context)
    {
        Vector3 moveDirection = context.MoveDirection;
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, context.GroundNormal).normalized;
        }

        body.linearVelocity = moveDirection * CurrentMoveSpeed;
        body.AddForce(-context.GroundNormal * gravityState.Strength, ForceMode.Acceleration);
    }

    internal void ApplyAirborneMotion(PlayerFixedContext context)
    {
        Vector3 gravityVelocity = Vector3.Project(body.linearVelocity, context.GravityDirection);
        body.linearVelocity = context.MoveDirection * moveSpeed + gravityVelocity;
        body.AddForce(gravityState.Gravity, ForceMode.Acceleration);
    }

    internal void EnterZeroGravity()
    {
        ClearBufferedJump();
        IsSprinting = false;
        CurrentMoveSpeed = moveSpeed;

        if (IsCrouching && CanUseStandingCapsule(GravityUp))
        {
            SetCrouching(false);
        }
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
                $"{nameof(PlayerController)} on '{name}' filled its stance clearance buffer.",
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

    internal void ApplyJump(PlayerFixedContext context)
    {
        Vector3 moveDirection = context.MoveDirection;
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, context.GroundNormal).normalized;
        }

        body.linearVelocity = moveDirection * moveSpeed + context.Up * jumpSpeed;
        body.AddForce(gravityState.Gravity, ForceMode.Acceleration);
    }

    internal bool TryBeginGrapplePull(Vector3 anchorPoint, Vector3 surfaceNormal)
    {
        if (body == null
            || body.isKinematic
            || gravityTransitionActive
            || !IsFinite(anchorPoint)
            || !IsFinite(surfaceNormal)
            || surfaceNormal.sqrMagnitude < Mathf.Epsilon)
        {
            return false;
        }

        grappleAnchorPoint = anchorPoint;
        grappleSurfaceNormal = surfaceNormal.normalized;
        grapplePullSpeed = 0f;
        currentGrapplePullSpeed = 0f;
        grappleArrivalDistance = Vector3.Distance(body.position, GetGrappleArrivalPoint());
        lastGrappleArrived = false;
        grapplePullActive = true;
        return true;
    }

    internal void CancelGrapplePull()
    {
        grapplePullActive = false;
        grapplePullSpeed = 0f;
        currentGrapplePullSpeed = 0f;
        grappleArrivalDistance = -1f;
    }

    private void ApplyGrapplePull()
    {
        if (!grapplePullActive || body == null)
        {
            return;
        }

        Vector3 arrivalPoint = GetGrappleArrivalPoint();
        Vector3 toArrival = arrivalPoint - body.position;
        float distance = toArrival.magnitude;
        grappleArrivalDistance = distance;

        if (!IsFinite(toArrival) || distance <= Mathf.Epsilon)
        {
            CompleteGrapplePull();
            return;
        }

        grapplePullSpeed = Mathf.Min(
            maxGrapplePullSpeed,
            grapplePullSpeed + grapplePullAcceleration * Time.fixedDeltaTime);
        Vector3 direction = toArrival / distance;
        float safeStepSpeed = Time.fixedDeltaTime > Mathf.Epsilon
            ? distance / Time.fixedDeltaTime
            : grapplePullSpeed;
        float requestedSpeed = Mathf.Min(grapplePullSpeed, safeStepSpeed);
        Vector3 currentVelocity = body.linearVelocity;
        float currentTargetSpeed = Vector3.Dot(currentVelocity, direction);
        Vector3 perpendicularVelocity = currentVelocity - direction * currentTargetSpeed;
        float resolvedTargetSpeed = Mathf.Max(currentTargetSpeed, requestedSpeed);
        body.linearVelocity = perpendicularVelocity + direction * resolvedTargetSpeed;
        currentGrapplePullSpeed = requestedSpeed;

        if (distance <= grappleStopDistance * 0.01f)
        {
            CompleteGrapplePull();
        }
    }

    private Vector3 GetGrappleArrivalPoint()
    {
        return grappleAnchorPoint + grappleSurfaceNormal * grappleStopDistance;
    }

    private void CompleteGrapplePull()
    {
        float inwardSpeed = Vector3.Dot(body.linearVelocity, grappleSurfaceNormal);
        if (inwardSpeed < 0f)
        {
            body.linearVelocity -= grappleSurfaceNormal * inwardSpeed;
        }

        lastGrappleArrived = true;
        CancelGrapplePull();
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

    private bool TryGetGroundSupport(
        Vector3 gravityDirection,
        Vector3 up,
        float probeDistance,
        out Vector3 groundNormal,
        out float groundDistance)
    {
        Vector3 scale = transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = capsule.radius * radiusScale * 0.9f;
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
        float capsuleBottomDistance = halfHeight - radius;
        float castDistance = capsuleBottomDistance + probeDistance;
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
        groundDistance = -1f;

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

        if (float.IsPositiveInfinity(nearestDistance))
        {
            return false;
        }

        groundDistance = Mathf.Max(0f, nearestDistance - capsuleBottomDistance);
        return true;
    }
}
