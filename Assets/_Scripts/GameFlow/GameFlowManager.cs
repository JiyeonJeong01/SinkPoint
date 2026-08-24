using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// MVP 게임 진행을 관리하는 씬 단위 매니저입니다.
/// 중력 트리거, 플레이어 사망, 체크포인트 리셋처럼 여러 시스템을 함께 조정해야 하는 흐름의 중심으로 사용합니다.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public event System.Action<ZoneId> CurrentZoneChanged;

    [System.Serializable]
    private sealed class StateRespawnPoint
    {
        [Tooltip("이 리스폰 위치를 사용할 게임 진행 상태입니다.")]
        public GameFlowState state;

        [Tooltip("해당 상태에서 플레이어가 사망했을 때 돌아갈 위치입니다.")]
        public Transform respawnPoint;
    }

    [System.Serializable]
    private sealed class ZoneFlowData
    {
        [Tooltip("이 데이터가 담당하는 Zone입니다.")]
        public ZoneId zoneId;

        [Tooltip("이 Zone의 메시, 이펙트 등 시각 오브젝트 부모입니다.")]
        public GameObject visualRoot;

        [Tooltip("플레이어가 이 Zone에 들어왔음을 알리는 Trigger입니다.")]
        public ZoneEntryTrigger entryTrigger;

        [Tooltip("이 Zone에 들어온 뒤, 플레이어가 지나온 이전 Zone 방향을 막을 바리게이트입니다. 첫 Zone은 비워둡니다.")]
        public MapBoxBarrier previousBarrier;

        [Tooltip("현재 Zone 몬스터를 모두 처치하면 열릴 다음 Zone 방향 바리게이트입니다. 마지막 Zone은 비워둡니다.")]
        public MapBoxBarrier nextBarrier;

        [Header("Runtime State")]
        [Tooltip("이 Zone 완료 처리가 끝났는지 표시합니다.")]
        public bool completed;

        [Tooltip("이 Zone에 한 번이라도 진입했는지 표시합니다.")]
        public bool entered;
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
    [SerializeField, Tooltip("플레이어 체력입니다. 비워두면 씬에서 자동으로 찾고, 사망 리스폰 때 체력을 회복합니다.")]
    private PlayerHealth playerHealth;

    [Tooltip("Zone별 몬스터 활성화, 리스폰, 전멸 알림을 담당하는 매니저입니다.")]
    [SerializeField] private MonsterManager monsterManager;

    [Tooltip("현재 GameFlowState에 따라 사용할 리스폰 위치 목록입니다.")]
    [SerializeField] private StateRespawnPoint[] stateRespawnPoints;

    [Header("Zone Activation")]
    [Tooltip("씬 시작 시 활성화할 Zone입니다.")]
    [SerializeField] private ZoneId initialZone = ZoneId.Zone01_Entry;

    [Tooltip("Zone별 시각 오브젝트 부모, 진입 Trigger, 바리게이트 목록입니다.")]
    [SerializeField] private ZoneFlowData[] zones;

    [Header("Current State")]
    [SerializeField] private GameFlowState currentState = GameFlowState.Entry;
    [SerializeField, Tooltip("현재 활성 진행 Zone입니다.")]
    private ZoneId currentZone = ZoneId.Zone01_Entry;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    public GameFlowState CurrentState => currentState;
    public ZoneId CurrentZone => currentZone;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameFlowManager] Duplicate instance found. Destroying the new one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveSceneReferences();
        RegisterPlayerHealth();
        RegisterGravityTriggers();
        InitializeZones();
    }

    private void OnDestroy()
    {
        UnregisterPlayerHealth();
        UnregisterGravityTriggers();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            DebugOpenCurrentNextBarrier();
        }
    }

    [Button("Open Current Next Barrier")]
    private void DebugOpenCurrentNextBarrier()
    {
        if (!TryGetZone(currentZone, out ZoneFlowData zone))
        {
            Debug.LogWarning($"[GameFlowManager] Cannot open barrier. Missing current Zone data: {currentZone}", this);
            return;
        }

        if (zone.nextBarrier == null)
        {
            Debug.LogWarning($"[GameFlowManager] {currentZone} has no next barrier to open.", this);
            return;
        }

        PrepareNextZoneBeforeOpeningBarrier(currentZone);
        StartCoroutine(zone.nextBarrier.OpenRoutine());
    }

    /// <summary>
    /// ZoneEntryTrigger가 플레이어 진입을 감지했을 때 호출하는 진입점입니다.
    /// 빠른 중복 Trigger는 무시하고, 이전 Zone 정리는 바리게이트 닫힘 이후에만 실행합니다.
    /// </summary>
    public void NotifyZoneEntered(ZoneId zoneId, ZoneEntryTrigger trigger)
    {
        if (!TryGetZone(zoneId, out ZoneFlowData zone))
        {
            Debug.LogWarning($"[GameFlowManager] Ignored Zone enter. Missing Zone data: {zoneId}", trigger != null ? trigger : this);
            return;
        }

        if (isZoneTransitionRunning)
        {
            if (showDebugLog)
            {
                Debug.Log($"[GameFlowManager] Ignored {zoneId} enter because a transition is already running.", trigger);
            }

            return;
        }

        if (currentZone == zoneId && zone.entered)
        {
            return;
        }

        StartCoroutine(EnterZoneRoutine(zoneId, zone));
    }

    /// <summary>
    /// 몬스터 매니저가 현재 Zone의 몬스터가 모두 정리됐을 때 호출하는 API입니다.
    /// 완료 처리는 한 번만 실행되며, 성공하면 현재 Zone의 다음 바리게이트를 엽니다.
    /// </summary>
    public void NotifyCurrentZoneCleared()
    {
        NotifyZoneCleared(currentZone);
    }

    /// <summary>
    /// 몬스터 매니저가 특정 Zone의 몬스터가 모두 정리됐을 때 호출하는 API입니다.
    /// 현재 플레이어가 있는 Zone이 아닌 완료 알림은 진행 순서가 꼬이지 않도록 무시합니다.
    /// </summary>
    public void NotifyZoneCleared(ZoneId zoneId)
    {
        if (!TryGetZone(zoneId, out ZoneFlowData zone))
        {
            Debug.LogWarning($"[GameFlowManager] Ignored Zone clear. Missing Zone data: {zoneId}", this);
            return;
        }

        TryCompleteZone(zoneId, zone);
    }

    public void NotifyZoneCleared(Zone zone)
    {
        if (zone == null)
        {
            Debug.LogWarning("[GameFlowManager] Ignored Zone clear. Zone reference is null.", this);
            return;
        }

        NotifyZoneCleared(zone.Id);
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

        if (monsterManager != null)
        {
            monsterManager.RespawnZone(currentZone);
        }
        else
        {
            Debug.LogWarning("[GameFlowManager] MonsterManager is not assigned.", this);
        }

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
        else
        {
            Debug.LogWarning("[GameFlowManager] PlayerHealth is not assigned.", this);
        }

        if (showDebugLog)
        {
            Debug.Log("[GameFlowManager] Player death handled. TODO: respawn player, restore HP, reset enemies, restore checkpoint state.", this);
        }
        // TODO: GravityManager가 생기면 체크포인트 기준 중력 상태로 복구할지 결정해서 연결할 것.
        // TODO: UI가 생기면 사망/리스폰 피드백과 현재 목표 텍스트를 갱신할 것.
    }

    private void ResolveSceneReferences()
    {
        monsterManager ??= FindFirstObjectByType<MonsterManager>();
        playerHealth ??= FindFirstObjectByType<PlayerHealth>();
    }

    private void RegisterPlayerHealth()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[GameFlowManager] PlayerHealth is not assigned.", this);
            return;
        }

        playerHealth.Died -= OnPlayerDied;
        playerHealth.Died += OnPlayerDied;
    }

    private void UnregisterPlayerHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }
    }

    private void OnPlayerDied(PlayerHealth health)
    {
        HandlePlayerDeath();
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

    private bool isZoneTransitionRunning;

    private void InitializeZones()
    {
        if (zones == null || zones.Length == 0)
        {
            return;
        }

        currentZone = initialZone;

        for (int i = 0; i < zones.Length; i++)
        {
            ZoneFlowData zone = zones[i];
            if (zone == null)
            {
                Debug.LogWarning($"[GameFlowManager] Zones[{i}] is null.", this);
                continue;
            }

            zone.entered = zone.zoneId == currentZone;
            zone.completed = false;

            SetZoneActive(zone, zone.zoneId == currentZone);
            RegisterZoneTrigger(zone);

            if (zone.previousBarrier != null && zone.zoneId == currentZone)
            {
                zone.previousBarrier.SetImmediate(true);
            }

            if (zone.nextBarrier != null)
            {
                zone.nextBarrier.SetImmediate(true);
            }
        }

        if (monsterManager != null)
        {
            monsterManager.InitializeForGameFlow(this);
            monsterManager.BeginZone(currentZone);
        }
    }

    private void RegisterZoneTrigger(ZoneFlowData zone)
    {
        if (zone.entryTrigger == null)
        {
            return;
        }

        // Trigger는 자체 OnTriggerEnter로 NotifyZoneEntered를 호출하므로 여기서는 누락 여부만 확인합니다.
    }

    private IEnumerator EnterZoneRoutine(ZoneId zoneId, ZoneFlowData zone)
    {
        isZoneTransitionRunning = true;

        ZoneId previousZoneId = currentZone;
        TryGetZone(previousZoneId, out ZoneFlowData previousZone);

        SetZoneActive(zone, true);
        zone.entered = true;
        currentZone = zoneId;
        CurrentZoneChanged?.Invoke(currentZone);
        if (monsterManager != null)
        {
            monsterManager.BeginZone(zoneId);
        }

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] Enter {zoneId}. Previous Zone: {previousZoneId}", this);
        }

        if (previousZone != null && previousZoneId != zoneId)
        {
            MapBoxBarrier barrierToClose = zone.previousBarrier != null
                ? zone.previousBarrier
                : previousZone.nextBarrier;

            if (barrierToClose != null)
            {
                yield return barrierToClose.CloseRoutine();
            }
            else if (TryGetPreviousZone(zoneId, out _))
            {
                Debug.LogWarning($"[GameFlowManager] {zoneId} has no previous barrier to close.", this);
            }

            SetZoneActive(previousZone, false);
            if (monsterManager != null)
            {
                monsterManager.DeactivateZone(previousZoneId);
            }
        }

        isZoneTransitionRunning = false;
    }

    private void TryCompleteZone(ZoneId zoneId, ZoneFlowData zone)
    {
        if (zone == null || zone.completed || zoneId != currentZone)
        {
            return;
        }

        StartCoroutine(CompleteZoneRoutine(zoneId, zone));
    }

    private IEnumerator CompleteZoneRoutine(ZoneId zoneId, ZoneFlowData zone)
    {
        if (zone.completed)
        {
            yield break;
        }

        zone.completed = true;

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] {zoneId} completed.", this);
        }

        if (zone.nextBarrier != null)
        {
            PrepareNextZoneBeforeOpeningBarrier(zoneId);
            yield return zone.nextBarrier.OpenRoutine();
        }
    }

    // 다음 Zone 문이 열릴 때 너머가 비어 보이지 않도록, 진행 상태 변경 없이 시각 루트만 미리 켭니다.
    private void PrepareNextZoneBeforeOpeningBarrier(ZoneId zoneId)
    {
        if (!TryGetNextZone(zoneId, out ZoneId nextZoneId))
        {
            return;
        }

        if (!TryGetZone(nextZoneId, out ZoneFlowData nextZone))
        {
            Debug.LogWarning($"[GameFlowManager] Cannot prepare next Zone. Missing Zone data: {nextZoneId}", this);
            return;
        }

        SetZoneActive(nextZone, true);
        if (monsterManager != null)
        {
            monsterManager.PrepareZone(nextZoneId);
        }
    }

    private void SetZoneActive(ZoneFlowData zone, bool active)
    {
        if (zone == null)
        {
            return;
        }

        if (zone.visualRoot != null)
        {
            zone.visualRoot.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"[GameFlowManager] {zone.zoneId} visual root is not assigned.", this);
        }
    }

    private bool TryGetZone(ZoneId zoneId, out ZoneFlowData zone)
    {
        zone = null;
        if (zones == null)
        {
            return false;
        }

        foreach (ZoneFlowData candidate in zones)
        {
            if (candidate == null || candidate.zoneId != zoneId)
            {
                continue;
            }

            zone = candidate;
            return true;
        }

        return false;
    }

    private static bool TryGetPreviousZone(ZoneId zoneId, out ZoneId previousZone)
    {
        int previousValue = (int)zoneId - 1;
        if (!System.Enum.IsDefined(typeof(ZoneId), previousValue))
        {
            previousZone = zoneId;
            return false;
        }

        previousZone = (ZoneId)previousValue;
        return true;
    }

    public static bool TryGetNextZone(ZoneId zoneId, out ZoneId nextZone)
    {
        int nextValue = (int)zoneId + 1;
        if (!System.Enum.IsDefined(typeof(ZoneId), nextValue))
        {
            nextZone = zoneId;
            return false;
        }

        nextZone = (ZoneId)nextValue;
        return true;
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

        if (!gravityManager.ActivateZone(trigger.Zone))
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
