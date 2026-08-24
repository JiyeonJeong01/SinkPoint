using UnityEngine;

/// <summary>
/// MVP 게임 진행을 관리하는 씬 단위 매니저입니다.
/// 중력 트리거, 플레이어 사망, 체크포인트 리셋처럼 여러 시스템을 함께 조정해야 하는 흐름의 중심으로 사용합니다.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    [System.Serializable]
    private sealed class StateRespawnPoint
    {
        [Tooltip("이 리스폰 위치를 사용할 게임 진행 상태입니다.")]
        public GameFlowState state;

        [Tooltip("해당 상태에서 플레이어가 사망했을 때 돌아갈 위치입니다.")]
        public Transform respawnPoint;
    }

    public enum GameFlowState
    {
        Entry,
        Normal,
        GravityShift,
        Inversion,
        FastDown,
        Slow,
        ZeroGravity,
        Source,
        Ending
    }

    public static GameFlowManager Instance { get; private set; }

    [Header("Gravity Event Triggers")]
    [Tooltip("씬에 배치된 GravityEventTrigger 목록입니다. 시작 시 코드로 일괄 구독합니다.")]
    [SerializeField] private GravityEventTrigger[] gravityEventTriggers;
    [SerializeField] private GravityManager gravityManager;

    [Header("Respawn")]
    [Tooltip("플레이어 위치/속도 리스폰을 담당하는 컨트롤러입니다.")]
    [SerializeField] private RespawnController respawnController;

    [Tooltip("현재 GameFlowState에 따라 사용할 리스폰 위치 목록입니다.")]
    [SerializeField] private StateRespawnPoint[] stateRespawnPoints;

    [Header("Current State")]
    [SerializeField] private GameFlowState currentState = GameFlowState.Entry;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    public GameFlowState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameFlowManager] Duplicate instance found. Destroying the new one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RegisterGravityTriggers();
    }

    private void OnDestroy()
    {
        UnregisterGravityTriggers();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 플레이어 HP가 0 이하가 됐을 때 호출되는 게임 진행 리셋 진입점입니다.
    /// 실제 플레이어 회복, 리스폰, 몬스터 초기화는 각 담당 시스템이 생기면 여기서 연결합니다.
    /// </summary>
    public void HandlePlayerDeath()
    {
        if (respawnController != null)
        {
            Transform respawnPoint = GetRespawnPoint(currentState);
            respawnController.RespawnPlayer(respawnPoint);
        }
        else
        {
            Debug.LogWarning("[GameFlowManager] RespawnController is not assigned.", this);
        }

        if (showDebugLog)
        {
            Debug.Log("[GameFlowManager] Player death handled. TODO: respawn player, restore HP, reset enemies, restore checkpoint state.", this);
        }

        // TODO: PlayerHealth가 생기면 HP를 풀피로 복구하는 함수를 호출할 것.
        // TODO: Enemy/Spawn 시스템이 생기면 현재 구역 몬스터를 정리하고 풀피 상태로 재배치할 것.
        // TODO: GravityManager가 생기면 체크포인트 기준 중력 상태로 복구할지 결정해서 연결할 것.
        // TODO: UI가 생기면 사망/리스폰 피드백과 현재 목표 텍스트를 갱신할 것.
    }

    // 현재 진행 상태에 맞는 리스폰 위치를 찾습니다.
    // 체크포인트 규칙이 복잡해지기 전까지는 Inspector 배열 매핑만으로 충분합니다.
    private Transform GetRespawnPoint(GameFlowState state)
    {
        if (stateRespawnPoints == null)
        {
            return null;
        }

        foreach (StateRespawnPoint stateRespawnPoint in stateRespawnPoints)
        {
            if (stateRespawnPoint == null || stateRespawnPoint.state != state)
            {
                continue;
            }

            return stateRespawnPoint.respawnPoint;
        }

        Debug.LogWarning($"[GameFlowManager] Respawn point is not assigned for state: {state}", this);
        return null;
    }

    private void RegisterGravityTriggers()
    {
        if (gravityEventTriggers == null)
        {
            return;
        }

        foreach (GravityEventTrigger trigger in gravityEventTriggers)
        {
            if (trigger == null)
            {
                continue;
            }

            trigger.Triggered += OnGravityEventTriggered;
        }
    }

    private void UnregisterGravityTriggers()
    {
        if (gravityEventTriggers == null)
        {
            return;
        }

        foreach (GravityEventTrigger trigger in gravityEventTriggers)
        {
            if (trigger == null)
            {
                continue;
            }

            trigger.Triggered -= OnGravityEventTriggered;
        }
    }

    // GravityEventTrigger가 통과 판정을 완료했을 때 호출되는 게임 진행 이벤트 처리 함수입니다.
    // 진행 상태를 갱신하고, 이후 GravityManager/UI/스폰/연출 시스템도 이 함수에서 연결합니다.
    private void OnGravityEventTriggered(GravityEventTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }

        GameFlowState nextState = ConvertToFlowState(trigger.EventType);

        if (gravityManager == null)
        {
            Debug.LogError("[GameFlowManager] GravityManager is not assigned.", this);
            return;
        }

        if (!gravityManager.ApplyPreset(trigger.Preset))
        {
            return;
        }

        SetState(nextState);

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] Gravity event received: {trigger.EventType}. Objective: {trigger.ObjectiveText}", trigger);
        }

        // TODO: Objective UI가 생기면 trigger.ObjectiveText를 표시할 것.
        // TODO: 필요하면 이벤트 타입별 몬스터 스폰, 문 개방, VFX/SFX를 호출할 것.
    }

    private GameFlowState ConvertToFlowState(GravityEventTrigger.GravityEventType eventType)
    {
        switch (eventType)
        {
            case GravityEventTrigger.GravityEventType.ShiftGravity:
                return GameFlowState.GravityShift;
            case GravityEventTrigger.GravityEventType.Inversion:
                return GameFlowState.Inversion;
            case GravityEventTrigger.GravityEventType.FastDown:
                return GameFlowState.FastDown;
            case GravityEventTrigger.GravityEventType.Slow:
                return GameFlowState.Slow;
            case GravityEventTrigger.GravityEventType.ZeroGravity:
                return GameFlowState.ZeroGravity;
            default:
                return currentState;
        }
    }

    private void SetState(GameFlowState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        GameFlowState previousState = currentState;
        currentState = nextState;

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] State changed: {previousState} -> {currentState}", this);
        }
    }
}
