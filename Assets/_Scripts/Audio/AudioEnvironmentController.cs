using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class AudioEnvironmentController : MonoBehaviour
{
    [Header("Shared Environment Settings")]
    [SerializeField] private AudioMixerSnapshot entrySnapshot;
    [SerializeField] private AudioMixerSnapshot caveSnapshot;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float zoneTransitionDuration = 0.4f;

    [Header("Player Reverb")]
    [SerializeField, Range(-10000f, 2000f)] private float entryReverbLevel = -10000f;
    [SerializeField, Tooltip("Zone02~05 진입 때 Player AudioReverbFilter에 적용할 런타임 목표값입니다. Filter의 고정 Preset 값이 아닙니다."), Range(-10000f, 2000f)]
    private float caveReverbLevel = -1200f;

    private GameFlowManager gameFlowManager;
    private AudioReverbFilter playerReverbFilter;
    private float reverbTransitionElapsed;
    private float reverbTransitionStart;
    private float reverbTransitionTarget;
    private bool reverbTransitionActive;
    private bool isStarted;
    private bool isSubscribed;

    /// <summary>
    /// Supplies scene-owned references to this shared AudioSystem instance.
    /// </summary>
    public void Configure(GameFlowManager configuredGameFlowManager, AudioReverbFilter configuredPlayerReverbFilter)
    {
        Unsubscribe();

        gameFlowManager = configuredGameFlowManager;
        playerReverbFilter = configuredPlayerReverbFilter;

        if (isStarted)
        {
            InitializeForConfiguredScene();
        }
    }

    private void Start()
    {
        isStarted = true;
        InitializeForConfiguredScene();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void InitializeForConfiguredScene()
    {
        if (!HasValidConfiguration())
        {
            Debug.LogError($"{nameof(AudioEnvironmentController)} on '{name}' is missing: {GetMissingConfigurationNames()}.", this);
            enabled = false;
            return;
        }

        Subscribe();
        ApplySnapshot(gameFlowManager.CurrentZone, 0f);
    }

    private bool HasValidConfiguration()
    {
        return gameFlowManager != null
            && entrySnapshot != null
            && caveSnapshot != null
            && playerReverbFilter != null;
    }

    private string GetMissingConfigurationNames()
    {
        string missing = string.Empty;
        AppendMissingName(ref missing, gameFlowManager, "Game Flow Manager");
        AppendMissingName(ref missing, entrySnapshot, "Entry Snapshot");
        AppendMissingName(ref missing, caveSnapshot, "Cave Snapshot");
        AppendMissingName(ref missing, playerReverbFilter, "Player Reverb Filter");
        return missing;
    }

    private static void AppendMissingName(ref string missing, Object reference, string displayName)
    {
        if (reference != null)
        {
            return;
        }

        missing += missing.Length == 0 ? displayName : $", {displayName}";
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        gameFlowManager.CurrentZoneChanged += OnCurrentZoneChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        gameFlowManager.CurrentZoneChanged -= OnCurrentZoneChanged;
        isSubscribed = false;
    }

    private void OnCurrentZoneChanged(ZoneId zoneId)
    {
        ApplySnapshot(zoneId, zoneTransitionDuration);
    }

    private void Update()
    {
        if (!reverbTransitionActive)
        {
            return;
        }

        if (zoneTransitionDuration <= 0f)
        {
            playerReverbFilter.reverbLevel = reverbTransitionTarget;
            reverbTransitionActive = false;
            return;
        }

        reverbTransitionElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(reverbTransitionElapsed / zoneTransitionDuration);
        playerReverbFilter.reverbLevel = Mathf.Lerp(
            reverbTransitionStart,
            reverbTransitionTarget,
            progress);
        reverbTransitionActive = progress < 1f;
    }

    private void ApplySnapshot(ZoneId zoneId, float transitionDuration)
    {
        AudioMixerSnapshot snapshot = UsesCaveEnvironment(zoneId)
            ? caveSnapshot
            : entrySnapshot;
        snapshot.TransitionTo(transitionDuration);

        reverbTransitionStart = playerReverbFilter.reverbLevel;
        reverbTransitionTarget = UsesCaveEnvironment(zoneId)
            ? caveReverbLevel
            : entryReverbLevel;

        if (transitionDuration <= 0f)
        {
            playerReverbFilter.reverbLevel = reverbTransitionTarget;
            reverbTransitionActive = false;
            return;
        }

        reverbTransitionElapsed = 0f;
        reverbTransitionActive = true;
    }

    private static bool UsesCaveEnvironment(ZoneId zoneId)
    {
        return zoneId == ZoneId.Zone02_Normal
            || zoneId == ZoneId.Zone03_GravityShift
            || zoneId == ZoneId.Zone04_Inversion
            || zoneId == ZoneId.Zone05_ZeroGravitySource;
    }
}
