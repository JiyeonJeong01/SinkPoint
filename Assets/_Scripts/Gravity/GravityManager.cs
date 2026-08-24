using System;
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

    public GravityPreset CurrentPreset { get; private set; }
    public GravityPreset InitialPreset => initialPreset;
    public GravityPreset TargetPreset { get; private set; }
    public Vector3 PresentationUp { get; private set; } = Vector3.up;
    public float TransitionProgress { get; private set; } = 1f;
    public bool IsTransitioning { get; private set; }
    public Vector3 Direction => gravityState != null ? gravityState.Direction : Vector3.down;
    public float Strength => gravityState != null ? gravityState.Strength : 0f;

    public event Action TransitionStarted;
    public event Action TransitionCompleted;

    private Vector3 transitionStartUp;
    private Vector3 transitionTargetUp;
    private Vector3 transitionAxis;
    private float transitionAngle;
    private float transitionElapsed;

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
            ApplyInitialPreset();
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
        if (IsTransitioning)
        {
            PresentationUp = transitionTargetUp;
            CompleteTransition();
        }
    }

    public bool ApplyPreset(GravityPreset preset)
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

        Vector3 targetUp = -preset.Direction;
        if (!IsFinite(targetUp) || targetUp.sqrMagnitude < Mathf.Epsilon)
        {
            Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an invalid direction.", preset);
            return false;
        }

        if (float.IsNaN(preset.Strength) || float.IsInfinity(preset.Strength) || preset.Strength < 0f)
        {
            Debug.LogError($"[GravityManager] GravityPreset '{preset.name}' has an invalid strength.", preset);
            return false;
        }

        targetUp.Normalize();
        bool gravityAlreadyMatches = gravityState.Direction == preset.Direction.normalized
            && Mathf.Approximately(gravityState.Strength, preset.Strength);
        bool presentationAlreadyMatches = Vector3.Dot(PresentationUp, targetUp) > 0.9999f;

        if (IsTransitioning && TargetPreset == preset && gravityAlreadyMatches)
        {
            return true;
        }

        if (!IsTransitioning
            && CurrentPreset == preset
            && gravityAlreadyMatches
            && presentationAlreadyMatches)
        {
            return true;
        }

        bool wasTransitioning = IsTransitioning;
        transitionStartUp = PresentationUp.normalized;
        transitionTargetUp = targetUp;
        transitionAngle = Vector3.Angle(transitionStartUp, transitionTargetUp);
        transitionAxis = GetTransitionAxis(transitionStartUp, transitionTargetUp);
        transitionElapsed = 0f;
        TransitionProgress = 0f;
        TargetPreset = preset;
        IsTransitioning = !presentationAlreadyMatches;

        if (IsTransitioning && !wasTransitioning)
        {
            TransitionStarted?.Invoke();
        }

        if (!gravityState.SetGravity(preset.Direction, preset.Strength))
        {
            AbortTransition(wasTransitioning || IsTransitioning);
            Debug.LogError($"[GravityManager] Failed to apply GravityPreset '{preset.name}'.", preset);
            return false;
        }

        CurrentPreset = preset;

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

            if (wasTransitioning)
            {
                TransitionCompleted?.Invoke();
            }
        }

        return true;
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

    private void ApplyInitialPreset()
    {
        if (!gravityState.SetGravity(initialPreset.Direction, initialPreset.Strength))
        {
            Debug.LogError($"[GravityManager] Failed to apply initial GravityPreset '{initialPreset.name}'.", initialPreset);
            return;
        }

        CurrentPreset = initialPreset;
        TargetPreset = null;
        PresentationUp = -gravityState.Direction;
        TransitionProgress = 1f;
        IsTransitioning = false;
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
