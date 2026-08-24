using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 플레이어가 처음 닿았을 때 중력 이벤트를 한 번 발생시키는 컴포넌트입니다.
/// 이 클래스는 중력을 직접 바꾸지 않고 GameFlowManager가 GravityManager에 Zone 활성화를 요청하게 둡니다.
/// </summary>
public class GravityEventTrigger : MonoBehaviour
{
    [System.Serializable]
    public sealed class GravityEventUnityEvent : UnityEvent<GravityEventType>
    {
    }

    public enum GravityEventType
    {
        ShiftGravity,
        Inversion,
        FastDown,
        Slow,
        ZeroGravity
    }

    [Header("Event")]
    [SerializeField] private GravityEventType eventType = GravityEventType.ShiftGravity;
    [FormerlySerializedAs("zone")]
    [SerializeField] private GravityPreset gravityPreset;

    [Header("Optional Flow Data")]
    [TextArea]
    [SerializeField] private string objectiveText;

    [Header("Player Detection")]
    [Tooltip("플레이어로 인정할 태그입니다. Player 태그가 없거나 자식 Collider 구조라면 PlayerInput 부모도 함께 검사합니다.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    [Header("Unity Event")]
    [SerializeField] private GravityEventUnityEvent onTriggered;

    private bool hasTriggered;

    public GravityEventType EventType => eventType;
    public GravityPreset Preset => gravityPreset;
    public string ObjectiveText => objectiveText;
    public bool HasTriggered => hasTriggered;

    public event Action<GravityEventTrigger> Triggered;

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
        if (!IsPlayer(other))
        {
            return;
        }

        TryTrigger();
    }

    private void TryTrigger()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        if (showDebugLog)
        {
            Debug.Log($"[GravityEventTrigger] Triggered {eventType} on Enter. Objective: {objectiveText}", this);
        }

        // 코드 등록 방식용 이벤트입니다. GameFlowManager가 씬 시작 시 여러 트리거를 한 번에 구독할 때 사용합니다.
        Triggered?.Invoke(this);

        // Inspector에서 임시 테스트나 개별 연결이 필요할 때를 위해 남겨둔 보조 이벤트입니다.
        onTriggered?.Invoke(eventType);
    }

    private bool IsPlayer(Collider other)
    {
        // MVP 단계에서는 Player 태그를 우선 사용합니다.
        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        // 플레이어 Rigidbody의 자식 콜라이더가 들어오는 경우를 대비해 부모 쪽 입력 컴포넌트도 확인합니다.
        return other.GetComponentInParent<PlayerInput>() != null;
    }
}
