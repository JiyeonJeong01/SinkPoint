using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class GravityManager : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private GravityState gravityState;
    [FormerlySerializedAs("initialZone")]
    [SerializeField] private GravityPreset initialPreset;

    [Header("Presentation Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.5f;

    [Header("Gravity Change Feedback")]
    [SerializeField, Range(0.01f, 1f)] private float gravityChangeTimeScale = 0.2f;
    [SerializeField, Range(0.01f, 1f)] private float periodicGravityChangeTimeScale = 0.5f;
    [SerializeField, Min(0f)] private float gravityChangeFeedbackDuration = 1.2f;
    [SerializeField, Min(0f)] private float distortionStopDelay = 0.2f;
    [SerializeField] private ParticleSystem distortionParticlePrefab;
    [SerializeField] private Transform distortionParticleAnchor;
    [SerializeField, Min(0f)] private float distortionParticleScaleMultiplier = 1f;

    public GravityPreset CurrentPreset { get; private set; }
    public GravityPreset InitialPreset => initialPreset;
    public GravityPreset TargetPreset { get; private set; }
    public Vector3 PresentationUp { get; private set; } = Vector3.up;
    public float TransitionProgress { get; private set; } = 1f;
    public bool IsTransitioning { get; private set; }
    public bool IsPeriodicRunning => periodicRoutine != null;
    public bool IsWarningActive { get; private set; }
    public Vector3 NextPeriodicDirection { get; private set; }
    public float SecondsUntilNextGravityChange { get; private set; }
    public Vector3 Direction => gravityState != null ? gravityState.Direction : Vector3.down;
    public float Strength => gravityState != null ? gravityState.Strength : 0f;

    public event Action TransitionStarted;
    public event Action TransitionCompleted;
    public event Action<GravityPreset, Vector3, float> GravityChangeWarning;
    public event Action<GravityPreset, Vector3, float> GravityChangeFeedbackStarted;

    private Vector3 transitionStartUp;
    private Vector3 transitionTargetUp;
    private Vector3 transitionAxis;
    private float transitionAngle;
    private float transitionElapsed;
    private Coroutine periodicRoutine;
    private Coroutine feedbackRoutine;
    private ParticleSystem distortionParticleInstance;
    private float timeScaleBeforeFeedback = 1f;
    private float fixedDeltaTimeBeforeFeedback = 0.02f;
    private int periodicRunId;
    private bool suppressFeedback;

    private void Awake()
    {
        gravityState ??= GetComponent<GravityState>();

        if (gravityState == null)
        {
            Debug.LogError($"{nameof(GravityManager)} on '{name}' requires a {nameof(GravityState)} reference.", this);
            enabled = false;
            return;
        }

        PresentationUp = -gravityState.Direction;

        if (initialPreset != null)
        {
            suppressFeedback = true;
            ApplyPreset(initialPreset);
            suppressFeedback = false;
        }
    }

    private void Update()
    {
        if (!IsTransitioning)
        {
            return;
        }

        transitionElapsed += Time.deltaTime;
        TransitionProgress = transitionDuration <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(transitionElapsed / transitionDuration);

        float easedProgress = Mathf.SmoothStep(0f, 1f, TransitionProgress);
        PresentationUp = (
            Quaternion.AngleAxis(transitionAngle * easedProgress, transitionAxis)
            * transitionStartUp).normalized;

        if (TransitionProgress >= 1f)
        {
            CompleteTransition();
        }
    }

    private void OnDisable()
    {
        StopGravityChangeFeedback(true);
        StopPeriodicRoutine();

        if (IsTransitioning)
        {
            PresentationUp = transitionTargetUp;
            CompleteTransition();
        }
    }

    public bool ApplyPreset(GravityPreset preset)
    {
        if (!TryValidatePreset(preset))
        {
            return false;
        }

        if (CurrentPreset == preset
            && preset.Mode == GravityPresetMode.Periodic
            && IsPeriodicRunning)
        {
            return true;
        }

        StopPeriodicRoutine();

        switch (preset.Mode)
        {
            case GravityPresetMode.Fixed:
                return ApplyGravityValue(preset, preset.Direction, preset.Strength, false);
            case GravityPresetMode.Periodic:
                return StartPeriodicPreset(preset, false);
            case GravityPresetMode.ZeroGravity:
                return ApplyGravityValue(preset, gravityState.Direction, 0f, false);
            default:
                Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an unsupported mode.", preset);
                return false;
        }
    }

    public bool RestoreCurrentPresetImmediately()
    {
        GravityPreset preset = CurrentPreset != null ? CurrentPreset : initialPreset;
        if (!TryValidatePreset(preset))
        {
            return false;
        }

        StopPeriodicRoutine();

        switch (preset.Mode)
        {
            case GravityPresetMode.Fixed:
                return ApplyGravityValue(preset, preset.Direction, preset.Strength, true);
            case GravityPresetMode.Periodic:
                return StartPeriodicPreset(preset, true);
            case GravityPresetMode.ZeroGravity:
                return ApplyGravityValue(preset, gravityState.Direction, 0f, true);
            default:
                Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an unsupported mode.", preset);
                return false;
        }
    }

    public bool RestoreInitialPreset()
    {
        if (initialPreset == null)
        {
            Debug.LogError("[GravityManager] Cannot restore a missing initial GravityPreset.", this);
            return false;
        }

        return ApplyPreset(initialPreset);
    }

    private bool StartPeriodicPreset(GravityPreset preset, bool instantPresentation)
    {
        Vector3 firstDirection = preset.GetPeriodicDirection(0);
        if (!ApplyGravityValue(preset, firstDirection, preset.Strength, instantPresentation))
        {
            return false;
        }

        int runId = ++periodicRunId;
        periodicRoutine = StartCoroutine(RunPeriodicPreset(preset, 0, runId));
        return true;
    }

    private IEnumerator RunPeriodicPreset(GravityPreset preset, int currentIndex, int runId)
    {
        while (enabled && CurrentPreset == preset && runId == periodicRunId)
        {
            int nextIndex = (currentIndex + 1) % preset.PeriodicDirectionCount;
            NextPeriodicDirection = preset.GetPeriodicDirection(nextIndex).normalized;
            SecondsUntilNextGravityChange = preset.ChangeInterval;
            IsWarningActive = false;

            float warningDelay = preset.ChangeInterval - preset.WarningDuration;
            yield return WaitForPeriodicDelay(warningDelay, runId);

            if (!CanContinuePeriodic(preset, runId))
            {
                break;
            }

            IsWarningActive = true;
            GravityChangeWarning?.Invoke(preset, NextPeriodicDirection, preset.WarningDuration);
            yield return WaitForPeriodicDelay(preset.WarningDuration, runId);

            if (!CanContinuePeriodic(preset, runId))
            {
                break;
            }

            IsWarningActive = false;
            SecondsUntilNextGravityChange = 0f;
            if (!ApplyGravityValue(preset, NextPeriodicDirection, preset.Strength, false))
            {
                break;
            }

            currentIndex = nextIndex;
        }

        if (runId == periodicRunId)
        {
            periodicRoutine = null;
            ResetPeriodicReadout();
        }
    }

    private IEnumerator WaitForPeriodicDelay(float duration, int runId)
    {
        float elapsed = 0f;
        while (elapsed < duration && runId == periodicRunId)
        {
            yield return null;
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            SecondsUntilNextGravityChange = Mathf.Max(
                0f,
                SecondsUntilNextGravityChange - deltaTime);
        }
    }

    private bool CanContinuePeriodic(GravityPreset preset, int runId)
    {
        return enabled
            && CurrentPreset == preset
            && runId == periodicRunId;
    }

    private void StopPeriodicRoutine()
    {
        periodicRunId++;

        if (periodicRoutine != null)
        {
            StopCoroutine(periodicRoutine);
            periodicRoutine = null;
        }

        ResetPeriodicReadout();
    }

    private void ResetPeriodicReadout()
    {
        IsWarningActive = false;
        NextPeriodicDirection = Vector3.zero;
        SecondsUntilNextGravityChange = 0f;
    }

    private bool ApplyGravityValue(
        GravityPreset preset,
        Vector3 direction,
        float strength,
        bool instantPresentation)
    {
        Vector3 targetUp = -direction;
        if (!IsFinite(targetUp) || targetUp.sqrMagnitude < Mathf.Epsilon)
        {
            Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an invalid direction.", preset);
            return false;
        }

        if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
        {
            Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an invalid strength.", preset);
            return false;
        }

        targetUp.Normalize();
        Vector3 normalizedDirection = direction.normalized;
        bool gravityAlreadyMatches = gravityState.Direction == normalizedDirection
            && Mathf.Approximately(gravityState.Strength, strength);
        bool presentationAlreadyMatches = Vector3.Dot(PresentationUp, targetUp) > 0.9999f;

        if (!instantPresentation
            && IsTransitioning
            && TargetPreset == preset
            && gravityAlreadyMatches)
        {
            return true;
        }

        if (!instantPresentation
            && !IsTransitioning
            && CurrentPreset == preset
            && gravityAlreadyMatches
            && presentationAlreadyMatches)
        {
            return true;
        }

        if (instantPresentation)
        {
            bool wasTransitioning = IsTransitioning;
            if (!gravityState.SetGravity(direction, strength))
            {
                Debug.LogError($"[GravityManager] Failed to apply GravityPreset '{preset.name}'.", preset);
                return false;
            }

            CurrentPreset = preset;
            TargetPreset = null;
            PresentationUp = targetUp;
            TransitionProgress = 1f;
            IsTransitioning = false;

            if (wasTransitioning)
            {
                TransitionCompleted?.Invoke();
            }

            return true;
        }

        bool wasTransitioningRegularly = IsTransitioning;
        transitionStartUp = PresentationUp.normalized;
        transitionTargetUp = targetUp;
        transitionAngle = Vector3.Angle(transitionStartUp, transitionTargetUp);
        transitionAxis = GetTransitionAxis(transitionStartUp, transitionTargetUp);
        transitionElapsed = 0f;
        TransitionProgress = 0f;
        TargetPreset = preset;
        IsTransitioning = !presentationAlreadyMatches;

        if (IsTransitioning && !wasTransitioningRegularly)
        {
            TransitionStarted?.Invoke();
        }

        if (!gravityState.SetGravity(direction, strength))
        {
            AbortTransition(wasTransitioningRegularly || IsTransitioning);
            Debug.LogError($"[GravityManager] Failed to apply GravityPreset '{preset.name}'.", preset);
            return false;
        }

        CurrentPreset = preset;
        StartGravityChangeFeedback(preset, normalizedDirection, instantPresentation);

        if (IsTransitioning && transitionDuration <= Mathf.Epsilon)
        {
            CompleteTransition();
            return true;
        }

        if (!IsTransitioning)
        {
            PresentationUp = transitionTargetUp;
            TransitionProgress = 1f;
            TargetPreset = null;

            if (wasTransitioningRegularly)
            {
                TransitionCompleted?.Invoke();
            }
        }

        return true;
    }

    private void StartGravityChangeFeedback(
        GravityPreset preset,
        Vector3 normalizedDirection,
        bool instantPresentation)
    {
        if (suppressFeedback || instantPresentation || !Application.isPlaying)
        {
            return;
        }

        StopGravityChangeFeedback(true);
        feedbackRoutine = StartCoroutine(RunGravityChangeFeedback(preset, normalizedDirection));
    }

    private IEnumerator RunGravityChangeFeedback(GravityPreset preset, Vector3 normalizedDirection)
    {
        timeScaleBeforeFeedback = Time.timeScale;
        fixedDeltaTimeBeforeFeedback = Time.fixedDeltaTime;

        float targetTimeScale = preset != null && preset.Mode == GravityPresetMode.Periodic
            ? periodicGravityChangeTimeScale
            : gravityChangeTimeScale;
        ApplyTimeScale(targetTimeScale);
        PlayDistortionParticle();
        GravityChangeFeedbackStarted?.Invoke(preset, normalizedDirection, gravityChangeFeedbackDuration);

        float elapsed = 0f;
        while (elapsed < gravityChangeFeedbackDuration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        ApplyTimeScale(timeScaleBeforeFeedback, fixedDeltaTimeBeforeFeedback);

        elapsed = 0f;
        while (elapsed < distortionStopDelay)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        StopDistortionParticle();
        feedbackRoutine = null;
    }

    private void StopGravityChangeFeedback(bool restoreTimeScale)
    {
        bool wasRunning = feedbackRoutine != null;
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (restoreTimeScale && wasRunning)
        {
            ApplyTimeScale(timeScaleBeforeFeedback, fixedDeltaTimeBeforeFeedback);
        }

        StopDistortionParticle();
    }

    private void ApplyTimeScale(float timeScale)
    {
        ApplyTimeScale(timeScale, fixedDeltaTimeBeforeFeedback * Mathf.Max(0.01f, timeScale));
    }

    private static void ApplyTimeScale(float timeScale, float fixedDeltaTime)
    {
        Time.timeScale = Mathf.Max(0f, timeScale);
        Time.fixedDeltaTime = Mathf.Max(0.0001f, fixedDeltaTime);
    }

    private void PlayDistortionParticle()
    {
        if (distortionParticlePrefab == null)
        {
            return;
        }

        if (distortionParticleInstance == null)
        {
            Transform anchor = distortionParticleAnchor != null ? distortionParticleAnchor : transform;
            distortionParticleInstance = Instantiate(
                distortionParticlePrefab,
                anchor.position,
                anchor.rotation,
                anchor);
        }

        distortionParticleInstance.gameObject.SetActive(true);
        distortionParticleInstance.transform.localPosition = Vector3.zero;
        distortionParticleInstance.transform.localRotation = Quaternion.identity;
        distortionParticleInstance.transform.localScale =
            distortionParticlePrefab.transform.localScale * distortionParticleScaleMultiplier;
        distortionParticleInstance.Play(true);
    }

    private void StopDistortionParticle()
    {
        if (distortionParticleInstance == null)
        {
            return;
        }

        distortionParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        distortionParticleInstance.gameObject.SetActive(false);
    }

    private bool TryValidatePreset(GravityPreset preset)
    {
        if (preset == null)
        {
            Debug.LogError("[GravityManager] Cannot apply a null GravityPreset.", this);
            return false;
        }

        if (gravityState == null)
        {
            Debug.LogError($"[GravityManager] Failed to apply GravityPreset '{preset.name}'.", preset);
            return false;
        }

        if (!preset.TryValidate(out string error))
        {
            Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' is invalid: {error}", preset);
            return false;
        }

        return true;
    }

    private void CompleteTransition()
    {
        PresentationUp = transitionTargetUp;
        TransitionProgress = 1f;
        IsTransitioning = false;
        TargetPreset = null;
        TransitionCompleted?.Invoke();
    }

    private void AbortTransition(bool notifyCompletion)
    {
        IsTransitioning = false;
        TargetPreset = null;
        TransitionProgress = 1f;

        if (notifyCompletion)
        {
            TransitionCompleted?.Invoke();
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.z);
    }

    private static Vector3 GetTransitionAxis(Vector3 startUp, Vector3 targetUp)
    {
        Vector3 axis = Vector3.Cross(startUp, targetUp);
        if (axis.sqrMagnitude >= Mathf.Epsilon)
        {
            return axis.normalized;
        }

        Vector3 fallback = Mathf.Abs(Vector3.Dot(startUp, Vector3.forward)) < 0.99f
            ? Vector3.forward
            : Vector3.right;
        return Vector3.Cross(startUp, fallback).normalized;
    }
}
