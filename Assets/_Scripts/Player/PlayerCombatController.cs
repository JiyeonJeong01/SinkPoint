using UnityEngine;

[DefaultExecutionOrder(105)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private ThirdPersonCameraController aimCamera;
    [SerializeField] private Transform muzzle;

    [Header("Firing")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.1f;
    [SerializeField, Min(0f)] private float maxRange = 100f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Shot Tracer")]
    [SerializeField] private bool showShotTracer = true;
    [SerializeField] private LineRenderer shotTracer;
    [SerializeField, Min(0f)] private float shotTracerDuration = 0.05f;
    [SerializeField] private Color hitTracerColor = Color.red;
    [SerializeField] private Color missTracerColor = Color.cyan;

    private readonly RaycastHit[] cameraHits = new RaycastHit[16];
    private readonly RaycastHit[] muzzleHits = new RaycastHit[16];

    private bool fireSequenceActive;
    private bool didWarnAboutCameraBuffer;
    private bool didWarnAboutMuzzleBuffer;
    private double nextShotTime;
    private double tracerVisibleUntil;
    private int shotCount;
    private Collider lastShotCollider;
    private Vector3 lastShotEnd;

    internal bool IsFiring { get; private set; }
    internal float AimPitchDegrees => aimCamera != null ? aimCamera.PitchDegrees : 0f;

    private void Awake()
    {
        input ??= GetComponent<PlayerInput>();
    }

    private void Start()
    {
        if (input == null || aimCamera == null || muzzle == null)
        {
            Debug.LogError(
                $"{nameof(PlayerCombatController)} on '{name}' requires Input, Aim Camera, and Muzzle references.",
                this);
            enabled = false;
            return;
        }

        if (showShotTracer && shotTracer == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCombatController)} on '{name}' has Shot Tracer enabled without a LineRenderer reference.",
                this);
        }

        HideShotTracer();
    }

    private void OnDisable()
    {
        StopFiring();
        HideShotTracer();
    }

    private void LateUpdate()
    {
        double now = Time.timeAsDouble;
        UpdateShotTracer(now);

        if (!input.AllowCombat || !input.FireHeld)
        {
            StopFiring();
            return;
        }

        if (input.FirePressed && !fireSequenceActive)
        {
            fireSequenceActive = true;
            nextShotTime = Time.timeAsDouble;
        }

        if (!fireSequenceActive)
        {
            IsFiring = false;
            return;
        }

        IsFiring = true;
        if (now < nextShotTime)
        {
            return;
        }

        FireShot(now);
        nextShotTime = now + fireInterval;
    }

    private void StopFiring()
    {
        fireSequenceActive = false;
        IsFiring = false;
        nextShotTime = 0d;
    }

    private void FireShot(double now)
    {
        Ray aimRay = aimCamera.CreateCenterRay();
        bool hasAimHit = TryGetNearestValidHit(
            aimRay.origin,
            aimRay.direction,
            maxRange,
            cameraHits,
            ref didWarnAboutCameraBuffer,
            out RaycastHit aimHit);

        Vector3 aimPoint = hasAimHit
            ? aimHit.point
            : aimRay.origin + aimRay.direction * maxRange;

        Vector3 muzzleToAim = aimPoint - muzzle.position;
        float aimDistance = muzzleToAim.magnitude;
        float shotDistance = Mathf.Min(maxRange, aimDistance + 0.05f);
        Vector3 shotDirection = aimDistance > Mathf.Epsilon
            ? muzzleToAim / muzzleToAim.magnitude
            : aimRay.direction;

        bool hasShotHit = TryGetNearestValidHit(
            muzzle.position,
            shotDirection,
            shotDistance,
            muzzleHits,
            ref didWarnAboutMuzzleBuffer,
            out RaycastHit shotHit);

        Vector3 shotEnd = hasShotHit
            ? shotHit.point
            : muzzle.position + shotDirection * shotDistance;
        shotCount++;
        lastShotCollider = hasShotHit ? shotHit.collider : null;
        lastShotEnd = shotEnd;

        if (showShotTracer && shotTracer != null)
        {
            Color tracerColor = hasShotHit ? hitTracerColor : missTracerColor;
            shotTracer.positionCount = 2;
            shotTracer.SetPosition(0, muzzle.position);
            shotTracer.SetPosition(1, shotEnd);
            shotTracer.startColor = tracerColor;
            shotTracer.endColor = tracerColor;
            shotTracer.enabled = true;
            tracerVisibleUntil = now + shotTracerDuration;
        }
    }

    private void UpdateShotTracer(double now)
    {
        if (shotTracer == null)
        {
            return;
        }

        if (!showShotTracer || now >= tracerVisibleUntil)
        {
            shotTracer.enabled = false;
        }
    }

    private void HideShotTracer()
    {
        tracerVisibleUntil = 0d;
        if (shotTracer != null)
        {
            shotTracer.enabled = false;
        }
    }

    private bool TryGetNearestValidHit(
        Vector3 origin,
        Vector3 direction,
        float distance,
        RaycastHit[] hits,
        ref bool didWarnAboutBuffer,
        out RaycastHit nearestHit)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            hits,
            distance,
            hitMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == hits.Length && !didWarnAboutBuffer)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCombatController)} on '{name}' filled a shot hit buffer.",
                this);
            didWarnAboutBuffer = true;
        }

        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            nearestHit = hit;
        }

        return nearestDistance < float.PositiveInfinity;
    }
}
