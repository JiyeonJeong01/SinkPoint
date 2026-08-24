using System.Collections;
using System.Collections.Generic;
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

    [System.Serializable]
    private sealed class ZoneFlowData
    {
        [Tooltip("진행용 Zone 번호입니다. 배열 순서와 맞추는 것을 권장합니다.")]
        public int zoneIndex;

        [Tooltip("이 Zone의 메시, 이펙트 등 시각 오브젝트 부모입니다.")]
        public GameObject visualRoot;

        [Tooltip("이 Zone의 이동/전투용 콜라이더 오브젝트 부모입니다. ZoneEntryTrigger는 꺼지지 않도록 별도 부모에 두는 것을 권장합니다.")]
        public GameObject colliderRoot;

        [Tooltip("플레이어가 이 Zone에 들어왔음을 알리는 Trigger입니다.")]
        public ZoneEntryTrigger entryTrigger;

        [Tooltip("이 Zone에 들어온 뒤, 플레이어가 지나온 이전 Zone 방향을 막을 바리게이트입니다. 첫 Zone은 비워둡니다.")]
        public MapBoxBarrier previousBarrier;

        [Tooltip("현재 Zone 몬스터를 모두 처치하면 열릴 다음 Zone 방향 바리게이트입니다. 마지막 Zone은 비워둡니다.")]
        public MapBoxBarrier nextBarrier;

        [Tooltip("이 Zone 완료 조건으로 추적할 몬스터 목록입니다. 비어 있으면 진입 즉시 완료로 처리합니다.")]
        public MonsterHealth[] monsters;

        [Header("Runtime State")]
        [Tooltip("Play Mode에서 확인하는 남은 몬스터 수입니다.")]
        public int remainingMonsterCount;

        [Tooltip("이 Zone 완료 처리가 끝났는지 표시합니다.")]
        public bool completed;

        [Tooltip("이 Zone에 한 번이라도 진입했는지 표시합니다.")]
        public bool entered;

        [System.NonSerialized] public readonly HashSet<MonsterHealth> defeatedMonsters = new HashSet<MonsterHealth>();
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

    [Header("Zone Activation")]
    [Tooltip("씬 시작 시 활성화할 Zone 인덱스입니다.")]
    [SerializeField, Min(0)] private int initialZoneIndex;

    [Tooltip("Zone별 시각/콜라이더 부모, 진입 Trigger, 바리게이트, 몬스터 목록입니다.")]
    [SerializeField] private ZoneFlowData[] zones;

    [Header("Current State")]
    [SerializeField] private GameFlowState currentState = GameFlowState.Entry;
    [SerializeField, Tooltip("현재 활성 진행 Zone 인덱스입니다.")]
    private int currentZoneIndex = -1;

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
        InitializeZones();
    }

    private void OnDestroy()
    {
        UnregisterGravityTriggers();
        UnregisterZoneEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// ZoneEntryTrigger가 플레이어 진입을 감지했을 때 호출하는 진입점입니다.
    /// 빠른 중복 Trigger는 무시하고, 이전 Zone 정리는 바리게이트 닫힘 이후에만 실행합니다.
    /// </summary>
    public void NotifyZoneEntered(int zoneIndex, ZoneEntryTrigger trigger)
    {
        if (!TryGetZone(zoneIndex, out ZoneFlowData zone))
        {
            Debug.LogWarning($"[GameFlowManager] Ignored Zone enter. Invalid Zone index: {zoneIndex}", trigger != null ? trigger : this);
            return;
        }

        if (isZoneTransitionRunning)
        {
            if (showDebugLog)
            {
                Debug.Log($"[GameFlowManager] Ignored Zone {zoneIndex} enter because a transition is already running.", trigger);
            }

            return;
        }

        if (currentZoneIndex == zoneIndex && zone.entered)
        {
            return;
        }

        StartCoroutine(EnterZoneRoutine(zoneIndex, zone));
    }

    /// <summary>
    /// 몬스터 매니저나 몬스터 사망 이벤트에서 호출할 Zone 완료 추적 API입니다.
    /// 같은 몬스터 사망이 중복으로 들어와도 남은 수가 두 번 줄지 않습니다.
    /// </summary>
    public void NotifyMonsterDied(MonsterHealth monsterHealth)
    {
        if (monsterHealth == null || zones == null)
        {
            return;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            ZoneFlowData zone = zones[i];
            if (zone == null || zone.monsters == null)
            {
                continue;
            }

            for (int j = 0; j < zone.monsters.Length; j++)
            {
                if (zone.monsters[j] == monsterHealth)
                {
                    RegisterMonsterDeath(i, zone, monsterHealth);
                    return;
                }
            }
        }
    }

    public void NotifyMonsterDied(int zoneIndex, MonsterHealth monsterHealth)
    {
        if (!TryGetZone(zoneIndex, out ZoneFlowData zone))
        {
            Debug.LogWarning($"[GameFlowManager] Ignored monster death. Invalid Zone index: {zoneIndex}", this);
            return;
        }

        RegisterMonsterDeath(zoneIndex, zone, monsterHealth);
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

    private bool isZoneTransitionRunning;

    private void InitializeZones()
    {
        if (zones == null || zones.Length == 0)
        {
            return;
        }

        currentZoneIndex = Mathf.Clamp(initialZoneIndex, 0, zones.Length - 1);

        for (int i = 0; i < zones.Length; i++)
        {
            ZoneFlowData zone = zones[i];
            if (zone == null)
            {
                Debug.LogWarning($"[GameFlowManager] Zones[{i}] is null.", this);
                continue;
            }

            if (zone.zoneIndex < 0)
            {
                zone.zoneIndex = i;
            }

            zone.entered = i == currentZoneIndex;
            zone.completed = false;
            zone.defeatedMonsters.Clear();
            zone.remainingMonsterCount = CountLivingMonsters(zone);

            SetZoneActive(zone, i == currentZoneIndex);
            RegisterZoneTrigger(zone);
            RegisterMonsterEvents(zone);

            if (zone.previousBarrier != null && i == currentZoneIndex)
            {
                zone.previousBarrier.SetImmediate(true);
            }

            if (zone.nextBarrier != null)
            {
                zone.nextBarrier.SetImmediate(true);
            }
        }

        TryCompleteZone(currentZoneIndex, zones[currentZoneIndex]);
    }

    private void RegisterZoneTrigger(ZoneFlowData zone)
    {
        if (zone.entryTrigger == null)
        {
            return;
        }

        // Trigger는 자체 OnTriggerEnter로 NotifyZoneEntered를 호출하므로 여기서는 누락 여부만 확인합니다.
    }

    private void RegisterMonsterEvents(ZoneFlowData zone)
    {
        if (zone.monsters == null)
        {
            return;
        }

        foreach (MonsterHealth monster in zone.monsters)
        {
            if (monster == null)
            {
                continue;
            }

            monster.Died -= OnTrackedMonsterDied;
            monster.Died += OnTrackedMonsterDied;
        }
    }

    private void UnregisterZoneEvents()
    {
        if (zones == null)
        {
            return;
        }

        foreach (ZoneFlowData zone in zones)
        {
            if (zone == null || zone.monsters == null)
            {
                continue;
            }

            foreach (MonsterHealth monster in zone.monsters)
            {
                if (monster != null)
                {
                    monster.Died -= OnTrackedMonsterDied;
                }
            }
        }
    }

    private void OnTrackedMonsterDied(MonsterHealth monsterHealth)
    {
        NotifyMonsterDied(monsterHealth);
    }

    private IEnumerator EnterZoneRoutine(int zoneIndex, ZoneFlowData zone)
    {
        isZoneTransitionRunning = true;

        int previousZoneIndex = currentZoneIndex;
        ZoneFlowData previousZone = previousZoneIndex >= 0 && previousZoneIndex < zones.Length
            ? zones[previousZoneIndex]
            : null;

        SetZoneActive(zone, true);
        zone.entered = true;
        currentZoneIndex = zoneIndex;

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] Enter Zone {zoneIndex}. Previous Zone: {previousZoneIndex}", this);
        }

        if (previousZone != null && previousZoneIndex != zoneIndex)
        {
            MapBoxBarrier barrierToClose = zone.previousBarrier != null
                ? zone.previousBarrier
                : previousZone.nextBarrier;

            if (barrierToClose != null)
            {
                yield return barrierToClose.CloseRoutine();
            }
            else if (zoneIndex > 0)
            {
                Debug.LogWarning($"[GameFlowManager] Zone {zoneIndex} has no previous barrier to close.", this);
            }

            SetZoneActive(previousZone, false);
        }

        TryCompleteZone(zoneIndex, zone);
        isZoneTransitionRunning = false;
    }

    private void RegisterMonsterDeath(int zoneIndex, ZoneFlowData zone, MonsterHealth monsterHealth)
    {
        if (zone == null || zone.completed)
        {
            return;
        }

        if (monsterHealth != null && !zone.defeatedMonsters.Add(monsterHealth))
        {
            return;
        }

        zone.remainingMonsterCount = Mathf.Max(0, zone.remainingMonsterCount - 1);

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] Zone {zoneIndex} monster defeated. Remaining: {zone.remainingMonsterCount}", this);
        }

        TryCompleteZone(zoneIndex, zone);
    }

    private void TryCompleteZone(int zoneIndex, ZoneFlowData zone)
    {
        if (zone == null || zone.completed || zoneIndex != currentZoneIndex || zone.remainingMonsterCount > 0)
        {
            return;
        }

        StartCoroutine(CompleteZoneRoutine(zoneIndex, zone));
    }

    private IEnumerator CompleteZoneRoutine(int zoneIndex, ZoneFlowData zone)
    {
        if (zone.completed)
        {
            yield break;
        }

        zone.completed = true;

        if (showDebugLog)
        {
            Debug.Log($"[GameFlowManager] Zone {zoneIndex} completed.", this);
        }

        if (zone.nextBarrier != null)
        {
            yield return zone.nextBarrier.OpenRoutine();
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
            Debug.LogWarning($"[GameFlowManager] Zone {zone.zoneIndex} visual root is not assigned.", this);
        }

        if (zone.colliderRoot != null)
        {
            zone.colliderRoot.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"[GameFlowManager] Zone {zone.zoneIndex} collider root is not assigned.", this);
        }
    }

    private int CountLivingMonsters(ZoneFlowData zone)
    {
        if (zone.monsters == null || zone.monsters.Length == 0)
        {
            return 0;
        }

        int count = 0;
        foreach (MonsterHealth monster in zone.monsters)
        {
            if (monster != null && !monster.IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private bool TryGetZone(int zoneIndex, out ZoneFlowData zone)
    {
        zone = null;
        if (zones == null || zoneIndex < 0 || zoneIndex >= zones.Length)
        {
            return false;
        }

        zone = zones[zoneIndex];
        return zone != null;
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
