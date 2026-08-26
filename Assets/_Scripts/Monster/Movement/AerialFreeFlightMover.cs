using UnityEngine;

/// <summary>
/// Zero Zone 공중 몬스터의 NavTarget을 수동 waypoint route를 따라 이동시킵니다.
/// 랜덤 목적지/충돌 회피 검사는 사용하지 않고, 안전한 경로는 씬에 배치한 MonsterRoute가 책임집니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AerialFreeFlightMover : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum FlightDebugStatus
    {
        Ready,
        NoNavTarget,
        NoRoute,
        EmptyRoute,
        NullWaypoint,
        Dead,
        Paused,
        MovingToWaypoint,
        ArrivedWaypoint,
        RouteComplete
    }

    [Header("References")]
    [SerializeField, Tooltip("이 몬스터의 몸이 따라갈 공중 이동 기준점입니다. 비워두면 NavTarget/Nav Target을 찾습니다.")]
    private Transform navTarget;
    [SerializeField, Tooltip("가오리가 순서대로 따라갈 안전 waypoint route입니다. 장애물 회피 대신 이 route를 믿고 이동합니다.")]
    private MonsterRoute route;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterStateMachine stateMachine;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterHealth monsterHealth;

    [Header("Movement")]
    [SerializeField, Min(0f), Tooltip("공중 이동 속도입니다.")]
    private float moveSpeed = 5f;
    [SerializeField, Min(0f), Tooltip("진행 방향을 바라보는 회전 속도입니다.")]
    private float rotationSpeed = 70f;
    [SerializeField, Min(0f), Tooltip("waypoint에 이 거리만큼 가까워지면 도착으로 봅니다.")]
    private float waypointReachDistance = 1f;
    [SerializeField, Min(0f), Tooltip("waypoint에 도착할 때마다 잠깐 멈출 시간입니다. 공격 타이밍을 만들고 싶으면 살짝 올립니다.")]
    private float pauseAtWaypointSeconds = 0.25f;
    [SerializeField, Tooltip("켜면 route가 끝났을 때 처음 waypoint로 돌아갑니다. MonsterRoute의 Loop도 함께 켜져 있어야 반복합니다.")]
    private bool loop = true;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 공중 이동 상태입니다.")]
    private FlightDebugStatus flightDebugStatus = FlightDebugStatus.Ready;
    [SerializeField, Tooltip("현재 목표 waypoint 배열 index입니다.")]
    private int currentWaypointIndex;
    [SerializeField, Tooltip("현재 목표 waypoint 참조입니다.")]
    private MonsterWaypoint currentWaypoint;
    [SerializeField, Tooltip("현재 목표 waypoint 이름입니다.")]
    private string currentWaypointName;
    [SerializeField, Tooltip("NavTarget에서 현재 waypoint까지의 실제 거리입니다.")]
    private float distanceToWaypoint = -1f;
    [SerializeField, Tooltip("씬에서 선택했을 때 현재 목표 waypoint와 이동 방향을 표시합니다.")]
    private bool drawDebugGizmos = true;

    private Vector3 initialNavTargetPosition;
    private Quaternion initialNavTargetRotation;
    private Vector3 currentMoveDirection = Vector3.forward;
    private float pauseTimer;

    private void Awake()
    {
        ResolveReferences();
        CaptureInitialPose();
        UpdateDebugWaypoint();
    }

    private void Reset()
    {
        ResolveReferences();
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

        FollowRoute(Time.fixedDeltaTime);
    }

    /// <summary>
    /// 리스폰 시 시작 위치로 되돌리고 첫 waypoint부터 다시 순회합니다.
    /// </summary>
    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        if (navTarget != null)
        {
            navTarget.SetPositionAndRotation(initialNavTargetPosition, initialNavTargetRotation);
            currentMoveDirection = navTarget.forward;
        }

        currentWaypointIndex = 0;
        pauseTimer = 0f;
        flightDebugStatus = FlightDebugStatus.Ready;
        UpdateDebugWaypoint();
    }

    public void OnMonsterDied()
    {
        flightDebugStatus = FlightDebugStatus.Dead;
    }

    /// <summary>
    /// 현재 route의 waypoint를 향해 이동하고, 도착하면 다음 waypoint로 넘깁니다.
    /// 물리 Cast를 전혀 쓰지 않으므로 안전한 위치 선정은 waypoint 배치가 담당합니다.
    /// </summary>
    private void FollowRoute(float deltaTime)
    {
        if (route == null)
        {
            flightDebugStatus = FlightDebugStatus.NoRoute;
            ClearDebugWaypoint();
            return;
        }

        if (route.Count == 0)
        {
            flightDebugStatus = FlightDebugStatus.EmptyRoute;
            ClearDebugWaypoint();
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer = Mathf.Max(0f, pauseTimer - deltaTime);
            flightDebugStatus = FlightDebugStatus.Paused;
            return;
        }

        MonsterWaypoint waypoint = route.GetWaypoint(currentWaypointIndex);
        if (waypoint == null)
        {
            flightDebugStatus = FlightDebugStatus.NullWaypoint;
            AdvanceWaypoint();
            return;
        }

        currentWaypoint = waypoint;
        currentWaypointName = waypoint.name;

        Vector3 toWaypoint = waypoint.transform.position - navTarget.position;
        distanceToWaypoint = toWaypoint.magnitude;
        if (distanceToWaypoint <= waypointReachDistance)
        {
            flightDebugStatus = FlightDebugStatus.ArrivedWaypoint;
            AdvanceWaypoint();
            return;
        }

        Vector3 previousPosition = navTarget.position;
        navTarget.position = Vector3.MoveTowards(
            navTarget.position,
            waypoint.transform.position,
            moveSpeed * deltaTime);

        // 위치는 정확히 waypoint로 보내고, 실제 이동한 방향만 사용해 몸 회전이 뒤따라오게 합니다.
        Vector3 actualMoveDirection = navTarget.position - previousPosition;
        if (actualMoveDirection.sqrMagnitude > 0.0001f)
        {
            currentMoveDirection = actualMoveDirection.normalized;
            RotateToward(currentMoveDirection, deltaTime);
        }

        flightDebugStatus = FlightDebugStatus.MovingToWaypoint;
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= route.Count)
        {
            if (loop && route.Loop)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                currentWaypointIndex = Mathf.Max(0, route.Count - 1);
                flightDebugStatus = FlightDebugStatus.RouteComplete;
            }
        }

        pauseTimer = pauseAtWaypointSeconds;
        UpdateDebugWaypoint();
    }

    private void RotateToward(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        navTarget.rotation = Quaternion.RotateTowards(navTarget.rotation, targetRotation, rotationSpeed * deltaTime);
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
    }

    private void CaptureInitialPose()
    {
        if (navTarget == null)
        {
            return;
        }

        initialNavTargetPosition = navTarget.position;
        initialNavTargetRotation = navTarget.rotation;
        currentMoveDirection = navTarget.forward;
    }

    private bool IsDead()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return true;
        }

        return stateMachine != null && stateMachine.State == MonsterState.Dead;
    }

    private void UpdateDebugWaypoint()
    {
        currentWaypoint = route != null ? route.GetWaypoint(currentWaypointIndex) : null;
        currentWaypointName = currentWaypoint != null ? currentWaypoint.name : string.Empty;
        distanceToWaypoint = currentWaypoint != null && navTarget != null
            ? Vector3.Distance(navTarget.position, currentWaypoint.transform.position)
            : -1f;
    }

    private void ClearDebugWaypoint()
    {
        currentWaypoint = null;
        currentWaypointName = string.Empty;
        distanceToWaypoint = -1f;
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
        if (!drawDebugGizmos || navTarget == null || currentWaypoint == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentWaypoint.transform.position, waypointReachDistance);
        Gizmos.DrawLine(navTarget.position, currentWaypoint.transform.position);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        waypointReachDistance = Mathf.Max(0f, waypointReachDistance);
        pauseAtWaypointSeconds = Mathf.Max(0f, pauseAtWaypointSeconds);
    }
}
