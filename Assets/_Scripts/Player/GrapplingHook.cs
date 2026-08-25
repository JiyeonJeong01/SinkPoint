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

    [Header("Debug")]
    [SerializeField] private bool logGrappleFailures = true;
    [SerializeField, TextArea] private string lastGrappleDebug;

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
    private const float MuzzlePathEndpointTolerance = 0.05f;
    private Vector3 launchOrigin;
    private Vector3 launchEndpoint;
    private double launchStartedAt;
    private string launchTargetDebug;

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
            launchTargetDebug = "Right mouse button is no longer held.";
            EndGrapple(input.AllowGrapple ? GrappleEndReason.Release : GrappleEndReason.InputBlocked);
            return;
        }

        if (playerHealth.IsDead)
        {
            launchTargetDebug = "PlayerHealth reports dead.";
            EndGrapple(GrappleEndReason.Died);
            return;
        }

        if (playerController.IsGravityTransitioning)
        {
            launchTargetDebug = "PlayerController is in gravity transition.";
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
            launchTargetDebug = $"Stored target became invalid before pull. target={FormatCollider(targetCollider)}";
            EndGrapple(GrappleEndReason.TargetInvalid);
            return;
        }

        if (!playerController.TryBeginGrapplePull(anchorPoint, surfaceNormal))
        {
            launchTargetDebug = $"PlayerController rejected pull. anchor={anchorPoint}, normal={surfaceNormal}";
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
            launchTargetDebug = $"Stored target became invalid during pull. target={FormatCollider(targetCollider)}";
            EndGrapple(GrappleEndReason.TargetInvalid);
            return;
        }

        pullElapsed += Time.deltaTime;
        SetLinePositions(muzzle.position, anchorPoint);
        if (pullElapsed >= maxPullDuration)
        {
            launchTargetDebug = $"Pull exceeded max duration. elapsed={pullElapsed:0.###}, max={maxPullDuration:0.###}";
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
        string centerHitDebug = "centerRayHit=None";
        bool hasCenterHit = TryGetNearestNonPlayerHit(centerRay.origin, centerRay.direction, maxGrappleRange, out RaycastHit centerHit);
        if (hasCenterHit)
        {
            aimPoint = centerHit.point;
            centerHitDebug = $"centerRayHit={FormatHit(centerHit)}";
        }

        Vector3 toAimPoint = aimPoint - origin;
        float distance = Mathf.Min(maxGrappleRange, toAimPoint.magnitude);
        Vector3 direction = distance > Mathf.Epsilon ? toAimPoint / toAimPoint.magnitude : muzzle.forward;
        endpoint = origin + direction * distance;
        hitCollider = null;
        hitPoint = endpoint;
        hitNormal = Vector3.zero;
        isValidAnchor = false;
        launchTargetDebug =
            $"{centerHitDebug}, muzzleOrigin={origin}, muzzleDirection={direction}, muzzleDistance={distance:0.###}";

        if (hasCenterHit && IsValidAnchor(centerHit.collider))
        {
            if (IsMuzzlePathClearToCenterHit(origin, centerHit, out RaycastHit blockingHit))
            {
                endpoint = centerHit.point;
                hitCollider = centerHit.collider;
                hitPoint = centerHit.point;
                hitNormal = centerHit.normal;
                isValidAnchor = true;
                launchTargetDebug += ", cameraAnchorAccepted=True";
                return;
            }

            launchTargetDebug += $", cameraAnchorBlockedBy={FormatHit(blockingHit)}";
        }

        if (!TryGetNearestNonPlayerHit(origin, direction, distance, out RaycastHit muzzleHit))
        {
            launchTargetDebug += ", muzzleRayHit=None";
            return;
        }

        endpoint = muzzleHit.point;
        hitCollider = muzzleHit.collider;
        hitPoint = muzzleHit.point;
        hitNormal = muzzleHit.normal;
        isValidAnchor = IsValidAnchor(muzzleHit.collider);
        launchTargetDebug +=
            $", muzzleRayHit={FormatHit(muzzleHit)}, validAnchor={isValidAnchor}, invalidReason={GetInvalidAnchorReason(muzzleHit.collider)}";
    }

    /// <summary>
    /// 카메라 조준점은 유효하되 손/총구에서 그 지점까지 다른 벽이 끼어 있는지만 확인합니다.
    /// Muzzle Ray가 같은 표면을 정확히 다시 맞추지 못해도, 중간 차단물이 없으면 그래플을 허용합니다.
    /// </summary>
    private bool IsMuzzlePathClearToCenterHit(Vector3 origin, RaycastHit centerHit, out RaycastHit blockingHit)
    {
        Vector3 toAnchor = centerHit.point - origin;
        float distance = toAnchor.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            blockingHit = default;
            return true;
        }

        if (!TryGetNearestNonPlayerHit(origin, toAnchor / distance, distance, out RaycastHit muzzleHit))
        {
            blockingHit = default;
            return true;
        }

        if (muzzleHit.collider == centerHit.collider
            || Mathf.Abs(muzzleHit.distance - distance) <= MuzzlePathEndpointTolerance)
        {
            blockingHit = default;
            return true;
        }

        blockingHit = muzzleHit;
        return false;
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

    private string GetInvalidAnchorReason(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return "No collider";
        }

        if (hitCollider.attachedRigidbody != null)
        {
            return $"Collider has attached Rigidbody '{hitCollider.attachedRigidbody.name}'";
        }

        int layerBit = 1 << hitCollider.gameObject.layer;
        if ((grappleSurfaceMask.value & layerBit) == 0)
        {
            return $"Layer '{LayerMask.LayerToName(hitCollider.gameObject.layer)}'({hitCollider.gameObject.layer}) is not in grappleSurfaceMask({grappleSurfaceMask.value})";
        }

        return "None";
    }

    private string FormatHit(RaycastHit hit)
    {
        return $"{FormatCollider(hit.collider)}, point={hit.point}, normal={hit.normal}, distance={hit.distance:0.###}";
    }

    private string FormatCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return "None";
        }

        string layerName = LayerMask.LayerToName(hitCollider.gameObject.layer);
        if (string.IsNullOrEmpty(layerName))
        {
            layerName = hitCollider.gameObject.layer.ToString();
        }

        string rigidbodyName = hitCollider.attachedRigidbody != null
            ? hitCollider.attachedRigidbody.name
            : "None";

        return $"'{hitCollider.name}' path='{GetTransformPath(hitCollider.transform)}' layer='{layerName}'({hitCollider.gameObject.layer}) attachedRb='{rigidbodyName}'";
    }

    private static string GetTransformPath(Transform target)
    {
        if (target == null)
        {
            return "None";
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
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
        float endedPullElapsed = pullElapsed;
        playerController?.CancelGrapplePull();
        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        grappleState = GrappleState.Idle;
        pullElapsed = 0f;
        lastEndReason = reason;

        lastGrappleDebug =
            $"reason={reason}, validAnchor={lastLaunchHadValidAnchor}, target={FormatCollider(targetCollider)}, " +
            $"anchor={anchorPoint}, normal={surfaceNormal}, launchDistance={launchDistance:0.###}, " +
            $"launchProgress={launchProgress:0.###}, pullElapsed={endedPullElapsed:0.###}, detail={launchTargetDebug}";

        if (logGrappleFailures && reason != GrappleEndReason.Arrived && reason != GrappleEndReason.Disabled)
        {
            Debug.LogWarning($"[GrapplingHook] Ended: {lastGrappleDebug}", this);
        }
    }
}
