using DG.Tweening;
using DistantLands;
using UnityEngine;

/// <summary>
/// 거미가 죽었을 때 현재 중력 방향으로 바닥을 찾고, NavTarget을 그 표면 위로 내려앉히는 사망 연출입니다.
/// LegController는 공통 Monster가 멈추므로, 여기서는 IK 타겟만 짧게 흔들어 죽을 때 다리가 떨리게 합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpiderDeathFall : MonoBehaviour, IMonsterDeathHandler, IMonsterResettable
{
    private struct TargetPose
    {
        public Transform target;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Header("References")]
    [SerializeField, Tooltip("죽을 때 내려앉힐 기준 Transform입니다. 비워두면 NavTarget/Nav Target을 찾습니다.")]
    private Transform navTarget;
    [SerializeField, Tooltip("현재 중력 방향을 읽습니다. 비워두면 씬의 GravitySystem 또는 GravityState를 찾습니다.")]
    private GravityState gravityState;

    [Header("Ground Probe")]
    [SerializeField, Tooltip("죽을 때 바닥으로 인정할 레이어입니다. 비워두면 WalkableSurface 레이어를 자동 사용합니다.")]
    private LayerMask groundMask;
    [SerializeField, Min(0f), Tooltip("Raycast 시작점을 중력 반대 방향으로 살짝 띄웁니다. 자기 콜라이더를 먼저 맞는 상황을 줄입니다.")]
    private float raycastStartOffset = 0.5f;
    [SerializeField, Min(0.01f), Tooltip("현재 중력 방향으로 바닥을 찾을 최대 거리입니다.")]
    private float raycastDistance = 12f;
    [SerializeField, Min(0f), Tooltip("Raycast에 맞은 표면에서 중력 반대 방향으로 띄울 거리입니다.")]
    private float surfaceOffset = 0.2f;
    [SerializeField, Min(0f), Tooltip("바닥을 못 찾았을 때 중력 방향으로 그냥 내려앉을 거리입니다.")]
    private float fallbackFallDistance = 1.2f;

    [Header("Fall Shape")]
    [SerializeField, Min(0.01f), Tooltip("풀썩 내려앉는 시간입니다.")]
    private float fallDuration = 0.45f;
    [SerializeField, Min(0f), Tooltip("내려앉기 전에 아주 잠깐 위로 튀는 거리입니다.")]
    private float smallHopDistance = 0.12f;
    [SerializeField, Min(0.01f), Tooltip("작은 튐 시간입니다.")]
    private float smallHopDuration = 0.08f;

    [Header("Leg Tremble")]
    [SerializeField, Tooltip("죽는 동안 다리 IK 타겟을 흔들어 바들바들 떨림을 만듭니다.")]
    private bool shakeLegTargets = true;
    [SerializeField, Min(0f), Tooltip("다리 떨림 세기입니다.")]
    private float legShakeStrength = 0.12f;
    [SerializeField, Min(0.01f), Tooltip("다리 떨림 시간입니다.")]
    private float legShakeDuration = 0.65f;
    [SerializeField, Min(1), Tooltip("다리 떨림 진동 횟수입니다.")]
    private int legShakeVibrato = 24;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("마지막 사망 Raycast가 바닥을 찾았는지 표시합니다.")]
    private bool lastGroundHit;
    [SerializeField, Tooltip("마지막 사망 착지 목표 위치입니다.")]
    private Vector3 lastFallTarget;

    private Sequence deathSequence;
    private TargetPose[] initialLegTargetPoses;

    private void Awake()
    {
        ResolveReferences();
        CaptureLegTargetPoses();
    }

    private void Reset()
    {
        ResolveReferences();
        ResolveDefaultLayers();
    }

    public void OnMonsterDied()
    {
        ResolveReferences();
        ResolveDefaultLayers();
        CaptureLegTargetPoses();

        if (navTarget == null)
        {
            return;
        }

        KillDeathSequence(false);

        Vector3 gravityDirection = GetGravityDirection();
        Vector3 surfaceNormal = -gravityDirection;
        Vector3 startPosition = navTarget.position;
        Vector3 fallTarget = GetFallTarget(startPosition, gravityDirection, surfaceNormal);
        Vector3 hopPosition = startPosition + surfaceNormal * smallHopDistance;
        lastFallTarget = fallTarget;

        deathSequence = DOTween.Sequence()
            .SetTarget(this)
            .Append(navTarget.DOMove(hopPosition, smallHopDuration).SetEase(Ease.OutQuad))
            .Append(navTarget.DOMove(fallTarget, fallDuration).SetEase(Ease.InQuad));

        if (shakeLegTargets)
        {
            AddLegShakeTweens(deathSequence);
        }
    }

    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        KillDeathSequence(false);
        RestoreLegTargetPoses();
        lastGroundHit = false;
        lastFallTarget = Vector3.zero;
    }

    private void ResolveReferences()
    {
        navTarget ??= transform.Find("NavTarget");
        navTarget ??= transform.Find("Nav Target");

        if (gravityState == null)
        {
            GameObject gravitySystem = GameObject.Find("GravitySystem");
            if (gravitySystem != null)
            {
                gravitySystem.TryGetComponent(out gravityState);
            }
        }

        gravityState ??= FindFirstObjectByType<GravityState>();
        ResolveDefaultLayers();
    }

    private void ResolveDefaultLayers()
    {
        if (groundMask.value == 0)
        {
            groundMask = LayerMask.GetMask("WalkableSurface");
        }
    }

    private Vector3 GetGravityDirection()
    {
        Vector3 direction = gravityState != null ? gravityState.Direction : Vector3.down;
        return direction.sqrMagnitude < Mathf.Epsilon ? Vector3.down : direction.normalized;
    }

    private Vector3 GetFallTarget(Vector3 startPosition, Vector3 gravityDirection, Vector3 surfaceNormal)
    {
        Vector3 origin = startPosition + surfaceNormal * raycastStartOffset;
        int mask = groundMask.value != 0 ? groundMask.value : ~0;
        lastGroundHit = Physics.Raycast(
            origin,
            gravityDirection,
            out RaycastHit hit,
            raycastDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        if (lastGroundHit)
        {
            return hit.point + surfaceNormal * surfaceOffset;
        }

        return startPosition + gravityDirection * fallbackFallDistance;
    }

    private void CaptureLegTargetPoses()
    {
        Leg[] legs = GetComponentsInChildren<Leg>(true);
        initialLegTargetPoses = new TargetPose[legs.Length];
        for (int i = 0; i < legs.Length; i++)
        {
            Transform target = legs[i] != null && legs[i].IKSolver != null
                ? legs[i].IKSolver.target
                : null;

            if (target == null)
            {
                continue;
            }

            initialLegTargetPoses[i] = new TargetPose
            {
                target = target,
                position = target.position,
                rotation = target.rotation
            };
        }
    }

    private void AddLegShakeTweens(Sequence sequence)
    {
        if (sequence == null || initialLegTargetPoses == null)
        {
            return;
        }

        for (int i = 0; i < initialLegTargetPoses.Length; i++)
        {
            Transform target = initialLegTargetPoses[i].target;
            if (target == null)
            {
                continue;
            }

            sequence.Insert(
                0f,
                target.DOShakePosition(legShakeDuration, legShakeStrength, legShakeVibrato, 90f, false, true)
                    .SetEase(Ease.OutQuad));
        }
    }

    private void RestoreLegTargetPoses()
    {
        if (initialLegTargetPoses == null)
        {
            return;
        }

        for (int i = 0; i < initialLegTargetPoses.Length; i++)
        {
            TargetPose pose = initialLegTargetPoses[i];
            if (pose.target == null)
            {
                continue;
            }

            pose.target.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }

    private void KillDeathSequence(bool complete)
    {
        if (deathSequence == null)
        {
            return;
        }

        deathSequence.Kill(complete);
        deathSequence = null;
    }

    private void OnValidate()
    {
        raycastStartOffset = Mathf.Max(0f, raycastStartOffset);
        raycastDistance = Mathf.Max(0.01f, raycastDistance);
        surfaceOffset = Mathf.Max(0f, surfaceOffset);
        fallbackFallDistance = Mathf.Max(0f, fallbackFallDistance);
        fallDuration = Mathf.Max(0.01f, fallDuration);
        smallHopDistance = Mathf.Max(0f, smallHopDistance);
        smallHopDuration = Mathf.Max(0.01f, smallHopDuration);
        legShakeStrength = Mathf.Max(0f, legShakeStrength);
        legShakeDuration = Mathf.Max(0.01f, legShakeDuration);
        legShakeVibrato = Mathf.Max(1, legShakeVibrato);
    }
}
