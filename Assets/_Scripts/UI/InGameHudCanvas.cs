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
    [SerializeField, Tooltip("현재 장탄 수를 표시할 Text입니다. 비워두면 자식의 Ammo Count Text를 찾습니다.")]
    private Text ammoCountText;
    [SerializeField, Tooltip("중력 변경 경고를 담는 패널 RectTransform입니다. 비워두면 자식의 Gravity Warning Panel을 찾고, 없으면 Gravity Warning Text를 직접 움직입니다.")]
    private RectTransform gravityWarningPanel;
    [SerializeField, Tooltip("주기 중력 변경 예고를 표시할 Text입니다. 비워두면 자식의 Gravity Warning Text를 찾습니다.")]
    private Text gravityWarningText;
    [SerializeField, Tooltip("플레이어 피격 시 짧게 표시할 Image입니다. 비워두면 자식의 Hurt를 찾습니다.")]
    private Image hurtImage;
    [SerializeField, Tooltip("사망/리스폰 전환 때 화면을 덮을 검은 Fade Image입니다. 비워두면 자식의 Fade를 찾습니다.")]
    private Image fadeImage;
    [SerializeField, Tooltip("비워두면 씬에서 PlayerHealth를 자동으로 찾습니다.")]
    private PlayerHealth playerHealth;
    [SerializeField, Tooltip("비워두면 씬의 GameFlowManager를 자동으로 찾습니다.")]
    private GameFlowManager gameFlowManager;
    [SerializeField, Tooltip("비워두면 씬의 MonsterManager를 자동으로 찾습니다.")]
    private MonsterManager monsterManager;
    [SerializeField, Tooltip("비워두면 씬의 GravityManager를 자동으로 찾습니다.")]
    private GravityManager gravityManager;
    [SerializeField, Tooltip("비워두면 씬에서 PlayerCombatController를 자동으로 찾습니다.")]
    private PlayerCombatController playerCombatController;

    [Header("Runtime Binding")]
    [SerializeField, Tooltip("씬에 따로 배치된 HUD가 플레이어/매니저를 런타임에 자동으로 찾게 합니다.")]
    private bool autoBindRuntimeReferences = true;
    [SerializeField, Min(0.1f), Tooltip("플레이어가 늦게 생성되는 경우 다시 찾는 간격입니다.")]
    private float retryBindInterval = 0.5f;
    [SerializeField, Min(0f), Tooltip("플레이어 HP 슬라이더가 목표 체력으로 따라가는 시간입니다.")]
    private float hpTweenDuration = 0.18f;
    [SerializeField, Range(0, 255), Tooltip("피격 이미지의 최대 알파값입니다. 24면 아주 옅은 붉은 플래시입니다.")]
    private int hurtMaxAlpha = 24;
    [SerializeField, Min(0f), Tooltip("피격 이미지가 밝아지는 시간입니다.")]
    private float hurtFadeInDuration = 0.06f;
    [SerializeField, Min(0f), Tooltip("피격 이미지가 다시 사라지는 시간입니다.")]
    private float hurtFadeOutDuration = 0.2f;

    [Header("Gravity Warning Animation")]
    [SerializeField, Min(0f), Tooltip("중력 변경 경고 UI가 내려오는 시간입니다.")]
    private float gravityWarningSlideInDuration = 0.25f;
    [SerializeField, Min(0f), Tooltip("중력 변경 경고 UI가 올라가는 시간입니다.")]
    private float gravityWarningSlideOutDuration = 0.25f;
    [SerializeField, Min(0f), Tooltip("숨김 위치를 표시 위치보다 위로 얼마나 올릴지입니다.")]
    private float gravityWarningHiddenOffsetY = 96f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("HUD가 현재 카운트를 읽고 있는 Zone입니다.")]
    private ZoneId monsterCountZone;
    [SerializeField, Tooltip("현재 Zone의 살아있는 몬스터 수입니다.")]
    private int aliveMonsterCount;
    [SerializeField, Tooltip("현재 Zone의 전체 몬스터 수입니다.")]
    private int totalMonsterCount;

    private PlayerHealth boundPlayerHealth;
    private PlayerCombatController boundPlayerCombatController;
    private Coroutine bindRoutine;
    private Tween hpTween;
    private Tween hurtTween;
    private Tween fadeTween;
    private Tween gravityWarningTween;
    private RectTransform gravityWarningRectTransform;
    private Vector2 gravityWarningShownPosition;
    private bool gravityWarningPositionCaptured;
    private bool gravityChangeFeedbackVisible;
    private int displayedCurrentHp = -1;
    private int displayedMaxHp = -1;
    private int displayedCurrentRounds = -1;
    private int displayedMagazineCapacity = -1;

    private void Awake()
    {
        ResolveReferences();
        SetHurtAlpha(0f);
        SetFadeAlpha(0f);
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
        KillHurtTween();
        KillFadeTween();
        KillGravityWarningTween();
        SetHurtAlpha(0f);
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

    public void SetAmmoCount(int currentRounds, int magazineCapacity)
    {
        if (ammoCountText == null)
        {
            return;
        }

        int safeCapacity = Mathf.Max(1, magazineCapacity);
        int safeCurrentRounds = Mathf.Clamp(currentRounds, 0, safeCapacity);
        if (displayedCurrentRounds == safeCurrentRounds
            && displayedMagazineCapacity == safeCapacity)
        {
            return;
        }

        displayedCurrentRounds = safeCurrentRounds;
        displayedMagazineCapacity = safeCapacity;
        ammoCountText.text = $"{safeCurrentRounds} / {safeCapacity}";
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

        if (ammoCountText == null)
        {
            ammoCountText = FindTextByName("Ammo Count Text");
        }

        if (gravityWarningPanel == null)
        {
            gravityWarningPanel = FindRectTransformByName("Gravity Warning Panel");
        }

        if (gravityWarningText == null)
        {
            gravityWarningText = FindTextByName("Gravity Warning Text");
        }

        if (gravityWarningRectTransform == null)
        {
            gravityWarningRectTransform = gravityWarningPanel != null
                ? gravityWarningPanel
                : gravityWarningText != null
                    ? gravityWarningText.rectTransform
                    : null;
        }

        if (hurtImage == null)
        {
            hurtImage = FindImageByName("Hurt");
        }

        if (fadeImage == null)
        {
            fadeImage = FindImageByName("Fade");
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
        playerCombatController ??= FindFirstObjectByType<PlayerCombatController>();

        BindPlayerHealth(playerHealth);
        BindPlayerCombatController(playerCombatController);
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
                || boundPlayerCombatController == null
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

    private void BindPlayerCombatController(PlayerCombatController controller)
    {
        if (boundPlayerCombatController == controller)
        {
            RefreshAmmoCount();
            return;
        }

        if (boundPlayerCombatController != null)
        {
            boundPlayerCombatController.MagazineChanged -= OnMagazineChanged;
        }

        boundPlayerCombatController = controller;
        if (boundPlayerCombatController != null)
        {
            boundPlayerCombatController.MagazineChanged -= OnMagazineChanged;
            boundPlayerCombatController.MagazineChanged += OnMagazineChanged;
        }

        RefreshAmmoCount();
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
            gravityManager.GravityChangeFeedbackStarted -= OnGravityChangeFeedbackStarted;
        }

        gravityManager = manager;
        if (gravityManager != null)
        {
            gravityManager.GravityChangeWarning -= OnGravityChangeWarning;
            gravityManager.GravityChangeWarning += OnGravityChangeWarning;
            gravityManager.GravityChangeFeedbackStarted -= OnGravityChangeFeedbackStarted;
            gravityManager.GravityChangeFeedbackStarted += OnGravityChangeFeedbackStarted;
        }

        RefreshGravityWarning();
    }

    private void RefreshAll()
    {
        RefreshHp();
        RefreshMonsterCount();
        RefreshAmmoCount();
    }

    private void RefreshAmmoCount()
    {
        if (boundPlayerCombatController == null)
        {
            return;
        }

        SetAmmoCount(
            boundPlayerCombatController.CurrentRounds,
            boundPlayerCombatController.MagazineCapacity);
    }

    private void Update()
    {
        GameObject warningObject = GetGravityWarningObject();
        if (warningObject != null
            && warningObject.activeSelf
            && !gravityChangeFeedbackVisible
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

        ShowGravityWarning(gravityManager.CurrentPreset);
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

        if (boundPlayerCombatController != null)
        {
            boundPlayerCombatController.MagazineChanged -= OnMagazineChanged;
            boundPlayerCombatController = null;
        }

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
            gravityManager.GravityChangeFeedbackStarted -= OnGravityChangeFeedbackStarted;
        }
    }

    private void OnPlayerDamaged(PlayerHealth playerHealth, int amount)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
        PlayHurtFlash();
    }

    private void OnPlayerDied(PlayerHealth playerHealth)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void OnPlayerRestored(PlayerHealth playerHealth)
    {
        SetHp(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void OnMagazineChanged(int currentRounds, int magazineCapacity)
    {
        SetAmmoCount(currentRounds, magazineCapacity);
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

    public void PlayHurtFlash()
    {
        ResolveReferences();
        if (hurtImage == null)
        {
            return;
        }

        KillHurtTween();
        float targetAlpha = Mathf.Clamp01(hurtMaxAlpha / 255f);
        SetHurtAlpha(0f);

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);

        if (hurtFadeInDuration <= 0f)
        {
            sequence.AppendCallback(() => SetHurtAlpha(targetAlpha));
        }
        else
        {
            sequence.Append(hurtImage.DOFade(targetAlpha, hurtFadeInDuration));
        }

        if (hurtFadeOutDuration <= 0f)
        {
            sequence.AppendCallback(() => SetHurtAlpha(0f));
        }
        else
        {
            sequence.Append(hurtImage.DOFade(0f, hurtFadeOutDuration));
        }

        hurtTween = sequence;
    }

    private void SetHurtAlpha(float alpha)
    {
        if (hurtImage == null)
        {
            return;
        }

        Color color = hurtImage.color;
        color.a = Mathf.Clamp01(alpha);
        hurtImage.color = color;
    }

    private void KillHurtTween()
    {
        if (hurtTween == null)
        {
            return;
        }

        hurtTween.Kill();
        hurtTween = null;
    }

    /// <summary>
    /// 플레이어 사망 리스폰 중 화면을 지정한 알파까지 페이드합니다.
    /// Time.timeScale 변경 중에도 동작하도록 unscaled time으로 재생합니다.
    /// </summary>
    public IEnumerator FadeScreenRoutine(float targetAlpha, float duration)
    {
        ResolveReferences();
        if (fadeImage == null)
        {
            yield break;
        }

        KillFadeTween();
        fadeImage.gameObject.SetActive(true);

        if (duration <= 0f)
        {
            SetFadeAlpha(targetAlpha);
            yield break;
        }

        fadeTween = fadeImage
            .DOFade(Mathf.Clamp01(targetAlpha), duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetTarget(this);

        yield return fadeTween.WaitForCompletion();
        fadeTween = null;

        if (targetAlpha <= 0f)
        {
            SetFadeAlpha(0f);
        }
    }

    public void SetFadeAlpha(float alpha)
    {
        ResolveReferences();
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(color.a > 0f);
    }

    private void KillFadeTween()
    {
        if (fadeTween == null)
        {
            return;
        }

        fadeTween.Kill();
        fadeTween = null;
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
        ShowGravityWarning(preset);
    }

    private void OnGravityChangeFeedbackStarted(
        GravityPreset preset,
        Vector3 direction,
        float visibleDuration)
    {
        ShowGravityChangeFeedback(preset, visibleDuration);
    }

    private void ShowGravityWarning(GravityPreset preset)
    {
        ResolveReferences();
        if (gravityWarningText == null && GetGravityWarningObject() == null)
        {
            return;
        }

        if (gravityWarningText != null)
        {
            gravityWarningText.text = GetGravityFeedbackText(preset);
        }

        SetGravityWarningVisible(true);
    }

    private void ShowGravityChangeFeedback(GravityPreset preset, float visibleDuration)
    {
        ResolveReferences();
        if (gravityWarningText == null && GetGravityWarningObject() == null)
        {
            return;
        }

        CaptureGravityWarningPosition();
        KillGravityWarningTween();

        if (gravityWarningText != null)
        {
            gravityWarningText.text = GetGravityFeedbackText(preset);
        }

        gravityChangeFeedbackVisible = true;
        SetGravityWarningVisible(true);

        if (gravityWarningRectTransform == null)
        {
            gravityWarningTween = DOVirtual.DelayedCall(
                    Mathf.Max(0f, visibleDuration),
                    HideGravityChangeFeedback)
                .SetUpdate(true)
                .SetTarget(this);
            return;
        }

        Vector2 hiddenPosition = gravityWarningShownPosition
            + Vector2.up * gravityWarningHiddenOffsetY;
        gravityWarningRectTransform.anchoredPosition = hiddenPosition;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(this);

        if (gravityWarningSlideInDuration <= 0f)
        {
            sequence.AppendCallback(() => gravityWarningRectTransform.anchoredPosition = gravityWarningShownPosition);
        }
        else
        {
            sequence.Append(gravityWarningRectTransform
                .DOAnchorPos(gravityWarningShownPosition, gravityWarningSlideInDuration)
                .SetEase(Ease.OutCubic));
        }

        sequence.AppendInterval(Mathf.Max(0f, visibleDuration));

        if (gravityWarningSlideOutDuration <= 0f)
        {
            sequence.AppendCallback(() => gravityWarningRectTransform.anchoredPosition = hiddenPosition);
        }
        else
        {
            sequence.Append(gravityWarningRectTransform
                .DOAnchorPos(hiddenPosition, gravityWarningSlideOutDuration)
                .SetEase(Ease.InCubic));
        }

        sequence.OnComplete(HideGravityChangeFeedback);
        gravityWarningTween = sequence;
    }

    private void CaptureGravityWarningPosition()
    {
        if (gravityWarningPositionCaptured || gravityWarningRectTransform == null)
        {
            return;
        }

        gravityWarningShownPosition = gravityWarningRectTransform.anchoredPosition;
        gravityWarningPositionCaptured = true;
    }

    private void HideGravityChangeFeedback()
    {
        gravityChangeFeedbackVisible = false;
        gravityWarningTween = null;
        SetGravityWarningVisible(false);
    }

    private void KillGravityWarningTween()
    {
        if (gravityWarningTween == null)
        {
            return;
        }

        gravityWarningTween.Kill();
        gravityWarningTween = null;
        gravityChangeFeedbackVisible = false;
    }

    private void SetGravityWarningVisible(bool visible)
    {
        GameObject warningObject = GetGravityWarningObject();
        if (warningObject != null && warningObject.activeSelf != visible)
        {
            warningObject.SetActive(visible);
        }
    }

    private GameObject GetGravityWarningObject()
    {
        if (gravityWarningPanel != null)
        {
            return gravityWarningPanel.gameObject;
        }

        return gravityWarningText != null ? gravityWarningText.gameObject : null;
    }

    private static string GetGravityFeedbackText(GravityPreset preset)
    {
        return preset != null && preset.Mode == GravityPresetMode.ZeroGravity
            ? "무중력 진입"
            : "중력 방향 전환";
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

    private RectTransform FindRectTransformByName(string objectName)
    {
        RectTransform[] transforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private Image FindImageByName(string objectName)
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == objectName)
            {
                return images[i];
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Text hpLabel, Text monsterCountLabel, Text ammoLabel = null)
    {
        monsterCountText = monsterCountLabel;
        ammoCountText = ammoLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
        SetAmmoCount(30, 30);
    }

    public void EditorConfigure(Slider slider, Text monsterCountLabel, Text ammoLabel = null)
    {
        hpSlider = slider;
        monsterCountText = monsterCountLabel;
        ammoCountText = ammoLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
        SetAmmoCount(30, 30);
    }
#endif
}
