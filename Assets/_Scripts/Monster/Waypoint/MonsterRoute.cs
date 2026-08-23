using UnityEngine;

/// <summary>
/// 여러 waypoint를 순서대로 묶은 가장 단순한 몬스터 이동 경로입니다.
/// 복잡한 길찾기 전에, 지정된 점들을 따라 NavTarget을 움직이는 MVP 검증용으로 사용합니다.
/// </summary>
public sealed class MonsterRoute : MonoBehaviour
{
    [SerializeField] private MonsterWaypoint[] waypoints;
    [SerializeField] private bool loop = true;

    public bool Loop => loop;
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
        if (waypoints == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
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

