using System;
using UnityEngine;

[DefaultExecutionOrder(105)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput), typeof(PlayerController))]
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
    [SerializeField, Min(0)] private int shotDamage = 1;

    [Header("Magazine")]
    [SerializeField, Min(1)] private int magazineCapacity = 30;
    [Tooltip("Play Mode에서 확인하는 현재 장탄 수입니다.")]
    [SerializeField, Min(0)] private int currentRounds;
    [SerializeField, Min(0.01f)] private float reloadDuration = 3.67f;

    [Header("Shot Audio")]
    [SerializeField] private AudioSource shotAudioSource;

    [Header("Reload Audio")]
    [SerializeField] private AudioSource reloadAudioSource;
    [SerializeField, Min(0f)] private float reloadAudioDelay = 0.3f;

    [Header("Physics Hit")]
    [SerializeField, Min(0f)] private float shotPushVelocityChange = 1.5f;

    [Header("Runtime Shot Debug")]
    [Tooltip("마지막 최종 사격 Ray가 맞힌 Collider입니다.")]
    [SerializeField] private Collider lastShotCollider;
    [Tooltip("마지막 사격이 확인한 비키네마틱 GravityBody Rigidbody입니다.")]
    [SerializeField] private Rigidbody lastShotRigidbody;
    [Tooltip("마지막 사격에서 GravityBody에 물리 밀기 힘을 적용했는지 표시합니다.")]
    [SerializeField] private bool lastShotAppliedPhysicsPush;

    [Header("Shot Tracer")]
    [SerializeField] private bool showShotTracer = true;
    [SerializeField] private LineRenderer shotTracer;
    [SerializeField, Min(0f)] private float shotTracerDuration = 0.05f;
    [SerializeField] private Color hitTracerColor = Color.red;
    [SerializeField] private Color missTracerColor = Color.cyan;

    private readonly RaycastHit[] cameraHits = new RaycastHit[16];
    private readonly RaycastHit[] muzzleHits = new RaycastHit[16];

    private PlayerController playerController;
    private bool fireSequenceActive;
    private bool didWarnAboutCameraBuffer;
    private bool didWarnAboutMuzzleBuffer;
    private double nextShotTime;
    private double reloadEndsAt;
    private double tracerVisibleUntil;
    private int shotCount;
    private Vector3 lastShotEnd;

    internal bool IsFiring { get; private set; }
    internal bool IsReloading { get; private set; }
    internal bool ReloadStartedThisFrame { get; private set; }
    internal int CurrentRounds => currentRounds;
    internal int MagazineCapacity => magazineCapacity;
    internal float AimPitchDegrees => aimCamera != null ? aimCamera.PitchDegrees : 0f;
    internal bool LastShotAppliedPhysicsPush => lastShotAppliedPhysicsPush;

    public event Action<int, int> MagazineChanged;

    private void OnValidate()
    {
        magazineCapacity = Mathf.Max(1, magazineCapacity);
        currentRounds = Mathf.Clamp(currentRounds, 0, magazineCapacity);
        reloadDuration = Mathf.Max(0.01f, reloadDuration);
    }

    private void Awake()
    {
        input ??= GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
        magazineCapacity = Mathf.Max(1, magazineCapacity);
        reloadDuration = Mathf.Max(0.01f, reloadDuration);
        currentRounds = magazineCapacity;
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

        if (shotAudioSource == null || shotAudioSource.clip == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCombatController)} on '{name}' has no shot AudioSource or AudioClip. Shots will remain silent.",
                this);
        }

        if (reloadAudioSource == null || reloadAudioSource.clip == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCombatController)} on '{name}' has no reload AudioSource or AudioClip. Reloads will remain silent.",
                this);
        }

        HideShotTracer();
    }

    private void OnDisable()
    {
        StopFiring();
        CancelReload();
        HideShotTracer();
    }

    private void LateUpdate()
    {
        double now = Time.timeAsDouble;
        ReloadStartedThisFrame = false;
        UpdateShotTracer(now);

        if (IsReloading)
        {
            if (now < reloadEndsAt)
            {
                return;
            }

            CompleteReload();
        }

        if (!input.AllowCombat)
        {
            StopFiring();
            return;
        }

        if (input.ReloadPressed && currentRounds < magazineCapacity)
        {
            StartReload(now);
            return;
        }

        if (currentRounds <= 0)
        {
            StartReload(now);
            return;
        }

        if (!input.FireHeld)
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
        if (fireSequenceActive)
        {
            nextShotTime = now + fireInterval;
        }
    }

    private void StopFiring()
    {
        fireSequenceActive = false;
        IsFiring = false;
        nextShotTime = 0d;
    }

    private void StartReload(double now)
    {
        if (IsReloading || currentRounds >= magazineCapacity)
        {
            return;
        }

        StopFiring();
        IsReloading = true;
        ReloadStartedThisFrame = true;
        reloadEndsAt = now + reloadDuration;
        PlayReloadAudio();
    }

    private void CompleteReload()
    {
        SetCurrentRounds(magazineCapacity);
        IsReloading = false;
        reloadEndsAt = 0d;
    }

    private void CancelReload()
    {
        IsReloading = false;
        ReloadStartedThisFrame = false;
        reloadEndsAt = 0d;
        reloadAudioSource?.Stop();
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
        PlayShotAudio();
        playerController.TryApplyZeroGravityRecoil(-shotDirection);
        lastShotCollider = hasShotHit ? shotHit.collider : null;
        lastShotRigidbody = null;
        lastShotAppliedPhysicsPush = false;
        lastShotEnd = shotEnd;

        if (hasShotHit && TryGetLivingMonster(shotHit.collider, out MonsterHealth monsterHealth))
        {
            monsterHealth.ApplyDamage(shotDamage);
        }

        if (hasShotHit)
        {
            ApplyShotPush(shotHit, shotDirection);
        }

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

        SetCurrentRounds(currentRounds - 1);
        if (currentRounds == 0)
        {
            StartReload(now);
        }
    }

    private void SetCurrentRounds(int rounds)
    {
        int nextRounds = Mathf.Clamp(rounds, 0, magazineCapacity);
        if (currentRounds == nextRounds)
        {
            return;
        }

        currentRounds = nextRounds;
        MagazineChanged?.Invoke(currentRounds, magazineCapacity);
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

    private void PlayShotAudio()
    {
        if (shotAudioSource == null || shotAudioSource.clip == null)
        {
            return;
        }

        shotAudioSource.Stop();
        shotAudioSource.Play();
    }

    private void PlayReloadAudio()
    {
        if (reloadAudioSource == null || reloadAudioSource.clip == null)
        {
            return;
        }

        reloadAudioSource.Stop();
        reloadAudioSource.PlayDelayed(reloadAudioDelay);
    }

    private void ApplyShotPush(RaycastHit shotHit, Vector3 shotDirection)
    {
        Rigidbody hitBody = shotHit.rigidbody;
        if (shotPushVelocityChange <= 0f
            || hitBody == null
            || hitBody.isKinematic
            || !hitBody.TryGetComponent(out GravityBody _))
        {
            return;
        }

        lastShotRigidbody = hitBody;
        hitBody.WakeUp();
        hitBody.AddForce(shotDirection * shotPushVelocityChange, ForceMode.VelocityChange);
        lastShotAppliedPhysicsPush = true;
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
            QueryTriggerInteraction.Collide);

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

            if (hit.collider.isTrigger && !TryGetLivingMonster(hit.collider, out _))
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

    private static bool TryGetLivingMonster(Collider hitCollider, out MonsterHealth monsterHealth)
    {
        monsterHealth = hitCollider != null
            ? hitCollider.GetComponentInParent<MonsterHealth>()
            : null;

        return monsterHealth != null && !monsterHealth.IsDead;
    }
}
