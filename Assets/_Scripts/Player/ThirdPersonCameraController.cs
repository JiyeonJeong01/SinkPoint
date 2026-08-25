using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraController : MonoBehaviour
{
    private enum CameraCompositionPreset
    {
        Centered,
        ShoulderGameplay,
    }

    [System.Serializable]
    private struct CameraCompositionSettings
    {
        [SerializeField] public float pivotHeight;
        [SerializeField] public Vector2 cameraLocalOffset;
        [SerializeField, Min(0f)] public float defaultDistance;
        [SerializeField, Min(1f)] public float fieldOfView;

        public float PivotHeight => pivotHeight;
        public Vector2 CameraLocalOffset => cameraLocalOffset;
        public float DefaultDistance => defaultDistance;
        public float FieldOfView => fieldOfView;

        public bool Matches(CameraCompositionSettings other)
        {
            return Mathf.Approximately(pivotHeight, other.pivotHeight)
                && cameraLocalOffset == other.cameraLocalOffset
                && Mathf.Approximately(defaultDistance, other.defaultDistance)
                && Mathf.Approximately(fieldOfView, other.fieldOfView);
        }
    }

    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GravityState gravityState;
    [SerializeField] private GravityManager gravityManager;

    [Header("Composition")]
    [SerializeField] private CameraCompositionPreset compositionPreset = CameraCompositionPreset.ShoulderGameplay;
    [SerializeField] private CameraCompositionSettings centeredComposition = new CameraCompositionSettings
    {
        pivotHeight = 1.03f,
        cameraLocalOffset = Vector2.zero,
        defaultDistance = 1.605f,
        fieldOfView = 60f,
    };
    [SerializeField] private CameraCompositionSettings shoulderGameplayComposition = new CameraCompositionSettings
    {
        pivotHeight = 1.03f,
        cameraLocalOffset = new Vector2(0.35f, 0f),
        defaultDistance = 1.605f,
        fieldOfView = 60f,
    };

    [Header("Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Distance")]
    [SerializeField, Min(0f)] private float minDistance = 0.6f;
    [SerializeField, Min(0f)] private float maxDistance = 3f;
    [SerializeField, Min(0f)] private float zoomStep = 0.25f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField, Min(0f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.05f;
    [SerializeField, Min(0f)] private float collisionReturnSpeed = 8f;

    [Header("Tween")]
    [SerializeField, Min(0f)] private float zoomDuration = 0.15f;
    [SerializeField, Min(0f)] private float collisionReturnDuration = 0.2f;
    [SerializeField] private Ease distanceEase = Ease.OutCubic;

    private readonly RaycastHit[] collisionHits = new RaycastHit[16];

    private float pitch;
    private float userDistance;
    private float displayedDistance;
    private CameraCompositionSettings activeComposition;
    private CameraCompositionPreset appliedCompositionPreset;
    private bool hasAppliedComposition;
    private bool wasCollisionLimited;
    private bool didWarnAboutHitBuffer;
    private Tween distanceTween;
    private Camera gameplayCamera;
    private Vector3 gravityUp;
    private Vector3 orbitForward;
    private bool gravityTransitionActive;
    private Vector3 transitionViewForward;

    internal float PitchDegrees => pitch;

    private void Awake()
    {
        cameraPivot ??= transform.Find("CameraPivot");
        cameraTransform ??= cameraPivot != null ? cameraPivot.Find("Main Camera") : null;
        gravityState ??= FindFirstObjectByType<GravityState>();
        gravityManager ??= FindFirstObjectByType<GravityManager>();
        gameplayCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
        pitch = cameraPivot != null ? NormalizeAngle(cameraPivot.localEulerAngles.x) : 0f;
        gravityUp = gravityState != null ? -gravityState.Direction : Vector3.up;
        orbitForward = GetPlanarForward(transform.forward, gravityUp);

        ApplySelectedComposition();
        ApplyCameraDistance();
    }

    private void Start()
    {
        if (input != null
            && target != null
            && cameraPivot != null
            && cameraTransform != null
            && gravityState != null
            && gameplayCamera != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(ThirdPersonCameraController)} on '{name}' requires Input, Target, Camera Pivot, Camera Transform, Gravity State, and Camera references.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        if (gravityState != null)
        {
            gravityState.Changed += OnGravityChanged;
        }

        if (gravityManager != null)
        {
            gravityManager.TransitionStarted += BeginGravityTransition;
            gravityManager.TransitionCompleted += EndGravityTransition;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (gravityState != null)
        {
            gravityState.Changed -= OnGravityChanged;
        }

        if (gravityManager != null)
        {
            gravityManager.TransitionStarted -= BeginGravityTransition;
            gravityManager.TransitionCompleted -= EndGravityTransition;
        }

        gravityTransitionActive = false;

        KillDistanceTween();

        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        ApplySelectedComposition();

        if (gravityTransitionActive && gravityManager != null)
        {
            gravityUp = gravityManager.PresentationUp.normalized;
            orbitForward = GetPlanarForward(transitionViewForward, gravityUp);
        }
        else
        {
            Vector3 currentUp = -gravityState.Direction;
            if (Vector3.Dot(gravityUp, currentUp) < 0.9999f)
            {
                RebuildOrbitBasis(currentUp);
            }

            orbitForward = Quaternion.AngleAxis(input.Look.x * mouseSensitivity, gravityUp) * orbitForward;
            orbitForward = GetPlanarForward(orbitForward, gravityUp);
            pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, minPitch, maxPitch);
        }

        transform.SetPositionAndRotation(
            target.position,
            Quaternion.LookRotation(orbitForward, gravityUp));
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        bool zoomChanged = UpdateUserDistance();
        float safeDistance = CalculateSafeDistance();
        bool isCollisionLimited = safeDistance < userDistance - Mathf.Epsilon;

        if (isCollisionLimited)
        {
            KillDistanceTween();

            if (safeDistance < displayedDistance)
            {
                displayedDistance = safeDistance;
            }
            else
            {
                displayedDistance = Mathf.MoveTowards(
                    displayedDistance,
                    safeDistance,
                    collisionReturnSpeed * Time.deltaTime);
            }
        }
        else if (zoomChanged)
        {
            StartDistanceTween(userDistance, zoomDuration);
        }
        else if (wasCollisionLimited)
        {
            StartDistanceTween(userDistance, collisionReturnDuration);
        }

        ApplyCameraDistance();
        wasCollisionLimited = isCollisionLimited;
    }

    private bool UpdateUserDistance()
    {
        if (Mathf.Approximately(input.CameraZoomDelta, 0f))
        {
            return false;
        }

        float lowerDistance = Mathf.Min(minDistance, maxDistance);
        float upperDistance = Mathf.Max(minDistance, maxDistance);
        float nextDistance = Mathf.Clamp(
            userDistance - input.CameraZoomDelta * zoomStep,
            lowerDistance,
            upperDistance);

        if (Mathf.Approximately(nextDistance, userDistance))
        {
            return false;
        }

        userDistance = nextDistance;
        return true;
    }

    private float CalculateSafeDistance()
    {
        Vector3 desiredLocalPosition = GetDesiredCameraLocalPosition(userDistance);
        float desiredTravelDistance = desiredLocalPosition.magnitude;
        if (desiredTravelDistance <= Mathf.Epsilon)
        {
            return 0f;
        }

        Vector3 desiredTravelDirection = cameraPivot.TransformDirection(desiredLocalPosition / desiredTravelDistance);

        int hitCount = Physics.SphereCastNonAlloc(
            cameraPivot.position,
            collisionRadius,
            desiredTravelDirection,
            collisionHits,
            desiredTravelDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == collisionHits.Length && !didWarnAboutHitBuffer)
        {
            Debug.LogWarning(
                $"{nameof(ThirdPersonCameraController)} on '{name}' filled its camera collision hit buffer.",
                this);
            didWarnAboutHitBuffer = true;
        }

        float safeTravelDistance = desiredTravelDistance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(target))
            {
                continue;
            }

            safeTravelDistance = Mathf.Min(safeTravelDistance, hit.distance - collisionPadding);
        }

        return Mathf.Clamp01(safeTravelDistance / desiredTravelDistance) * userDistance;
    }

    private void StartDistanceTween(float targetDistance, float duration)
    {
        KillDistanceTween();

        if (duration <= Mathf.Epsilon)
        {
            displayedDistance = targetDistance;
            return;
        }

        distanceTween = DOTween.To(
                () => displayedDistance,
                value => displayedDistance = value,
                targetDistance,
                duration)
            .SetEase(distanceEase)
            .OnComplete(() => distanceTween = null);
    }

    private void KillDistanceTween()
    {
        if (distanceTween == null)
        {
            return;
        }

        distanceTween.Kill();
        distanceTween = null;
    }

    private void ApplyCameraDistance()
    {
        if (cameraTransform == null)
        {
            return;
        }

        float displayedScale = userDistance > Mathf.Epsilon
            ? Mathf.Clamp01(displayedDistance / userDistance)
            : 1f;
        cameraTransform.localPosition = GetDesiredCameraLocalPosition(userDistance) * displayedScale;
    }

    private void ApplySelectedComposition()
    {
        CameraCompositionSettings selectedComposition = GetSelectedComposition();
        if (hasAppliedComposition
            && compositionPreset == appliedCompositionPreset
            && activeComposition.Matches(selectedComposition))
        {
            return;
        }

        activeComposition = selectedComposition;
        appliedCompositionPreset = compositionPreset;
        hasAppliedComposition = true;

        if (cameraPivot != null)
        {
            Vector3 pivotLocalPosition = cameraPivot.localPosition;
            pivotLocalPosition.y = activeComposition.PivotHeight;
            cameraPivot.localPosition = pivotLocalPosition;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.fieldOfView = activeComposition.FieldOfView;
        }

        float lowerDistance = Mathf.Min(minDistance, maxDistance);
        float upperDistance = Mathf.Max(minDistance, maxDistance);
        userDistance = Mathf.Clamp(activeComposition.DefaultDistance, lowerDistance, upperDistance);
        displayedDistance = userDistance;
        wasCollisionLimited = false;
        KillDistanceTween();
    }

    private CameraCompositionSettings GetSelectedComposition()
    {
        return compositionPreset == CameraCompositionPreset.Centered
            ? centeredComposition
            : shoulderGameplayComposition;
    }

    private Vector3 GetDesiredCameraLocalPosition(float distance)
    {
        return new Vector3(
            activeComposition.CameraLocalOffset.x,
            activeComposition.CameraLocalOffset.y,
            -distance);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void OnGravityChanged()
    {
        if (gravityManager != null && gravityManager.IsTransitioning)
        {
            return;
        }

        RebuildOrbitBasis(-gravityState.Direction);
    }

    private void BeginGravityTransition()
    {
        gravityTransitionActive = true;
        transitionViewForward = cameraTransform != null
            ? cameraTransform.forward
            : transform.forward;
    }

    private void EndGravityTransition()
    {
        gravityTransitionActive = false;

        if (gravityManager != null)
        {
            RebuildOrbitBasis(gravityManager.PresentationUp);
        }
    }

    private void RebuildOrbitBasis(Vector3 newUp)
    {
        gravityUp = newUp.normalized;
        Vector3 preservedForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        orbitForward = GetPlanarForward(preservedForward, gravityUp);
    }

    private Vector3 GetPlanarForward(Vector3 preferredForward, Vector3 up)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(preferredForward, up);
        if (planarForward.sqrMagnitude < Mathf.Epsilon && target != null)
        {
            planarForward = Vector3.ProjectOnPlane(target.forward, up);
        }

        if (planarForward.sqrMagnitude < Mathf.Epsilon)
        {
            Vector3 fallbackAxis = Mathf.Abs(Vector3.Dot(up, Vector3.forward)) < 0.99f
                ? Vector3.forward
                : Vector3.right;
            planarForward = Vector3.ProjectOnPlane(fallbackAxis, up);
        }

        return planarForward.normalized;
    }

    internal Ray CreateCenterRay()
    {
        return gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
    }
}
