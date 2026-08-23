using UnityEngine;

/// <summary>
/// Normal Zone 지네처럼 바닥 한 면에서만 플레이어를 추적하는 이동 컴포넌트입니다.
/// waypoint 없이 현재 중력 기준 바닥 위에서 로컬 추적만 수행하는 첫 전투 MVP용입니다.
/// </summary>
public sealed class CentipedeFloorMover : MonsterNavTargetMover
{
    private enum MoveDebugStatus
    {
        None,
        NoNavTarget,
        NoStateMachine,
        Dead,
        NoTarget,
        AttackState,
        ReachedStoppingDistance,
        Forward,
        AvoidRight,
        AvoidLeft,
        Blocked
    }

    [Header("Obstacle Probe")]
    [Tooltip("NavTarget 앞을 검사할 장애물 레이어입니다. 보통 Obstacle 레이어만 넣습니다.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("전방 검사에 사용할 가상 구의 반지름입니다. 지네 몸 두께에 가깝게 맞추면 장애물을 덜 파고듭니다.")]
    [SerializeField, Min(0.01f)] private float obstacleProbeRadius = 0.35f;
    [Tooltip("NavTarget 앞을 얼마나 멀리 미리 검사할지 정합니다. 너무 짧으면 늦게 피하고, 너무 길면 일찍 멈춥니다.")]
    [SerializeField, Min(0.01f)] private float obstacleProbeDistance = 0.8f;
    [Tooltip("검사 시작점을 바닥에서 살짝 띄우는 값입니다. 바닥 콜라이더를 장애물로 잘못 잡는 상황을 줄입니다.")]
    [SerializeField, Min(0f)] private float obstacleProbeSurfaceOffset = 0.25f;
    [Tooltip("직진이 막혔을 때 좌우 방향을 짧게 검사해서 가능한 쪽으로 우회합니다.")]
    [SerializeField] private bool trySimpleAvoidance = true;
    [Tooltip("우회 검사 시 직진 방향에서 좌우로 틀어볼 각도입니다.")]
    [SerializeField, Range(5f, 85f)] private float avoidanceAngle = 45f;
    [Tooltip("씬에서 지네를 선택했을 때 전방 장애물 검사 범위를 표시합니다. 초록은 통과, 빨강은 막힘입니다.")]
    [SerializeField] private bool drawObstacleProbe = true;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 이동이 멈췄거나 진행 중인 이유입니다. 런타임 확인용입니다.")]
    private MoveDebugStatus moveDebugStatus;
    [SerializeField, Tooltip("NavTarget 월드 좌표입니다. 자식 로컬 좌표와 헷갈릴 때 확인합니다.")]
    private Vector3 navTargetWorldPosition;
    [SerializeField, Tooltip("현재 추적 타겟의 월드 좌표입니다.")]
    private Vector3 targetWorldPosition;
    [SerializeField, Tooltip("NavTarget 기준 타겟까지의 거리입니다.")]
    private float navTargetDistanceToTarget = -1f;
    [SerializeField, Tooltip("직진 방향 SphereCast가 Obstacle에 막혔는지 표시합니다.")]
    private bool isForwardBlocked;

    private Vector3 lastProbeOrigin;
    private Vector3 lastProbeDirection;
    private Vector3 lastMoveDirection;
    private bool lastProbeBlocked;

    protected override void ResolveSceneReferences()
    {
        base.ResolveSceneReferences();

        Transform centipedeNavTarget = transform.Find("Nav Target");
        if (centipedeNavTarget != null)
        {
            navTarget = centipedeNavTarget;
        }

        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Obstacle");
        }
    }

    private void FixedUpdate()
    {
        if (stateMachine == null)
        {
            moveDebugStatus = MoveDebugStatus.NoStateMachine;
            return;
        }

        if (stateMachine.State == MonsterState.Dead)
        {
            moveDebugStatus = MoveDebugStatus.Dead;
            return;
        }

        Transform target = stateMachine.Target;
        if (target == null)
        {
            moveDebugStatus = MoveDebugStatus.NoTarget;
            return;
        }

        if (stateMachine.State == MonsterState.Attack)
        {
            moveDebugStatus = MoveDebugStatus.AttackState;
            return;
        }

        Vector3 floorNormal = gravityState != null ? -gravityState.Direction : Vector3.up;
        MoveNavTargetPositionOnly(target.position, floorNormal, Time.fixedDeltaTime);
    }

    /// <summary>
    /// 지네 prefab의 Nav Target은 기존 Follow/Worm 체인이 따라가는 선행 목표점입니다.
    /// Spider처럼 root 회전까지 제어하면 다리 기준축이 흔들릴 수 있으므로 위치만 이동합니다.
    /// </summary>
    private void MoveNavTargetPositionOnly(Vector3 destination, Vector3 floorNormal, float deltaTime)
    {
        if (navTarget == null)
        {
            moveDebugStatus = MoveDebugStatus.NoNavTarget;
            return;
        }

        navTargetWorldPosition = navTarget.position;
        targetWorldPosition = destination;
        navTargetDistanceToTarget = Vector3.Distance(navTarget.position, destination);

        Vector3 normal = floorNormal.sqrMagnitude < Mathf.Epsilon
            ? Vector3.up
            : floorNormal.normalized;
        Vector3 toDestination = destination - navTarget.position;
        Vector3 moveDirection = Vector3.ProjectOnPlane(toDestination, normal);

        if (moveDirection.magnitude <= stoppingDistance)
        {
            moveDebugStatus = MoveDebugStatus.ReachedStoppingDistance;
            return;
        }

        Vector3 direction = moveDirection.normalized;
        if (!TryGetWalkDirection(direction, normal, out Vector3 walkDirection))
        {
            moveDebugStatus = MoveDebugStatus.Blocked;
            return;
        }

        navTarget.position += walkDirection * moveSpeed * deltaTime;
    }

    /// <summary>
    /// 진행 방향 앞에 Obstacle이 있으면 직진을 막고, 설정에 따라 좌우 우회 방향을 짧게 검사합니다.
    /// </summary>
    private bool TryGetWalkDirection(Vector3 desiredDirection, Vector3 floorNormal, out Vector3 walkDirection)
    {
        walkDirection = desiredDirection;
        lastMoveDirection = desiredDirection;

        isForwardBlocked = obstacleMask.value != 0 && IsDirectionBlocked(desiredDirection, floorNormal);
        if (!isForwardBlocked)
        {
            lastProbeBlocked = false;
            moveDebugStatus = MoveDebugStatus.Forward;
            return true;
        }

        lastProbeBlocked = true;
        if (!trySimpleAvoidance)
        {
            return false;
        }

        Vector3 rightDirection = Quaternion.AngleAxis(avoidanceAngle, floorNormal) * desiredDirection;
        if (!IsDirectionBlocked(rightDirection.normalized, floorNormal))
        {
            walkDirection = rightDirection.normalized;
            lastMoveDirection = walkDirection;
            moveDebugStatus = MoveDebugStatus.AvoidRight;
            return true;
        }

        Vector3 leftDirection = Quaternion.AngleAxis(-avoidanceAngle, floorNormal) * desiredDirection;
        if (!IsDirectionBlocked(leftDirection.normalized, floorNormal))
        {
            walkDirection = leftDirection.normalized;
            lastMoveDirection = walkDirection;
            moveDebugStatus = MoveDebugStatus.AvoidLeft;
            return true;
        }

        return false;
    }

    /// <summary>
    /// NavTarget 앞쪽으로 가상의 구를 굴려서 장애물 레이어와 닿는지 확인합니다.
    /// </summary>
    private bool IsDirectionBlocked(Vector3 direction, Vector3 floorNormal)
    {
        Vector3 origin = navTarget.position + floorNormal * obstacleProbeSurfaceOffset;
        lastProbeOrigin = origin;
        lastProbeDirection = direction;

        return Physics.SphereCast(
            origin,
            obstacleProbeRadius,
            direction,
            out _,
            obstacleProbeDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawObstacleProbe || navTarget == null)
        {
            return;
        }

        Gizmos.color = lastProbeBlocked ? Color.red : Color.green;
        Vector3 origin = Application.isPlaying ? lastProbeOrigin : navTarget.position + Vector3.up * obstacleProbeSurfaceOffset;
        Vector3 direction = Application.isPlaying && lastProbeDirection.sqrMagnitude > 0.0001f
            ? lastProbeDirection
            : navTarget.forward;

        Gizmos.DrawWireSphere(origin, obstacleProbeRadius);
        Gizmos.DrawLine(origin, origin + direction.normalized * obstacleProbeDistance);
        Gizmos.DrawWireSphere(origin + direction.normalized * obstacleProbeDistance, obstacleProbeRadius);

        Gizmos.color = Color.yellow;
        if (Application.isPlaying && lastMoveDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.DrawLine(navTarget.position, navTarget.position + lastMoveDirection.normalized);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        obstacleProbeRadius = Mathf.Max(0.01f, obstacleProbeRadius);
        obstacleProbeDistance = Mathf.Max(0.01f, obstacleProbeDistance);
        obstacleProbeSurfaceOffset = Mathf.Max(0f, obstacleProbeSurfaceOffset);
    }
}
