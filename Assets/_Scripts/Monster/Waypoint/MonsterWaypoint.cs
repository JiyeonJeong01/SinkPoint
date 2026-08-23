using UnityEngine;

/// <summary>
/// 몬스터가 따라갈 수 있는 수동 경로 지점입니다.
/// 위치뿐 아니라 해당 지점에서 NavTarget.up이 바라봐야 할 표면 normal도 함께 제공합니다.
/// </summary>
public sealed class MonsterWaypoint : MonoBehaviour
{
    [SerializeField] private Vector3 localSurfaceNormal = Vector3.up;
    [SerializeField] private MonsterWaypoint[] nextWaypoints;   // 각 WayPoint는 갈림길을 가질 수 있다.
    [SerializeField] private bool jumpRequired;

    // WayPoint의 노멀 Local -> World 
    public Vector3 SurfaceNormal => transform.TransformDirection(localSurfaceNormal.normalized);
    public MonsterWaypoint[] NextWaypoints => nextWaypoints;
    public bool JumpRequired => jumpRequired;

    private void OnValidate()
    {
        if (localSurfaceNormal.sqrMagnitude < Mathf.Epsilon)
        {
            localSurfaceNormal = Vector3.up;
        }
        else
        {
            localSurfaceNormal.Normalize();
        }
    }

    private void OnDrawGizmos()
    {
        // 점프가 필요하면 청록색, 일반 이동은 초록색으로 표시한다.
        Gizmos.color = jumpRequired ? Color.cyan : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawRay(transform.position, SurfaceNormal * 0.75f);
    }
}
