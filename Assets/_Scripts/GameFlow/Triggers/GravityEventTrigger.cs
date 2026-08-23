using System;
using UnityEngine;
using UnityEngine.Events;

// TODO: GravityManager 클래스가 생기면 GameFlowManager에서 이 트리거 이벤트를 받아 반드시 중력 변경 호출로 연결할 것.

/// <summary>
/// 플레이어가 중력 전환 트리거를 올바른 진행 방향으로 통과했는지 판정하는 컴포넌트입니다.
/// 이 클래스는 중력을 직접 바꾸지 않고, 통과가 확정됐을 때 이벤트만 발생시켜 GameFlowManager나 GravityManager가 받아서 처리하게 둡니다.
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
    [SerializeField] private bool oneShot = true;

    [Header("Pass Direction")]
    [Tooltip("통과 방향 기준입니다. 비워두면 이 트리거 오브젝트의 forward 축을 사용합니다.")]
    [SerializeField] private Transform directionReference;

    [Tooltip("켜두면 들어온 쪽과 반대쪽으로 빠져나간 경우에만 이벤트를 실행합니다.")]
    [SerializeField] private bool requireOppositeSideExit = true;

    [Tooltip("트리거 중심 근처에서 들어오거나 나간 값을 무시하기 위한 여유 거리입니다.")]
    [SerializeField] private float sideDeadZone = 0.05f;

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
    private bool playerInside;
    private Transform playerTransform;
    private float enterSide;

    public GravityEventType EventType => eventType;
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

        playerInside = true;
        playerTransform = other.transform;
        enterSide = GetSideValue(other.transform.position);

        // 반대편 통과 검사를 쓰지 않는 트리거는 Enter 순간에 바로 실행합니다.
        if (!requireOppositeSideExit)
        {
            TryTrigger(other.transform.position, "Enter");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerInside || other.transform != playerTransform)
        {
            return;
        }

        playerInside = false;
        playerTransform = null;

        TryTrigger(other.transform.position, "Exit");
    }

    // 트리거 실행의 최종 관문입니다.
    // oneShot, 통과 방향 검증, 디버그 로그, 외부 이벤트 호출을 여기서 한 번에 처리합니다.
    private void TryTrigger(Vector3 currentPosition, string triggerPhase)
    {
        if (oneShot && hasTriggered)
        {
            return;
        }

        if (requireOppositeSideExit && !IsOppositeSideExit(currentPosition))
        {
            if (showDebugLog)
            {
                Debug.Log($"[GravityEventTrigger] Ignored {eventType}: exited from the same side.", this);
            }

            return;
        }

        hasTriggered = true;

        if (showDebugLog)
        {
            Debug.Log($"[GravityEventTrigger] Triggered {eventType} on {triggerPhase}. Objective: {objectiveText}", this);
        }

        // 코드 등록 방식용 이벤트입니다. GameFlowManager가 씬 시작 시 여러 트리거를 한 번에 구독할 때 사용합니다.
        Triggered?.Invoke(this);

        // Inspector에서 임시 테스트나 개별 연결이 필요할 때를 위해 남겨둔 보조 이벤트입니다.
        onTriggered?.Invoke(eventType);
    }

    private bool IsOppositeSideExit(Vector3 exitPosition)
    {
        float exitSide = GetSideValue(exitPosition);

        // 중심선 근처에서 들어오거나 나간 경우는 어느 쪽인지 불안정하므로 통과로 보지 않습니다.
        if (Mathf.Abs(enterSide) <= sideDeadZone || Mathf.Abs(exitSide) <= sideDeadZone)
        {
            return false;
        }

        return Mathf.Sign(enterSide) != Mathf.Sign(exitSide);
    }

    private float GetSideValue(Vector3 worldPosition)
    {
        Transform reference = directionReference != null
            ? directionReference
            : transform;

        Vector3 offsetFromCenter = worldPosition - reference.position;
        return Vector3.Dot(offsetFromCenter, reference.forward.normalized);
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
