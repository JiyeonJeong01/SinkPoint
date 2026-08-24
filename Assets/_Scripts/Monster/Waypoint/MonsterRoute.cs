using UnityEngine;

/// <summary>
/// 여러 waypoint를 순서대로 묶은 가장 단순한 몬스터 이동 경로입니다.
/// 복잡한 길찾기 전에, 지정된 점들을 따라 NavTarget을 움직이는 MVP 검증용으로 사용합니다.
/// </summary>
public sealed class MonsterRoute : MonoBehaviour
{
    [SerializeField] private MonsterWaypoint[] waypoints;
    [SerializeField] private bool loop = true;

    [Header("Debug Gizmos")]
    [SerializeField, Tooltip("이 route의 waypoint 연결선을 Scene View에 표시합니다. 여러 route 중 수정 중인 route만 켜두면 보기 쉽습니다.")]
    private bool drawRouteGizmos = true;
    [SerializeField, Tooltip("이 route 연결선 색상입니다. route마다 다르게 두면 구분하기 쉽습니다.")]
    private Color routeGizmoColor = Color.yellow;

    public bool Loop => loop;
    public bool DrawRouteGizmos => drawRouteGizmos;
    public int Count => waypoints != null ? waypoints.Length : 0;

    /// <summary>
    /// index 위치의 waypoint를 반환합니다. 범위를 벗어나면 null을 반환해 호출자가 정지할 수 있게 합니다.
    /// </summary>
    public MonsterWaypoint GetWaypoint(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length)
        {
            return null;
        }

        return waypoints[index];
    }

    private void OnDrawGizmos()
    {
        if (!drawRouteGizmos || waypoints == null)
        {
            return;
        }

        Gizmos.color = routeGizmoColor;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }
        }

        if (loop && waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].transform.position, waypoints[0].transform.position);
        }
    }
}

