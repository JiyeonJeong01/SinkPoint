using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InGameHudCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("플레이어 HP를 표시할 Slider입니다. 비워두면 자식의 HP Slider를 찾습니다.")]
    private Slider hpSlider;
    [SerializeField, Tooltip("현재 Zone에 남은 몬스터 수를 표시할 Text입니다. 비워두면 자식의 Monster Count Text를 찾습니다.")]
    private Text monsterCountText;
    [SerializeField, Tooltip("주기 중력 변경 예고를 표시할 Text입니다. 비워두면 자식의 Gravity Warning Text를 찾습니다.")]
    private Text gravityWarningText;
    [SerializeField, Tooltip("비워두면 씬에서 PlayerHealth를 자동으로 찾습니다.")]
    private PlayerHealth playerHealth;
    [SerializeField, Tooltip("비워두면 씬의 GameFlowManager를 자동으로 찾습니다.")]
    private GameFlowManager gameFlowManager;
    [SerializeField, Tooltip("비워두면 씬의 MonsterManager를 자동으로 찾습니다.")]
    private MonsterManager monsterManager;
    [SerializeField, Tooltip("비워두면 씬의 GravityManager를 자동으로 찾습니다.")]
    private GravityManager gravityManager;

    [Header("Runtime Binding")]
    [SerializeField, Tooltip("씬에 따로 배치된 HUD가 플레이어/매니저를 런타임에 자동으로 찾게 합니다.")]
    private bool autoBindRuntimeReferences = true;
    [SerializeField, Min(0.1f), Tooltip("플레이어가 늦게 생성되는 경우 다시 찾는 간격입니다.")]
    private float retryBindInterval = 0.5f;
    [SerializeField, Min(0f), Tooltip("플레이어 HP 슬라이더가 목표 체력으로 따라가는 시간입니다.")]
    private float hpTweenDuration = 0.18f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("HUD가 현재 카운트를 읽고 있는 Zone입니다.")]
    private ZoneId monsterCountZone;
    [SerializeField, Tooltip("현재 Zone의 살아있는 몬스터 수입니다.")]
    private int aliveMonsterCount;
    [SerializeField, Tooltip("현재 Zone의 전체 몬스터 수입니다.")]
    private int totalMonsterCount;

    private PlayerHealth boundPlayerHealth;
    private Coroutine bindRoutine;
    private Tween hpTween;
    private int displayedCurrentHp = -1;
    private int displayedMaxHp = -1;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        BindRuntimeReferences();
        RefreshAll();
        RefreshGravityWarning();

        if (autoBindRuntimeReferences && bindRoutine == null)
        {
            bindRoutine = StartCoroutine(BindUntilReadyRoutine());
        }
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        UnbindRuntimeReferences();
        KillHpTween();
        SetGravityWarningVisible(false);
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public void BindPlayerHealth(PlayerHealth playerHealth)
    {
        if (boundPlayerHealth == playerHealth)
        {
            RefreshHp();
            return;
        }

        UnbindPlayerHealth();
        boundPlayerHealth = playerHealth;

        if (boundPlayerHealth != null)
        {
            boundPlayerHealth.Damaged += OnPlayerDamaged;
            boundPlayerHealth.Died += OnPlayerDied;
            boundPlayerHealth.Restored += OnPlayerRestored;
        }

        RefreshHp();
    }

    public void RefreshHp()
    {
        if (boundPlayerHealth != null)
        {
            SetHp(boundPlayerHealth.CurrentHealth, boundPlayerHealth.MaxHealth);
            return;
        }

        // 씬 오브젝트 활성화 순서상 플레이어를 아직 못 찾은 첫 프레임에는
        // 프리팹의 기본 풀피 표시를 0으로 덮어쓰지 않습니다.
        if (hpSlider != null && hpSlider.maxValue <= 0f)
        {
            SetHp(1, 1);
        }
    }

    public void SetHp(int currentHp, int maxHp)
    {
        SetHp(currentHp, maxHp, false);
    }

    private void SetHp(int currentHp, int maxHp, bool animate)
    {
        if (hpSlider != null)
        {
            int safeMaxHp = Mathf.Max(1, maxHp);
            int safeCurrentHp = Mathf.Clamp(currentHp, 0, safeMaxHp);
            if (displayedCurrentHp == safeCurrentHp && displayedMaxHp == safeMaxHp)
            {
                return;
            }

            displayedCurrentHp = safeCurrentHp;
            displayedMaxHp = safeMaxHp;
            hpSlider.maxValue = safeMaxHp;

            if (!animate || hpTweenDuration <= 0f)
            {
                KillHpTween();
                hpSlider.value = safeCurrentHp;
                return;
            }

            KillHpTween();
            hpTween = hpSlider
                .DOValue(safeCurrentHp, hpTweenDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(this);
        }
    }

    public void SetRemainingMonsterCount(int count)
    {
        if (monsterCountText != null)
        {
            monsterCountText.text = $"MONSTERS {Mathf.Max(0, count)}";
        }
    }

    private void ResolveReferences()
    {
        if (hpSlider == null)
        {
            hpSlider = FindSliderByName("HP Slider");
            hpSlider ??= GetComponentInChildren<Slider>(true);
        }

        if (monsterCountText == null)
        {
            monsterCountText = FindTextByName("Monster Count Text");
        }

        if (gravityWarningText == null)
        {
            gravityWarningText = FindTextByName("Gravity Warning Text");
        }
    }

    private void BindRuntimeReferences()
    {
        ResolveReferences();

        playerHealth ??= FindFirstObjectByType<PlayerHealth>();
        gameFlowManager ??= GameFlowManager.Instance != null
            ? GameFlowManager.Instance
            : FindFirstObjectByType<GameFlowManager>();
        monsterManager ??= FindFirstObjectByType<MonsterManager>();
        gravityManager ??= FindFirstObjectByType<GravityManager>();

        BindPlayerHealth(playerHealth);
        BindGameFlowManager(gameFlowManager);
        BindMonsterManager(monsterManager);
        BindGravityManager(gravityManager);
    }

    private IEnumerator BindUntilReadyRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(retryBindInterval);
        while (enabled)
        {
            if (boundPlayerHealth == null
                || gameFlowManager == null
                || monsterManager == null
                || gravityManager == null)
            {
                BindRuntimeReferences();
            }

            RefreshAll();
            yield return wait;
        }
    }

    private void BindGameFlowManager(GameFlowManager manager)
    {
        if (gameFlowManager != null)
        {
            gameFlowManager.CurrentZoneChanged -= OnCurrentZoneChanged;
        }

        gameFlowManager = manager;
        if (gameFlowManager != null)
        {
            gameFlowManager.CurrentZoneChanged -= OnCurrentZoneChanged;
            gameFlowManager.CurrentZoneChanged += OnCurrentZoneChanged;
        }
    }

    private void BindMonsterManager(MonsterManager manager)
    {
        if (monsterManager != null)
        {
            monsterManager.ZoneMonsterCountChanged -= OnZoneMonsterCountChanged;
        }

        monsterManager = manager;
        if (monsterManager != null)
        {
            monsterManager.ZoneMonsterCountChanged -= OnZoneMonsterCountChanged;
            monsterManager.ZoneMonsterCountChanged += OnZoneMonsterCountChanged;
        }
    }

    private void BindGravityManager(GravityManager manager)
    {
        if (gravityManager != null)
        {
            gravityManager.GravityChangeWarning -= OnGravityChangeWarning;
        }

        gravityManager = manager;
        if (gravityManager != null)
        {
            gravityManager.GravityChangeWarning -= OnGravityChangeWarning;
            gravityManager.GravityChangeWarning += OnGravityChangeWarning;
        }

        RefreshGravityWarning();
    }

    private void RefreshAll()
    {
        RefreshHp();
        RefreshMonsterCount();
    }

    private void Update()
    {
        if (gravityWarningText != null
            && gravityWarningText.gameObject.activeSelf
            && (gravityManager == null || !gravityManager.IsWarningActive))
        {
            SetGravityWarningVisible(false);
        }
    }

    private void RefreshGravityWarning()
    {
        if (gravityManager == null || !gravityManager.IsWarningActive)
        {
            SetGravityWarningVisible(false);
            return;
        }

        ShowGravityWarning(gravityManager.NextPeriodicDirection);
    }

    private void RefreshMonsterCount()
    {
        if (monsterManager == null || gameFlowManager == null)
        {
            aliveMonsterCount = 0;
            totalMonsterCount = 0;
            SetRemainingMonsterCount(0);
            return;
        }

        monsterCountZone = gameFlowManager.CurrentZone;
        aliveMonsterCount = monsterManager.GetAliveMonsterCount(monsterCountZone);
        totalMonsterCount = monsterManager.GetTotalMonsterCount(monsterCountZone);
        SetRemainingMonsterCount(aliveMonsterCount);
    }

    private void UnbindPlayerHealth()
    {
        if (boundPlayerHealth == null)
        {
            return;
        }

        boundPlayerHealth.Damaged -= OnPlayerDamaged;
        boundPlayerHealth.Died -= OnPlayerDied;
        boundPlayerHealth.Restored -= OnPlayerRestored;
        boundPlayerHealth = null;
    }

    private void UnbindRuntimeReferences()
    {
        UnbindPlayerHealth();

        if (gameFlowManager != null)
        {
            gameFlowManager.CurrentZoneChanged -= OnCurrentZoneChanged;
        }

        if (monsterManager != null)
        {
            monsterManager.ZoneMonsterCountChanged -= OnZoneMonsterCountChanged;
        }

        if (gravityManager != null)
        {
            gravityManager.GravityChangeWarning -= OnGravityChangeWarning;
        }
    }

    private void OnPlayerDamaged(PlayerHealth playerHealth, int amount)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void OnPlayerDied(PlayerHealth playerHealth)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void OnPlayerRestored(PlayerHealth playerHealth)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void KillHpTween()
    {
        if (hpTween == null)
        {
            return;
        }

        hpTween.Kill();
        hpTween = null;
    }

    private void OnCurrentZoneChanged(ZoneId zoneId)
    {
        RefreshMonsterCount();
    }

    private void OnZoneMonsterCountChanged(ZoneId zoneId, int alive, int total)
    {
        if (gameFlowManager == null || zoneId != gameFlowManager.CurrentZone)
        {
            return;
        }

        monsterCountZone = zoneId;
        aliveMonsterCount = alive;
        totalMonsterCount = total;
        SetRemainingMonsterCount(alive);
    }

    private void OnGravityChangeWarning(
        GravityPreset preset,
        Vector3 nextDirection,
        float warningDuration)
    {
        ShowGravityWarning(nextDirection);
    }

    private void ShowGravityWarning(Vector3 nextDirection)
    {
        ResolveReferences();
        if (gravityWarningText == null)
        {
            return;
        }

        gravityWarningText.text = $"GRAVITY SHIFT → {FormatAxis(nextDirection)}";
        SetGravityWarningVisible(true);
    }

    private void SetGravityWarningVisible(bool visible)
    {
        if (gravityWarningText != null && gravityWarningText.gameObject.activeSelf != visible)
        {
            gravityWarningText.gameObject.SetActive(visible);
        }
    }

    private static string FormatAxis(Vector3 direction)
    {
        Vector3 normalized = direction.normalized;
        float x = Mathf.Abs(normalized.x);
        float y = Mathf.Abs(normalized.y);
        float z = Mathf.Abs(normalized.z);

        if (x >= y && x >= z)
        {
            return normalized.x >= 0f ? "+X" : "-X";
        }

        if (y >= z)
        {
            return normalized.y >= 0f ? "+Y" : "-Y";
        }

        return normalized.z >= 0f ? "+Z" : "-Z";
    }

    private Slider FindSliderByName(string objectName)
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] != null && sliders[i].name == objectName)
            {
                return sliders[i];
            }
        }

        return null;
    }

    private Text FindTextByName(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Text hpLabel, Text monsterCountLabel)
    {
        monsterCountText = monsterCountLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
    }

    public void EditorConfigure(Slider slider, Text monsterCountLabel)
    {
        hpSlider = slider;
        monsterCountText = monsterCountLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
    }
#endif
}
