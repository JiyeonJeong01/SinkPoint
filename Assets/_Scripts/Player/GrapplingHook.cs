using UnityEngine;

[DefaultExecutionOrder(110)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput), typeof(PlayerController), typeof(PlayerHealth))]
public sealed class GrapplingHook : MonoBehaviour
{
    private enum GrappleState
    {
        Idle,
        Launching,
        Pulling,
    }

    private enum GrappleEndReason
    {
        None,
        Release,
        Miss,
        Arrived,
        Timeout,
        InputBlocked,
        GravityTransition,
        Died,
        TargetInvalid,
        Disabled,
    }

    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ThirdPersonCameraController aimCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer grappleLine;

    [Header("Grapple Settings")]
    [SerializeField, Min(0f)] private float maxGrappleRange = 45f;
    [SerializeField, Min(0.01f)] private float hookLaunchSpeed = 40f;
    [SerializeField, Min(0f)] private float maxPullDuration = 3f;
    [SerializeField] private LayerMask grappleSurfaceMask;

    [Header("Runtime State")]
    [SerializeField] private GrappleState grappleState;
    [SerializeField] private bool lastLaunchHadValidAnchor;
    [SerializeField] private Collider targetCollider;
    [SerializeField] private Vector3 anchorPoint;
    [SerializeField] private Vector3 surfaceNormal;
    [SerializeField] private float launchDistance;
    [SerializeField] private float launchProgress;
    [SerializeField] private float pullElapsed;
    [SerializeField] private GrappleEndReason lastEndReason;

    private readonly RaycastHit[] raycastHits = new RaycastHit[16];
    private Vector3 launchOrigin;
    private Vector3 launchEndpoint;
    private double launchStartedAt;

    internal bool IsBusy => grappleState != GrappleState.Idle;

    private void Awake()
    {
        input ??= GetComponent<PlayerInput>();
        playerController ??= GetComponent<PlayerController>();
        playerHealth ??= GetComponent<PlayerHealth>();
        aimCamera ??= FindFirstObjectByType<ThirdPersonCameraController>();
        muzzle ??= FindChildTransform("MuzzleVfxAnchor") ?? FindChildTransform("Muzzle");
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }
    }

    private void Start()
    {
        if (input == null || playerController == null || playerHealth == null || aimCamera == null || muzzle == null || grappleLine == null)
        {
            Debug.LogError(
                $"{nameof(GrapplingHook)} on '{name}' requires Input, Player Controller, Player Health, Aim Camera, Muzzle, and Grapple Line references.",
                this);
            enabled = false;
            return;
        }

        grappleLine.positionCount = 2;
        grappleLine.useWorldSpace = true;
        grappleLine.enabled = false;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        EndGrapple(GrappleEndReason.Disabled);
    }

    private void LateUpdate()
    {
        if (grappleState == GrappleState.Idle)
        {
            if (input.GrapplePressed && input.AllowGrapple && !playerHealth.IsDead && !playerController.IsGravityTransitioning)
            {
                BeginLaunch();
            }

            return;
        }

        if (!input.GrappleHeld)
        {
            EndGrapple(input.AllowGrapple ? GrappleEndReason.Release : GrappleEndReason.InputBlocked);
            return;
        }

        if (playerHealth.IsDead)
        {
            EndGrapple(GrappleEndReason.Died);
            return;
        }

        if (playerController.IsGravityTransitioning)
        {
            EndGrapple(GrappleEndReason.GravityTransition);
            return;
        }

        if (grappleState == GrappleState.Launching)
        {
            UpdateLaunch();
            return;
        }

        UpdatePull();
    }

    private void BeginLaunch()
    {
        launchOrigin = muzzle.position;
        TryGetLaunchTarget(launchOrigin, out launchEndpoint, out targetCollider, out anchorPoint, out surfaceNormal, out lastLaunchHadValidAnchor);
        launchDistance = Vector3.Distance(launchOrigin, launchEndpoint);
        launchProgress = 0f;
        pullElapsed = 0f;
        launchStartedAt = Time.timeAsDouble;
        grappleState = GrappleState.Launching;
        grappleLine.enabled = true;
        SetLinePositions(muzzle.position, launchOrigin);
    }

    private void UpdateLaunch()
    {
        float elapsed = (float)(Time.timeAsDouble - launchStartedAt);
        launchProgress = launchDistance <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(elapsed * hookLaunchSpeed / launchDistance);
        Vector3 head = Vector3.Lerp(launchOrigin, launchEndpoint, launchProgress);
        SetLinePositions(muzzle.position, head);

        if (launchProgress < 1f)
        {
            return;
        }

        if (!lastLaunchHadValidAnchor)
        {
            EndGrapple(GrappleEndReason.Miss);
            return;
        }

        if (targetCollider == null || !targetCollider.gameObject.activeInHierarchy)
        {
            EndGrapple(GrappleEndReason.TargetInvalid);
            return;
        }

        if (!playerController.TryBeginGrapplePull(anchorPoint, surfaceNormal))
        {
            EndGrapple(GrappleEndReason.GravityTransition);
            return;
        }

        pullElapsed = 0f;
        grappleState = GrappleState.Pulling;
    }

    private void UpdatePull()
    {
        if (targetCollider == null || !targetCollider.gameObject.activeInHierarchy)
        {
            EndGrapple(GrappleEndReason.TargetInvalid);
            return;
        }

        pullElapsed += Time.deltaTime;
        SetLinePositions(muzzle.position, anchorPoint);
        if (pullElapsed >= maxPullDuration)
        {
            EndGrapple(GrappleEndReason.Timeout);
            return;
        }

        if (!playerController.IsGrapplePullActive)
        {
            EndGrapple(GrappleEndReason.Arrived);
        }
    }

    private void TryGetLaunchTarget(
        Vector3 origin,
        out Vector3 endpoint,
        out Collider hitCollider,
        out Vector3 hitPoint,
        out Vector3 hitNormal,
        out bool isValidAnchor)
    {
        Ray centerRay = aimCamera.CreateCenterRay();
        Vector3 aimPoint = centerRay.origin + centerRay.direction * maxGrappleRange;
        if (TryGetNearestNonPlayerHit(centerRay.origin, centerRay.direction, maxGrappleRange, out RaycastHit centerHit))
        {
            aimPoint = centerHit.point;
        }

        Vector3 toAimPoint = aimPoint - origin;
        float distance = Mathf.Min(maxGrappleRange, toAimPoint.magnitude);
        Vector3 direction = distance > Mathf.Epsilon ? toAimPoint / toAimPoint.magnitude : muzzle.forward;
        endpoint = origin + direction * distance;
        hitCollider = null;
        hitPoint = endpoint;
        hitNormal = Vector3.zero;
        isValidAnchor = false;

        if (!TryGetNearestNonPlayerHit(origin, direction, distance, out RaycastHit muzzleHit))
        {
            return;
        }

        endpoint = muzzleHit.point;
        hitCollider = muzzleHit.collider;
        hitPoint = muzzleHit.point;
        hitNormal = muzzleHit.normal;
        isValidAnchor = IsValidAnchor(muzzleHit.collider);
    }

    private bool TryGetNearestNonPlayerHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            raycastHits,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            nearestHit = hit;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    private bool IsValidAnchor(Collider hitCollider)
    {
        return hitCollider != null
            && hitCollider.attachedRigidbody == null
            && (grappleSurfaceMask.value & (1 << hitCollider.gameObject.layer)) != 0;
    }

    private Transform FindChildTransform(string childName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == childName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private void SetLinePositions(Vector3 start, Vector3 end)
    {
        grappleLine.SetPosition(0, start);
        grappleLine.SetPosition(1, end);
    }

    private void OnPlayerDied(PlayerHealth health)
    {
        EndGrapple(GrappleEndReason.Died);
    }

    private void EndGrapple(GrappleEndReason reason)
    {
        playerController?.CancelGrapplePull();
        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        grappleState = GrappleState.Idle;
        pullElapsed = 0f;
        lastEndReason = reason;
    }
}
