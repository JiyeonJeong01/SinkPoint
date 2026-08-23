using UnityEngine;

/// <summary>
/// Shift Zone 거미처럼 waypoint 경로와 로컬 추적을 함께 쓰는 NavTarget 이동 컴포넌트입니다.
/// 먼 거리에서는 route를 따라가고, 플레이어가 감지되면 현재 표면 위에서 직접 접근합니다.
/// </summary>
public sealed class SpiderSurfaceRouteMover : MonsterNavTargetMover
{
    private enum RouteDebugStatus
    {
        None,
        NoRoute,
        EmptyRoute,
        Dead,
        Falling,
        ChaseTarget,
        AttackTarget,
        RoutePause,
        NullWaypoint,
        SurfaceTransition,
        MovingToWaypoint,
        ArrivedWaypoint
    }

    [Header("Route")]
    [SerializeField, Tooltip("기존 단일 route입니다. Routes가 비어 있으면 이 route를 사용합니다.")]
    private MonsterRoute route;
    [SerializeField, Tooltip("거미가 순서대로 사용할 route 목록입니다. 비어 있으면 위의 단일 Route만 사용합니다.")]
    private MonsterRoute[] routes;
    [SerializeField, Tooltip("켜면 활성화될 때마다 다음 route index를 골라 리스폰마다 다른 route처럼 보이게 합니다.")]
    private bool advanceRouteOnEnable = true;
    [SerializeField, Tooltip("route 선택 시작값입니다. 여러 거미가 같은 순서를 피해야 할 때 약간씩 다르게 둡니다.")]
    private int routeIndexOffset;
    [SerializeField, Tooltip("route 전체를 다 돌면 다음 route로 넘어갑니다. 꺼두면 선택된 route 안에서만 반복합니다.")]
    private bool advanceToNextRouteAfterLoop = true;
    [SerializeField, Tooltip("서로 다른 면의 waypoint로 넘어갈 때 평면 투영 대신 목표점으로 직접 접근합니다. 꺾이는 벽/천장 전환 구간에서 멈춤을 줄입니다.")]
    private bool directMoveOnSurfaceChange = true;
    [SerializeField, Tooltip("현재 표면 normal과 목표 waypoint normal의 각도가 이 값보다 크면 표면 전환으로 봅니다.")]
    private float surfaceChangeAngle = 35f;

    [Header("Route Pause")]
    [SerializeField, Tooltip("route를 대략 몇 구간으로 나눠 멈출지 정합니다. 2이면 전체 waypoint 수의 1/2 지점에서 멈춥니다.")]
    private int pauseSectionsPerRoute = 2;
    [SerializeField, Tooltip("구간마다 멈춰 투사체 공격 등을 진행할 시간입니다.")]
    private float routePauseSeconds = 2f;
    [SerializeField, Tooltip("바닥/벽/천장처럼 다음 waypoint의 면 normal이 바뀌기 직전에 한 번 멈춰 다리 자세를 정리합니다.")]
    private bool pauseBeforeSurfaceChange = true;

    [Header("Combat")]
    [SerializeField, Tooltip("플레이어 감지 후 전투 이동에 사용할 중력 방향입니다. Surface normal은 이 값의 반대 방향을 사용합니다.")]
    private Vector3 combatGravityDirection = Vector3.down;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 선택된 route index입니다.")]
    private int currentRouteIndex;
    [SerializeField, Tooltip("현재 선택된 route 참조입니다.")]
    private MonsterRoute currentRoute;
    [SerializeField, Tooltip("현재 선택된 route 이름입니다.")]
    private string currentRouteName;
    [SerializeField, Tooltip("현재 route 이동 상태입니다. 런타임 확인용입니다.")]
    private RouteDebugStatus routeDebugStatus;
    [SerializeField, Tooltip("현재 목표 waypoint 배열 index입니다.")]
    private int currentWaypointIndex;
    [SerializeField, Tooltip("현재 목표 waypoint 참조입니다.")]
    private MonsterWaypoint currentWaypoint;
    [SerializeField, Tooltip("현재 목표 waypoint 이름입니다.")]
    private string currentWaypointName;
    [SerializeField, Tooltip("NavTarget에서 목표 waypoint까지의 실제 3D 거리입니다.")]
    private float rawDistanceToWaypoint = -1f;
    [SerializeField, Tooltip("현재 surface normal 평면 위로 투영한 이동 거리입니다. 이 값이 작으면 목표가 옆/뒤로 있는 것처럼 보일 수 있습니다.")]
    private float projectedDistanceToWaypoint = -1f;
    [SerializeField, Tooltip("현재 목표 waypoint의 월드 surface normal입니다.")]
    private Vector3 currentWaypointSurfaceNormal = Vector3.up;
    [SerializeField, Tooltip("현재 waypoint가 다른 면으로 넘어가는 전환 지점인지 표시합니다.")]
    private bool isSurfaceTransition;
    [SerializeField, Tooltip("route 구간 정지 중 남은 시간입니다.")]
    private float routePauseRemaining;
    [SerializeField, Tooltip("씬에서 선택했을 때 현재 목표 waypoint와 투영 이동 방향을 표시합니다.")]
    private bool drawRouteDebug = true;

    private static int nextRouteIndex;

    private int waypointIndex;
    private int routeStepCounter;
    private float routePauseTimer;
    private bool pausedBeforeCurrentWaypoint;
    private Vector3 lastProjectedMoveDirection;

    private int RouteCount => routes != null && routes.Length > 0 ? routes.Length : route != null ? 1 : 0;

    protected override void Awake()
    {
        base.Awake();
        if (!Application.isPlaying)
        {
            SelectInitialRoute();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            SelectInitialRoute();
        }
    }

    private void FixedUpdate()
    {
        if (stateMachine != null && stateMachine.State == MonsterState.Dead)
        {
            routeDebugStatus = RouteDebugStatus.Dead;
            return;
        }

        if (stateMachine != null && stateMachine.State == MonsterState.Falling)
        {
            routeDebugStatus = RouteDebugStatus.Falling;
            return;
        }

        if (stateMachine != null && stateMachine.State == MonsterState.Chase && stateMachine.Target != null)
        {
            routeDebugStatus = RouteDebugStatus.ChaseTarget;
            ClearRouteDebugTarget();
            MoveNavTargetToward(stateMachine.Target.position, GetCombatSurfaceNormal(), Time.fixedDeltaTime);
            return;
        }

        if (stateMachine != null && stateMachine.State == MonsterState.Attack && stateMachine.Target != null)
        {
            routeDebugStatus = RouteDebugStatus.AttackTarget;
            ClearRouteDebugTarget();
            Vector3 lookDirection = navTarget != null
                ? stateMachine.Target.position - navTarget.position
                : stateMachine.Target.position - transform.position;
            AlignNavTarget(GetCombatSurfaceNormal(), Time.fixedDeltaTime, lookDirection);
            return;
        }

        FollowRoute();
    }

    /// <summary>
    /// 현재 route waypoint를 향해 이동하고, 도착하면 다음 waypoint로 넘깁니다.
    /// waypoint의 SurfaceNormal을 사용해 NavTarget.up을 벽/천장 방향으로 맞춥니다.
    /// </summary>
    private void FollowRoute()
    {
        MonsterRoute activeRoute = GetActiveRoute();
        if (activeRoute == null)
        {
            routeDebugStatus = RouteDebugStatus.NoRoute;
            ClearRouteDebugTarget();
            return;
        }

        currentRoute = activeRoute;
        currentRouteName = activeRoute.name;

        if (routePauseTimer > 0f)
        {
            routePauseTimer = Mathf.Max(0f, routePauseTimer - Time.fixedDeltaTime);
            routePauseRemaining = routePauseTimer;
            routeDebugStatus = RouteDebugStatus.RoutePause;
            AlignNavTarget(currentSurfaceNormal, Time.fixedDeltaTime);
            return;
        }

        if (activeRoute.Count == 0)
        {
            routeDebugStatus = RouteDebugStatus.EmptyRoute;
            ClearRouteDebugTarget();
            return;
        }

        MonsterWaypoint waypoint = activeRoute.GetWaypoint(waypointIndex);
        if (waypoint == null)
        {
            routeDebugStatus = RouteDebugStatus.NullWaypoint;
            ClearRouteDebugTarget();
            AdvanceWaypoint();
            return;
        }

        UpdateRouteDebugTarget(waypoint);
        if (ShouldPauseBeforeSurfaceTransition())
        {
            pausedBeforeCurrentWaypoint = true;
            BeginRoutePause();
            routeDebugStatus = RouteDebugStatus.RoutePause;
            return;
        }

        bool arrived = isSurfaceTransition
            ? MoveNavTargetDirectlyToward(waypoint.transform.position, waypoint.SurfaceNormal, Time.fixedDeltaTime)
            : MoveNavTargetToward(waypoint.transform.position, waypoint.SurfaceNormal, Time.fixedDeltaTime);
        if (arrived)
        {
            routeDebugStatus = RouteDebugStatus.ArrivedWaypoint;
            AdvanceWaypoint();
            return;
        }

        routeDebugStatus = isSurfaceTransition
            ? RouteDebugStatus.SurfaceTransition
            : RouteDebugStatus.MovingToWaypoint;
    }

    private void AdvanceWaypoint()
    {
        MonsterRoute activeRoute = GetActiveRoute();
        if (activeRoute == null)
        {
            return;
        }

        routeStepCounter++;
        waypointIndex++;
        pausedBeforeCurrentWaypoint = false;
        if (waypointIndex >= activeRoute.Count)
        {
            AdvanceRoute(activeRoute);
            return;
        }

        if (ShouldPauseAfterWaypoint(activeRoute))
        {
            BeginRoutePause();
        }
    }

    private void SelectInitialRoute()
    {
        int routeCount = RouteCount;
        if (routeCount <= 0)
        {
            currentRouteIndex = 0;
            currentRoute = null;
            currentRouteName = string.Empty;
            waypointIndex = 0;
            routeStepCounter = 0;
            return;
        }

        currentRouteIndex = advanceRouteOnEnable
            ? Mod(routeIndexOffset + nextRouteIndex++, routeCount)
            : Mod(routeIndexOffset, routeCount);
        currentRoute = GetActiveRoute();
        currentRouteName = currentRoute != null ? currentRoute.name : string.Empty;
        waypointIndex = 0;
        routeStepCounter = 0;
        routePauseTimer = 0f;
        routePauseRemaining = 0f;
        pausedBeforeCurrentWaypoint = false;
    }

    private MonsterRoute GetActiveRoute()
    {
        if (routes != null && routes.Length > 0)
        {
            currentRouteIndex = Mod(currentRouteIndex, routes.Length);
            return routes[currentRouteIndex];
        }

        currentRouteIndex = route != null ? 0 : -1;
        return route;
    }

    private void AdvanceRoute(MonsterRoute completedRoute)
    {
        int routeCount = RouteCount;
        if (routeCount <= 0)
        {
            waypointIndex = 0;
            routeStepCounter = 0;
            return;
        }

        if (routes != null && routes.Length > 0 && advanceToNextRouteAfterLoop)
        {
            currentRouteIndex = Mod(currentRouteIndex + 1, routes.Length);
            waypointIndex = 0;
            routeStepCounter = 0;
            pausedBeforeCurrentWaypoint = false;
            BeginRoutePause();
            return;
        }

        waypointIndex = completedRoute.Loop ? 0 : Mathf.Max(0, completedRoute.Count - 1);
        routeStepCounter = 0;
        pausedBeforeCurrentWaypoint = false;
        if (completedRoute.Loop)
        {
            BeginRoutePause();
        }
    }

    private bool ShouldPauseAfterWaypoint(MonsterRoute activeRoute)
    {
        if (routePauseSeconds <= 0f || pauseSectionsPerRoute <= 0 || activeRoute.Count <= 1)
        {
            return false;
        }

        int pauseInterval = Mathf.Max(1, Mathf.RoundToInt(activeRoute.Count / (float)pauseSectionsPerRoute));
        return routeStepCounter > 0 && routeStepCounter % pauseInterval == 0;
    }

    private bool ShouldPauseBeforeSurfaceTransition()
    {
        return pauseBeforeSurfaceChange
            && !pausedBeforeCurrentWaypoint
            && isSurfaceTransition
            && routePauseSeconds > 0f;
    }

    private void BeginRoutePause()
    {
        routePauseTimer = Mathf.Max(0f, routePauseSeconds);
        routePauseRemaining = routePauseTimer;
    }

    private void UpdateRouteDebugTarget(MonsterWaypoint waypoint)
    {
        currentWaypointIndex = waypointIndex;
        currentRoute = GetActiveRoute();
        currentRouteName = currentRoute != null ? currentRoute.name : string.Empty;
        currentWaypoint = waypoint;
        currentWaypointName = waypoint.name;
        currentWaypointSurfaceNormal = waypoint.SurfaceNormal;

        if (navTarget == null)
        {
            rawDistanceToWaypoint = -1f;
            projectedDistanceToWaypoint = -1f;
            lastProjectedMoveDirection = Vector3.zero;
            return;
        }

        Vector3 toWaypoint = waypoint.transform.position - navTarget.position;
        Vector3 normal = currentWaypointSurfaceNormal.sqrMagnitude < Mathf.Epsilon
            ? currentSurfaceNormal
            : currentWaypointSurfaceNormal.normalized;
        Vector3 projected = Vector3.ProjectOnPlane(toWaypoint, normal);

        rawDistanceToWaypoint = toWaypoint.magnitude;
        projectedDistanceToWaypoint = projected.magnitude;
        lastProjectedMoveDirection = projected.sqrMagnitude > 0.0001f ? projected.normalized : Vector3.zero;
        isSurfaceTransition = directMoveOnSurfaceChange
            && Vector3.Angle(currentSurfaceNormal, normal) >= surfaceChangeAngle;
    }

    private void ClearRouteDebugTarget()
    {
        currentWaypointIndex = waypointIndex;
        currentWaypoint = null;
        currentWaypointName = string.Empty;
        rawDistanceToWaypoint = -1f;
        projectedDistanceToWaypoint = -1f;
        currentWaypointSurfaceNormal = currentSurfaceNormal;
        isSurfaceTransition = false;
        lastProjectedMoveDirection = Vector3.zero;
    }

    private Vector3 GetCombatSurfaceNormal()
    {
        Vector3 gravityDirection = combatGravityDirection.sqrMagnitude < Mathf.Epsilon
            ? Vector3.down
            : combatGravityDirection.normalized;
        return -gravityDirection;
    }

    private static int Mod(int value, int length)
    {
        if (length <= 0)
        {
            return 0;
        }

        int result = value % length;
        return result < 0 ? result + length : result;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRouteDebug || navTarget == null || currentWaypoint == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(navTarget.position, currentWaypoint.transform.position);
        Gizmos.DrawWireSphere(currentWaypoint.transform.position, 0.35f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(currentWaypoint.transform.position, currentWaypointSurfaceNormal.normalized);

        if (lastProjectedMoveDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(navTarget.position, lastProjectedMoveDirection);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        surfaceChangeAngle = Mathf.Clamp(surfaceChangeAngle, 0f, 180f);
        routeIndexOffset = Mathf.Max(0, routeIndexOffset);
        pauseSectionsPerRoute = Mathf.Max(0, pauseSectionsPerRoute);
        routePauseSeconds = Mathf.Max(0f, routePauseSeconds);
        if (combatGravityDirection.sqrMagnitude < Mathf.Epsilon)
        {
            combatGravityDirection = Vector3.down;
        }
    }
}
