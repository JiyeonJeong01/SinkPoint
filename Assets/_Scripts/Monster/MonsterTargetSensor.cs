using UnityEngine;

/// <summary>
/// 몬스터가 플레이어를 감지하고 추적 대상으로 제공하는 컴포넌트입니다.
/// 실제 시야각/은신 판정이 생기기 전까지는 거리 기반 감지만 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterTargetSensor : MonoBehaviour
{
    [SerializeField] private Transform explicitTarget;
    [SerializeField, Min(0f)] private float detectionRadius = 12f;
    [SerializeField, Min(0f)] private float loseRadius = 16f;
    [SerializeField] private string playerTag = "Player";

    private Transform currentTarget;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    private void Update()
    {
        RefreshTarget();
    }

    /// <summary>
    /// 현재 추적 대상이 있으면 유지 가능 여부를 확인하고, 없으면 주변 플레이어를 찾습니다.
    /// </summary>
    public void RefreshTarget()
    {
        if (explicitTarget != null)
        {
            currentTarget = explicitTarget;
            return;
        }

        if (currentTarget != null)
        {
            float sqrLoseRadius = loseRadius * loseRadius;
            if ((currentTarget.position - transform.position).sqrMagnitude <= sqrLoseRadius)
            {
                return;
            }

            currentTarget = null;
        }

        GameObject player = string.IsNullOrWhiteSpace(playerTag)
            ? null
            : GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
        {
            return;
        }

        float sqrDetectionRadius = detectionRadius * detectionRadius;
        if ((player.transform.position - transform.position).sqrMagnitude <= sqrDetectionRadius)
        {
            currentTarget = player.transform;
        }
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        loseRadius = Mathf.Max(detectionRadius, loseRadius);
    }
}
