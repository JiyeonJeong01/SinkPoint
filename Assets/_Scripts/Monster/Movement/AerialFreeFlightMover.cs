using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Zero Zone의 공중 몬스터처럼 NavTarget을 3D 공간에서 자유롭게 이동시키는 컴포넌트입니다.
/// 무거운 길찾기 대신 임의 목적지로 날아가며, 전방의 실제 충돌체를 가볍게 검사해 피합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AerialFreeFlightMover : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum FlightDebugStatus
    {
        Ready,
        NoNavTarget,
        Dead,
        PickingDestination,
        Flying,
        AvoidingObstacle,
        Arrived
    }

    [Header("References")]
    [SerializeField, Tooltip("이 몬스터의 몸이 따라갈 공중 이동 기준점입니다. 비워두면 NavTarget/Nav Target을 찾습니다.")]
    private Transform navTarget;
    [SerializeField, Tooltip("비워두면 이 몬스터의 시작 위치를 배회 중심으로 사용합니다.")]
    private Transform flightCenter;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterStateMachine stateMachine;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterHealth monsterHealth;
    [SerializeField, Tooltip("충돌 회피 검사를 시작할 기준점입니다. 비워두면 렌더러 바운드 중심을 사용합니다.")]
    private Transform collisionProbeOrigin;

    [Header("Movement")]
    [SerializeField, Min(0f), Tooltip("공중 이동 속도입니다.")]
    private float moveSpeed = 5f;
    [SerializeField, Min(0f), Tooltip("진행 방향을 바라보는 회전 속도입니다.")]
    private float rotationSpeed = 70f;
    [SerializeField, Min(0f), Tooltip("Inspector 회전 속도가 높아도 실제 꺾임이 이 값보다 빠르지 않게 제한합니다.")]
    private float maxTurnSpeed = 70f;
    [SerializeField, Min(0f), Tooltip("실제 이동 방향이 새 목적지 방향으로 초당 얼마나 꺾일 수 있는지 제한합니다.")]
    private float movementTurnSpeed = 55f;
    [SerializeField, Min(0f), Tooltip("배회 중심에서 이 반경 안의 목적지를 고릅니다.")]
    private float roamRadius = 12f;
    [SerializeField, Min(0f), Tooltip("새 목적지를 현재 위치에서 최소 이 거리 이상 떨어지게 잡아 잦은 꺾임을 줄입니다.")]
    private float minimumDestinationDistance = 6f;
    [SerializeField, Min(0f), Tooltip("새 목적지를 현재 전방 쪽으로 밀어주는 거리입니다. 클수록 가오리가 부드럽게 직진합니다.")]
    private float forwardDestinationBias = 8f;
    [SerializeField, Min(0f), Tooltip("목적지에 이 거리만큼 가까워지면 다음 목적지를 고릅니다.")]
    private float destinationReachDistance = 1f;
    [SerializeField, Min(0f), Tooltip("목적지에 도착하지 않아도 이 시간이 지나면 새 목적지를 고릅니다.")]
    private float repickDestinationSeconds = 4f;
    [SerializeField, Min(0f), Tooltip("목적지 높이 랜덤 범위입니다. 중심 높이 기준 위아래로 적용됩니다.")]
    private float verticalRoamRange = 5f;

    [Header("Collision Avoidance")]
    [SerializeField, FormerlySerializedAs("obstacleMask"), Tooltip("비행 중 부딪히면 안 되는 실제 콜라이더 레이어입니다. 트리거와 자기 몸 콜라이더는 코드에서 무시합니다.")]
    private LayerMask collisionAvoidanceMask;
    [SerializeField, Min(0.01f), Tooltip("전방 장애물 검사에 사용할 가상 구 반지름입니다.")]
    private float obstacleProbeRadius = 0.7f;
    [SerializeField, Min(0.01f), Tooltip("앞쪽을 얼마나 미리 검사할지 정합니다.")]
    private float obstacleProbeDistance = 2.5f;
    [SerializeField, Min(0f), Tooltip("장애물을 발견했을 때 옆으로 틀어보는 강도입니다.")]
    private float avoidanceSideStep = 2f;
    [SerializeField, Min(0f), Tooltip("충돌체 바로 앞에서 멈추기 위해 남기는 여유 거리입니다.")]
    private float collisionStopPadding = 0.2f;
    [SerializeField, Min(0f), Tooltip("이미 콜라이더와 살짝 겹친 0거리 판정을 무시해 벽에서 빠져나올 수 있게 합니다.")]
    private float overlapEscapeDistance = 0.03f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 공중 이동 상태입니다.")]
    private FlightDebugStatus flightDebugStatus = FlightDebugStatus.Ready;
    [SerializeField, Tooltip("현재 이동 목적지입니다.")]
    private Vector3 currentDestination;
    [SerializeField, Tooltip("전방 충돌체 검사 결과입니다.")]
    private bool isForwardBlocked;
    [SerializeField, Tooltip("씬에서 선택했을 때 목적지와 장애물 검사선을 표시합니다.")]
    private bool drawDebugGizmos = true;

    private Vector3 initialNavTargetPosition;
    private Quaternion initialNavTargetRotation;
    private Vector3 initialCenterPosition;
    private float nextDestinationPickTime;
    private Vector3 lastProbeOrigin;
    private Vector3 lastProbeDirection = Vector3.forward;
    private Vector3 currentMoveDirection = Vector3.forward;
    private Renderer[] bodyRenderers;

    private Vector3 CenterPosition => flightCenter != null ? flightCenter.position : initialCenterPosition;

    private void Awake()
    {
        ResolveReferences();
        ResolveDefaultLayers();
        CaptureInitialPose();
        PickNewDestination();
    }

    private void Reset()
    {
        ResolveReferences();
        ResolveDefaultLayers();
    }

    private void FixedUpdate()
    {
        if (IsDead())
        {
            flightDebugStatus = FlightDebugStatus.Dead;
            return;
        }

        if (navTarget == null)
        {
            flightDebugStatus = FlightDebugStatus.NoNavTarget;
            return;
        }

        MoveTowardDestination(Time.fixedDeltaTime);
    }

    /// <summary>
    /// 리스폰 시 시작 위치로 되돌리고 새 배회 목적지를 다시 고릅니다.
    /// </summary>
    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        if (navTarget != null)
        {
            navTarget.SetPositionAndRotation(initialNavTargetPosition, initialNavTargetRotation);
            currentMoveDirection = navTarget.forward;
        }

        flightDebugStatus = FlightDebugStatus.Ready;
        PickNewDestination();
    }

    public void OnMonsterDied()
    {
        flightDebugStatus = FlightDebugStatus.Dead;
    }

    private void ResolveReferences()
    {
        navTarget ??= FindChildRecursive(transform, "NavTarget");
        navTarget ??= FindChildRecursive(transform, "Nav Target");

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        monsterHealth ??= GetComponent<MonsterHealth>();
        monsterHealth ??= GetComponentInParent<MonsterHealth>();
        monsterHealth ??= GetComponentInChildren<MonsterHealth>();

        bodyRenderers ??= GetComponentsInChildren<Renderer>(true);
    }

    private void ResolveDefaultLayers()
    {
        int oldObstacleOnlyMask = LayerMask.GetMask("Obstacle");
        if (collisionAvoidanceMask.value == 0 || collisionAvoidanceMask.value == oldObstacleOnlyMask)
        {
            collisionAvoidanceMask = BuildDefaultCollisionAvoidanceMask();
        }

        collisionAvoidanceMask = IncludeLayerIfExists(collisionAvoidanceMask.value, "Default");
    }

    private void CaptureInitialPose()
    {
        if (navTarget != null)
        {
            initialNavTargetPosition = navTarget.position;
            initialNavTargetRotation = navTarget.rotation;
            currentMoveDirection = navTarget.forward;
        }

        initialCenterPosition = flightCenter != null ? flightCenter.position : transform.position;
    }

    private void MoveTowardDestination(float deltaTime)
    {
        if (Time.time >= nextDestinationPickTime
            || Vector3.Distance(navTarget.position, currentDestination) <= destinationReachDistance)
        {
            flightDebugStatus = FlightDebugStatus.Arrived;
            PickNewDestination();
        }

        Vector3 desiredDirection = currentDestination - navTarget.position;
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        desiredDirection.Normalize();
        Vector3 steerDirection = GetSmoothedMoveDirection(desiredDirection, deltaTime);
        Vector3 moveDirection = GetObstacleAwareDirection(steerDirection, deltaTime);
        float moveDistance = GetSafeMoveDistance(moveDirection, moveSpeed * deltaTime);

        navTarget.position += moveDirection * moveDistance;
        RotateToward(moveDirection, deltaTime);
        flightDebugStatus = isForwardBlocked ? FlightDebugStatus.AvoidingObstacle : FlightDebugStatus.Flying;
    }

    /// <summary>
    /// 전방 SphereCast가 막히면 오른쪽/왼쪽/위쪽 후보를 순서대로 검사해서 가능한 방향으로 틀어줍니다.
    /// </summary>
    private Vector3 GetSmoothedMoveDirection(Vector3 desiredDirection, float deltaTime)
    {
        if (currentMoveDirection.sqrMagnitude < 0.0001f)
        {
            currentMoveDirection = navTarget != null ? navTarget.forward : desiredDirection;
        }

        currentMoveDirection = Vector3.RotateTowards(
            currentMoveDirection.normalized,
            desiredDirection,
            movementTurnSpeed * Mathf.Deg2Rad * deltaTime,
            0f).normalized;
        return currentMoveDirection;
    }

    private Vector3 GetObstacleAwareDirection(Vector3 desiredDirection, float deltaTime)
    {
        isForwardBlocked = IsBlocked(desiredDirection);
        if (!isForwardBlocked)
        {
            currentMoveDirection = desiredDirection;
            return desiredDirection;
        }

        Vector3 right = Vector3.Cross(Vector3.up, desiredDirection);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(Vector3.right, desiredDirection);
        }

        right.Normalize();
        Vector3[] candidates =
        {
            (desiredDirection + right * avoidanceSideStep).normalized,
            (desiredDirection - right * avoidanceSideStep).normalized,
            (desiredDirection + Vector3.up * avoidanceSideStep).normalized,
            (desiredDirection - Vector3.up * avoidanceSideStep).normalized
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (!IsBlocked(candidates[i]))
            {
                currentMoveDirection = Vector3.RotateTowards(
                    currentMoveDirection.normalized,
                    candidates[i],
                    movementTurnSpeed * Mathf.Deg2Rad * deltaTime,
                    0f).normalized;
                return currentMoveDirection;
            }
        }

        currentMoveDirection = Vector3.RotateTowards(
            currentMoveDirection.normalized,
            -desiredDirection,
            movementTurnSpeed * Mathf.Deg2Rad * deltaTime,
            0f).normalized;
        return currentMoveDirection;
    }

    private bool IsBlocked(Vector3 direction)
    {
        if (collisionAvoidanceMask.value == 0 || navTarget == null)
        {
            return false;
        }

        return TryFindBlockingHit(direction, obstacleProbeDistance, out _);
    }

    private float GetSafeMoveDistance(Vector3 direction, float requestedDistance)
    {
        if (requestedDistance <= 0f)
        {
            return 0f;
        }

        float probeDistance = requestedDistance + collisionStopPadding;
        if (!TryFindBlockingHit(direction, probeDistance, out RaycastHit hit))
        {
            return requestedDistance;
        }

        isForwardBlocked = true;
        PickNewDestination();
        return Mathf.Max(0f, hit.distance - collisionStopPadding);
    }

    private bool TryFindBlockingHit(Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        nearestHit = default;
        if (collisionAvoidanceMask.value == 0 || navTarget == null || direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        lastProbeOrigin = GetProbeOrigin();
        lastProbeDirection = direction.normalized;

        Collider[] overlaps = Physics.OverlapSphere(
            lastProbeOrigin,
            obstacleProbeRadius,
            collisionAvoidanceMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlapCollider = overlaps[i];
            if (overlapCollider == null || overlapCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            Vector3 closestPoint = overlapCollider.ClosestPoint(lastProbeOrigin);
            Vector3 awayFromCollider = lastProbeOrigin - closestPoint;
            if (awayFromCollider.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            if (Vector3.Dot(lastProbeDirection, awayFromCollider.normalized) < 0f)
            {
                return true;
            }
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            lastProbeOrigin,
            obstacleProbeRadius,
            lastProbeDirection,
            distance,
            collisionAvoidanceMask,
            QueryTriggerInteraction.Ignore);

        bool foundHit = false;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            // 가오리 자신의 몸/자식 콜라이더를 장애물로 오해하면 제자리에서 튀기 때문에 제외합니다.
            if (hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            // 이미 겹친 상태의 0거리 히트까지 막으면 벽에 박힌 뒤 빠져나오지 못합니다.
            if (hits[i].distance <= overlapEscapeDistance)
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                foundHit = true;
            }
        }

        return foundHit;
    }

    private void RotateToward(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        navTarget.rotation = Quaternion.RotateTowards(
            navTarget.rotation,
            targetRotation,
            GetEffectiveTurnSpeed() * deltaTime);
    }

    private void PickNewDestination()
    {
        if (navTarget == null)
        {
            return;
        }

        Vector3 forward = navTarget.forward.sqrMagnitude > 0.0001f ? navTarget.forward.normalized : transform.forward;
        Vector2 flat = Random.insideUnitCircle * roamRadius;
        float height = Random.Range(-verticalRoamRange, verticalRoamRange);
        currentDestination = CenterPosition
            + forward * forwardDestinationBias
            + new Vector3(flat.x, height, flat.y);

        Vector3 toDestination = currentDestination - navTarget.position;
        if (toDestination.magnitude < minimumDestinationDistance && toDestination.sqrMagnitude > 0.0001f)
        {
            currentDestination = navTarget.position + toDestination.normalized * minimumDestinationDistance;
        }

        nextDestinationPickTime = Time.time + repickDestinationSeconds;
        flightDebugStatus = FlightDebugStatus.PickingDestination;
    }

    private float GetEffectiveTurnSpeed()
    {
        if (maxTurnSpeed <= 0f)
        {
            return rotationSpeed;
        }

        return Mathf.Min(rotationSpeed, maxTurnSpeed);
    }

    private bool IsDead()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return true;
        }

        return stateMachine != null && stateMachine.State == MonsterState.Dead;
    }

    private Vector3 GetProbeOrigin()
    {
        if (collisionProbeOrigin != null)
        {
            return collisionProbeOrigin.position;
        }

        return TryGetBodyBounds(out Bounds bounds) ? bounds.center : navTarget.position;
    }

    private bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = default;
        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            return false;
        }

        bool hasBounds = false;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer bodyRenderer = bodyRenderers[i];
            if (!IsBodyRenderer(bodyRenderer))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = bodyRenderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(bodyRenderer.bounds);
        }

        return hasBounds;
    }

    private static bool IsBodyRenderer(Renderer bodyRenderer)
    {
        return bodyRenderer != null
            && bodyRenderer.enabled
            && (bodyRenderer is MeshRenderer || bodyRenderer is SkinnedMeshRenderer);
    }

    private static LayerMask BuildDefaultCollisionAvoidanceMask()
    {
        int mask = Physics.DefaultRaycastLayers;
        mask = ExcludeLayerIfExists(mask, "Monster");
        mask = ExcludeLayerIfExists(mask, "MonsterAttack");
        mask = ExcludeLayerIfExists(mask, "Player");
        return mask;
    }

    private static int ExcludeLayerIfExists(int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? mask & ~(1 << layer) : mask;
    }

    private static int IncludeLayerIfExists(int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? mask | (1 << layer) : mask;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentDestination, 0.35f);

        Gizmos.color = isForwardBlocked ? Color.red : Color.green;
        Gizmos.DrawWireSphere(lastProbeOrigin, obstacleProbeRadius);
        Gizmos.DrawLine(lastProbeOrigin, lastProbeOrigin + lastProbeDirection.normalized * obstacleProbeDistance);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        maxTurnSpeed = Mathf.Max(0f, maxTurnSpeed);
        movementTurnSpeed = Mathf.Max(0f, movementTurnSpeed);
        roamRadius = Mathf.Max(0f, roamRadius);
        minimumDestinationDistance = Mathf.Max(0f, minimumDestinationDistance);
        forwardDestinationBias = Mathf.Max(0f, forwardDestinationBias);
        destinationReachDistance = Mathf.Max(0f, destinationReachDistance);
        repickDestinationSeconds = Mathf.Max(0f, repickDestinationSeconds);
        verticalRoamRange = Mathf.Max(0f, verticalRoamRange);
        obstacleProbeRadius = Mathf.Max(0.01f, obstacleProbeRadius);
        obstacleProbeDistance = Mathf.Max(0.01f, obstacleProbeDistance);
        avoidanceSideStep = Mathf.Max(0f, avoidanceSideStep);
        collisionStopPadding = Mathf.Max(0f, collisionStopPadding);
        overlapEscapeDistance = Mathf.Max(0f, overlapEscapeDistance);
    }
}
