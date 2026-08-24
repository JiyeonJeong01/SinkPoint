using System;
using UnityEngine;

/// <summary>
/// Zone별 몬스터 활성화, 리스폰, 사망 카운트를 관리합니다.
/// 몬스터 배치는 Inspector에서 ZoneId 단위로 묶어 관리하고, GameFlowManager는 진행 순서만 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterManager : MonoBehaviour
{
    [Serializable]
    private sealed class ZoneMonsterGroup
    {
        [Tooltip("이 그룹이 담당하는 진행 Zone입니다.")]
        public ZoneId zoneId;

        [Tooltip("해당 Zone에서 관리할 몬스터 목록입니다. 씬에 배치된 비활성 몬스터도 넣을 수 있습니다.")]
        public Monster[] monsters;
    }

    [Serializable]
    private sealed class ZoneMonsterReadout
    {
        public ZoneId zoneId;
        public int total;
        public int alive;
        public bool active;
    }

    [Header("References")]
    [SerializeField, Tooltip("비워두면 씬에서 자동으로 찾습니다.")]
    private GameFlowManager gameFlowManager;

    [Header("Zone Monsters")]
    [SerializeField, Tooltip("ZoneId별로 관리할 몬스터 목록입니다. 여기 넣은 몬스터가 우선 기준입니다.")]
    private ZoneMonsterGroup[] zoneMonsters;

    [SerializeField, Tooltip("켜면 Zone Monsters에 빠진 씬 몬스터를 Monster.ZoneId 기준으로 보조 수집합니다.")]
    private bool includeUnassignedSceneMonsters = true;

    [SerializeField, Tooltip("Zone Monsters 외에 강제로 포함할 몬스터입니다. 보통은 비워둡니다.")]
    private Monster[] additionalMonsters;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 매니저가 추적 중인 몬스터 수입니다.")]
    private int trackedMonsterCount;

    [SerializeField, Tooltip("Zone별 총 몬스터 수와 남은 몬스터 수입니다.")]
    private ZoneMonsterReadout[] zoneReadouts;

    [SerializeField] private bool showDebugLog = true;

    private Monster[] trackedMonsters = Array.Empty<Monster>();

    private void Awake()
    {
        ResolveReferences();
        RefreshMonsterList();
    }

    private void OnDestroy()
    {
        UnregisterMonsterEvents();
    }

    private void Reset()
    {
        ResolveReferences();
        EnsureZoneGroups();
    }

    private void OnValidate()
    {
        EnsureZoneGroups();
    }

    /// <summary>
    /// GameFlowManager가 자기 자신을 넘겨주며 초기화할 때 사용합니다.
    /// 씬 로드 순서가 달라도 같은 참조를 보도록 보정합니다.
    /// </summary>
    public void InitializeForGameFlow(GameFlowManager manager)
    {
        if (manager != null)
        {
            gameFlowManager = manager;
        }

        RefreshMonsterList();
    }

    /// <summary>
    /// 다음 Zone 바리게이트가 열리기 전에 호출합니다.
    /// 다음 Zone 몬스터를 미리 켜서 문 너머 전투 오브젝트가 준비되게 합니다.
    /// </summary>
    public void PrepareZone(ZoneId zoneId)
    {
        SetZoneMonstersActive(zoneId, true);
        UpdateReadouts();
    }

    /// <summary>
    /// 플레이어가 실제로 Zone에 진입했을 때 호출합니다.
    /// 몬스터가 없는 Zone이면 즉시 완료 알림을 보냅니다.
    /// </summary>
    public void BeginZone(ZoneId zoneId)
    {
        SetZoneMonstersActive(zoneId, true);
        NotifyClearedIfNoLivingMonsters(zoneId);
        UpdateReadouts();
    }

    /// <summary>
    /// 플레이어 사망 후 같은 Zone을 다시 시작할 때 호출합니다.
    /// 해당 Zone 몬스터를 씬 배치 포즈와 기본 체력으로 되돌립니다.
    /// </summary>
    public void RespawnZone(ZoneId zoneId)
    {
        Monster[] monsters = GetMonstersForZone(zoneId);
        for (int i = 0; i < monsters.Length; i++)
        {
            Monster monster = monsters[i];
            if (monster == null)
            {
                continue;
            }

            monster.ResetForRespawn();
        }

        NotifyClearedIfNoLivingMonsters(zoneId);
        UpdateReadouts();

        if (showDebugLog)
        {
            Debug.Log($"[MonsterManager] Respawned monsters in {zoneId}.", this);
        }
    }

    /// <summary>
    /// 다음 Zone으로 넘어간 뒤 이전 바리게이트가 닫혔을 때 호출합니다.
    /// 지나온 Zone 몬스터는 다시 렌더링/동작하지 않도록 비활성화합니다.
    /// </summary>
    public void DeactivateZone(ZoneId zoneId)
    {
        SetZoneMonstersActive(zoneId, false);
        UpdateReadouts();
    }

    private void ResolveReferences()
    {
        gameFlowManager ??= FindFirstObjectByType<GameFlowManager>();
    }

    private void RefreshMonsterList()
    {
        UnregisterMonsterEvents();
        EnsureZoneGroups();

        trackedMonsters = BuildTrackedMonsterList();
        trackedMonsterCount = trackedMonsters.Length;

        for (int i = 0; i < trackedMonsters.Length; i++)
        {
            Monster monster = trackedMonsters[i];
            if (monster == null)
            {
                continue;
            }

            monster.InitializeForManager();
            monster.Died -= OnMonsterDied;
            monster.Died += OnMonsterDied;
        }

        UpdateReadouts();
    }

    private void UnregisterMonsterEvents()
    {
        if (trackedMonsters == null)
        {
            return;
        }

        for (int i = 0; i < trackedMonsters.Length; i++)
        {
            if (trackedMonsters[i] != null)
            {
                trackedMonsters[i].Died -= OnMonsterDied;
            }
        }
    }

    private void OnMonsterDied(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        ZoneId zoneId = monster.ZoneId;
        UpdateReadouts();

        if (showDebugLog)
        {
            Debug.Log($"[MonsterManager] {monster.name} died in {zoneId}. Alive: {CountLivingMonsters(zoneId)}", monster);
        }

        NotifyClearedIfNoLivingMonsters(zoneId);
    }

    private void NotifyClearedIfNoLivingMonsters(ZoneId zoneId)
    {
        if (CountZoneMonsters(zoneId) > 0 && CountLivingMonsters(zoneId) > 0)
        {
            return;
        }

        GameFlowManager manager = gameFlowManager != null ? gameFlowManager : GameFlowManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning($"[MonsterManager] Cannot notify {zoneId} clear. GameFlowManager is missing.", this);
            return;
        }

        manager.NotifyZoneCleared(zoneId);
    }

    private void SetZoneMonstersActive(ZoneId zoneId, bool active)
    {
        Monster[] monsters = GetMonstersForZone(zoneId);
        for (int i = 0; i < monsters.Length; i++)
        {
            Monster monster = monsters[i];
            if (monster == null)
            {
                continue;
            }

            monster.SetManagedActive(active);
        }
    }

    private Monster[] GetMonstersForZone(ZoneId zoneId)
    {
        Monster[] groupMonsters = GetInspectorGroupMonsters(zoneId);
        if (!includeUnassignedSceneMonsters)
        {
            return groupMonsters;
        }

        Monster[] fallbackMonsters = GetTrackedMonstersByZone(zoneId);
        return MergeMonsters(groupMonsters, fallbackMonsters);
    }

    private Monster[] GetInspectorGroupMonsters(ZoneId zoneId)
    {
        if (zoneMonsters == null)
        {
            return Array.Empty<Monster>();
        }

        for (int i = 0; i < zoneMonsters.Length; i++)
        {
            ZoneMonsterGroup group = zoneMonsters[i];
            if (group != null && group.zoneId == zoneId)
            {
                return group.monsters ?? Array.Empty<Monster>();
            }
        }

        return Array.Empty<Monster>();
    }

    private Monster[] GetTrackedMonstersByZone(ZoneId zoneId)
    {
        if (trackedMonsters == null)
        {
            return Array.Empty<Monster>();
        }

        Monster[] result = new Monster[trackedMonsters.Length];
        int count = 0;
        for (int i = 0; i < trackedMonsters.Length; i++)
        {
            Monster monster = trackedMonsters[i];
            if (monster != null && monster.ZoneId == zoneId)
            {
                result[count++] = monster;
            }
        }

        Array.Resize(ref result, count);
        return result;
    }

    private int CountZoneMonsters(ZoneId zoneId)
    {
        return GetMonstersForZone(zoneId).Length;
    }

    private int CountLivingMonsters(ZoneId zoneId)
    {
        int count = 0;
        Monster[] monsters = GetMonstersForZone(zoneId);
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && !monsters[i].IsDead)
            {
                count++;
            }
        }

        return count;
    }

    private void UpdateReadouts()
    {
        Array zoneValues = Enum.GetValues(typeof(ZoneId));
        zoneReadouts = new ZoneMonsterReadout[zoneValues.Length];

        for (int i = 0; i < zoneValues.Length; i++)
        {
            ZoneId zoneId = (ZoneId)zoneValues.GetValue(i);
            zoneReadouts[i] = new ZoneMonsterReadout
            {
                zoneId = zoneId,
                total = CountZoneMonsters(zoneId),
                alive = CountLivingMonsters(zoneId),
                active = HasActiveMonster(zoneId)
            };
        }
    }

    private bool HasActiveMonster(ZoneId zoneId)
    {
        Monster[] monsters = GetMonstersForZone(zoneId);
        for (int i = 0; i < monsters.Length; i++)
        {
            Monster monster = monsters[i];
            if (monster != null && monster.gameObject.activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    private Monster[] BuildTrackedMonsterList()
    {
        Monster[] fromGroups = CollectMonstersFromGroups();
        Monster[] fromScene = includeUnassignedSceneMonsters
            ? FindObjectsByType<Monster>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            : Array.Empty<Monster>();

        return MergeMonsters(MergeMonsters(fromGroups, additionalMonsters), fromScene);
    }

    private Monster[] CollectMonstersFromGroups()
    {
        if (zoneMonsters == null)
        {
            return Array.Empty<Monster>();
        }

        Monster[] result = Array.Empty<Monster>();
        for (int i = 0; i < zoneMonsters.Length; i++)
        {
            ZoneMonsterGroup group = zoneMonsters[i];
            if (group == null)
            {
                continue;
            }

            result = MergeMonsters(result, group.monsters);
        }

        return result;
    }

    private void EnsureZoneGroups()
    {
        Array zoneValues = Enum.GetValues(typeof(ZoneId));
        if (zoneMonsters != null && zoneMonsters.Length == zoneValues.Length)
        {
            return;
        }

        ZoneMonsterGroup[] previousGroups = zoneMonsters ?? Array.Empty<ZoneMonsterGroup>();
        ZoneMonsterGroup[] nextGroups = new ZoneMonsterGroup[zoneValues.Length];

        for (int i = 0; i < zoneValues.Length; i++)
        {
            ZoneId zoneId = (ZoneId)zoneValues.GetValue(i);
            ZoneMonsterGroup existing = FindGroup(previousGroups, zoneId);
            nextGroups[i] = existing ?? new ZoneMonsterGroup
            {
                zoneId = zoneId,
                monsters = Array.Empty<Monster>()
            };
        }

        zoneMonsters = nextGroups;
    }

    private static ZoneMonsterGroup FindGroup(ZoneMonsterGroup[] groups, ZoneId zoneId)
    {
        if (groups == null)
        {
            return null;
        }

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].zoneId == zoneId)
            {
                return groups[i];
            }
        }

        return null;
    }

    private static Monster[] MergeMonsters(Monster[] first, Monster[] second)
    {
        first ??= Array.Empty<Monster>();
        second ??= Array.Empty<Monster>();

        Monster[] merged = new Monster[first.Length + second.Length];
        int count = 0;

        AppendUnique(first, merged, ref count);
        AppendUnique(second, merged, ref count);

        if (count == merged.Length)
        {
            return merged;
        }

        Array.Resize(ref merged, count);
        return merged;
    }

    private static void AppendUnique(Monster[] source, Monster[] target, ref int count)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            Monster monster = source[i];
            if (monster == null || Contains(target, count, monster))
            {
                continue;
            }

            target[count++] = monster;
        }
    }

    private static bool Contains(Monster[] monsters, int count, Monster monster)
    {
        for (int i = 0; i < count; i++)
        {
            if (monsters[i] == monster)
            {
                return true;
            }
        }

        return false;
    }
}
