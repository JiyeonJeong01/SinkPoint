using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GravityState gravityState;

    [Header("Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Distance")]
    [SerializeField, Min(0f)] private float defaultDistance = 1.605f;
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
    private bool wasCollisionLimited;
    private bool didWarnAboutHitBuffer;
    private Tween distanceTween;
    private Camera gameplayCamera;
    private Vector3 gravityUp;
    private Vector3 orbitForward;

    internal float PitchDegrees => pitch;

    private void Awake()
    {
        cameraPivot ??= transform.Find("CameraPivot");
        cameraTransform ??= cameraPivot != null ? cameraPivot.Find("Main Camera") : null;
        gravityState ??= FindFirstObjectByType<GravityState>();
        gameplayCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
        pitch = cameraPivot != null ? NormalizeAngle(cameraPivot.localEulerAngles.x) : 0f;
        gravityUp = gravityState != null ? -gravityState.Direction : Vector3.up;
        orbitForward = GetPlanarForward(transform.forward, gravityUp);

        float lowerDistance = Mathf.Min(minDistance, maxDistance);
        float upperDistance = Mathf.Max(minDistance, maxDistance);
        userDistance = Mathf.Clamp(defaultDistance, lowerDistance, upperDistance);
        displayedDistance = userDistance;
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
        Vector3 currentUp = -gravityState.Direction;
        if (Vector3.Dot(gravityUp, currentUp) < 0.9999f)
        {
            RebuildOrbitBasis(currentUp);
        }

        orbitForward = Quaternion.AngleAxis(input.Look.x * mouseSensitivity, gravityUp) * orbitForward;
        orbitForward = GetPlanarForward(orbitForward, gravityUp);
        pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, minPitch, maxPitch);

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
        if (userDistance <= Mathf.Epsilon)
        {
            return 0f;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            cameraPivot.position,
            collisionRadius,
            -cameraPivot.forward,
            collisionHits,
            userDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == collisionHits.Length && !didWarnAboutHitBuffer)
        {
            Debug.LogWarning(
                $"{nameof(ThirdPersonCameraController)} on '{name}' filled its camera collision hit buffer.",
                this);
            didWarnAboutHitBuffer = true;
        }

        float safeDistance = userDistance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(target))
            {
                continue;
            }

            safeDistance = Mathf.Min(safeDistance, hit.distance - collisionPadding);
        }

        return Mathf.Clamp(safeDistance, 0f, userDistance);
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

        Vector3 localPosition = cameraTransform.localPosition;
        localPosition.z = -displayedDistance;
        cameraTransform.localPosition = localPosition;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void OnGravityChanged()
    {
        RebuildOrbitBasis(-gravityState.Direction);
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
