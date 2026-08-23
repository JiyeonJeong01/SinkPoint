using UnityEngine;

/// <summary>
/// Inversion Zone 땅 지렁이의 단순 MVP 공격 컴포넌트입니다.
/// 완전한 잠복 이동 대신, 플레이어 근처의 현재 중력 기준 바닥 위치를 골라 등장/공격 타이밍을 만듭니다.
/// </summary>
public sealed class WormBurrowAttack : MonoBehaviour
{
    [SerializeField] private MonsterTargetSensor targetSensor;
    [SerializeField] private MvpGravityState gravityState;
    [SerializeField, Min(0f)] private float emergeDistanceFromPlayer = 2f;
    [SerializeField, Min(0f)] private float attackInterval = 3f;
    [SerializeField] private bool showDebugLog;

    private float nextAttackTime;

    private void Awake()
    {
        targetSensor ??= GetComponent<MonsterTargetSensor>();
    }

    private void Update()
    {
        if (targetSensor == null || targetSensor.CurrentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;
        Vector3 emergePosition = PickEmergePosition(targetSensor.CurrentTarget.position);

        // 실제 잠복/등장 애니메이션이 붙기 전까지는 위치 후보와 타이밍만 만든다.
        if (showDebugLog)
        {
            Debug.Log($"[WormBurrowAttack] Emerge candidate: {emergePosition}", this);
        }
    }

    /// <summary>
    /// 플레이어 주변에서 지렁이가 나타날 후보 위치를 계산합니다.
    /// 지금은 현재 중력의 바닥 방향만 반영하고, 실제 표면 Raycast 보정은 다음 단계에서 붙입니다.
    /// </summary>
    private Vector3 PickEmergePosition(Vector3 playerPosition)
    {
        Vector3 gravityDirection = gravityState != null ? gravityState.Direction : Vector3.down;
        Vector3 side = Vector3.Cross(-gravityDirection, transform.forward);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.Cross(-gravityDirection, Vector3.forward);
        }

        return playerPosition + side.normalized * emergeDistanceFromPlayer;
    }

    private void OnValidate()
    {
        emergeDistanceFromPlayer = Mathf.Max(0f, emergeDistanceFromPlayer);
        attackInterval = Mathf.Max(0f, attackInterval);
    }
}
