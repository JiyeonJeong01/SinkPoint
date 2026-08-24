using UnityEngine;

/// <summary>
/// 플레이어가 어느 Zone에 진입했는지만 GameFlowManager에 전달하는 얇은 Trigger입니다.
/// Zone 활성화, 바리게이트, 몬스터 완료 처리는 GameFlowManager가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ZoneEntryTrigger : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField, Tooltip("플레이어가 이 Trigger에 들어오면 진입 처리할 Zone입니다.")]
    private ZoneId zoneId;
    [SerializeField, Tooltip("비워두면 GameFlowManager.Instance를 사용합니다.")]
    private GameFlowManager gameFlowManager;
    [SerializeField, Tooltip("켜면 한 번 성공한 뒤 같은 Trigger는 다시 동작하지 않습니다.")]
    private bool oneShot = true;

    [Header("Player Detection")]
    [SerializeField, Tooltip("플레이어로 인정할 태그입니다. Player 태그가 없거나 자식 Collider 구조라면 PlayerInput 부모도 함께 검사합니다.")]
    private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasTriggered;

    public ZoneId ZoneId => zoneId;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && hasTriggered)
        {
            return;
        }

        if (!IsPlayer(other))
        {
            return;
        }

        GameFlowManager manager = gameFlowManager != null ? gameFlowManager : GameFlowManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[{nameof(ZoneEntryTrigger)}] GameFlowManager is not assigned.", this);
            return;
        }

        hasTriggered = true;
        manager.NotifyZoneEntered(zoneId, this);

        if (showDebugLog)
        {
            Debug.Log($"[{nameof(ZoneEntryTrigger)}] Player entered {zoneId}.", this);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerInput>() != null;
    }
}
