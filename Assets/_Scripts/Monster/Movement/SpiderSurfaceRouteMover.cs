using UnityEngine;

/// <summary>
/// Shift Zone 거미처럼 waypoint 경로와 로컬 추적을 함께 쓰는 NavTarget 이동 컴포넌트입니다.
/// 먼 거리에서는 route를 따라가고, 플레이어가 감지되면 현재 표면 위에서 직접 접근합니다.
/// </summary>
public sealed class SpiderSurfaceRouteMover : MonsterNavTargetMover
{
    [Header("Route")]
    [SerializeField] private MonsterRoute route;

    private int waypointIndex;

    private void FixedUpdate()
    {
        if (stateMachine != null && (stateMachine.State == MonsterState.Dead || stateMachine.State == MonsterState.Falling))
        {
            return;
        }

        if (stateMachine != null && stateMachine.State == MonsterState.Chase && stateMachine.Target != null)
        {
            MoveNavTargetToward(stateMachine.Target.position, currentSurfaceNormal, Time.fixedDeltaTime);
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
        if (route == null || route.Count == 0)
        {
            return;
        }

        MonsterWaypoint waypoint = route.GetWaypoint(waypointIndex);
        if (waypoint == null)
        {
            AdvanceWaypoint();
            return;
        }

        bool arrived = MoveNavTargetToward(waypoint.transform.position, waypoint.SurfaceNormal, Time.fixedDeltaTime);
        if (arrived)
        {
            AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        waypointIndex++;
        if (route != null && waypointIndex >= route.Count)
        {
            waypointIndex = route.Loop ? 0 : route.Count - 1;
        }
    }
}
