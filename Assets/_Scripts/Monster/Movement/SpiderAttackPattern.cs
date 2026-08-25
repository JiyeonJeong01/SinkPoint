using DG.Tweening;
using UnityEngine;

/// <summary>
/// 거미의 근접 몸빵과 짧은 독 분사 VFX 공격을 담당합니다.
/// 이동은 NavTarget만 조작하고, 공격 중에는 route mover를 잠깐 멈춰 기존 순찰 index를 보존합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpiderAttackPattern : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum AttackDebugStatus
    {
        Ready,
        NoNavTarget,
        NoStateMachine,
        NoTarget,
        Cooldown,
        WaitingForRoutePause,
        WaitingForRoutePauseEnd,
        AlreadySprayedThisPause,
        OutOfSprayRange,
        NoSprayVfx,
        SprayBlockedByObstacle,
        MeleeWindup,
        MeleeLunge,
        MeleeRecover,
        Spray
    }

    private enum SprayAimAxis
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    [Header("References")]
    [SerializeField] private Transform navTarget;
    [SerializeField] private MonsterStateMachine stateMachine;
    [SerializeField] private SpiderSurfaceRouteMover routeMover;
    [SerializeField, Tooltip("공격/이동/사망 사운드를 재생합니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterAudioFeedback audioFeedback;
    [SerializeField, Tooltip("배회 중 독 분사는 감지 상태와 무관하게 이 태그의 플레이어를 향해 발사합니다.")]
    private string playerTag = "Player";
    [SerializeField, Tooltip("거미 입에 배치해 둔 독 분사 VFX 오브젝트입니다. 비어 있으면 자식에서 VFX_Pressure_Pee_Attack 이름을 찾습니다.")]
    private GameObject sprayVfxObject;
    [SerializeField, Tooltip("길이를 늘릴 Stream 자식입니다. 비어 있으면 이름에 Stream이 들어간 자식을 찾습니다.")]
    private Transform streamTransform;

    [Header("Melee")]
    [SerializeField, Min(0f), Tooltip("근접 몸빵 후 다음 근접 공격까지 기다리는 시간입니다. 무한 몸빵 루프를 막습니다.")]
    private float meleeCooldown = 2.5f;
    [SerializeField, Min(0.01f), Tooltip("웅크렸다 올라오는 한 번의 시간입니다. 이 동작을 3번 반복합니다.")]
    private float crouchHalfDuration = 0.08f;
    [SerializeField, Min(0f), Tooltip("웅크릴 때 NavTarget을 현재 전투 바닥 쪽으로 내리는 거리입니다.")]
    private float crouchDistance = 0.35f;
    [SerializeField, Min(0.01f), Tooltip("플레이어 쪽으로 빠르게 몸빵하는 시간입니다.")]
    private float lungeDuration = 0.12f;
    [SerializeField, Min(0.01f), Tooltip("몸빵 후 원래 위치 근처로 빠지는 시간입니다.")]
    private float recoverDuration = 0.16f;
    [SerializeField, Min(0f), Tooltip("돌진 도착점이 플레이어 발 위치에서 얼마나 떨어질지 정합니다.")]
    private float meleeStopDistance = 0.35f;
    [SerializeField, Min(0f), Tooltip("플레이어를 통과해 뒤쪽으로 얼마나 더 지나갈지 정합니다.")]
    private float meleePassThroughDistance = 1.6f;
    [SerializeField, Min(0f), Tooltip("복귀 위치를 시작점보다 살짝 뒤로 뺄 거리입니다.")]
    private float recoverBackDistance = 0.25f;
    [SerializeField, Min(0f), Tooltip("근접 돌진 목표를 플레이어 발 위치에서 현재 표면 normal 방향으로 살짝 띄울 거리입니다. 낮을수록 몸으로 들이받는 느낌이 강합니다.")]
    private float meleeTargetSurfaceOffset = 0.05f;
    [SerializeField, Range(0f, 1f), Tooltip("첫 근접 돌진 후 한 번 더 되돌아 돌진할 확률입니다.")]
    private float secondLungeChance = 0.35f;
    [SerializeField, Min(0f), Tooltip("두 번째 돌진을 하기 전 짧게 기다리는 시간입니다.")]
    private float secondLungeDelay = 0.12f;
    [SerializeField, Tooltip("근접 피해를 코드로 직접 줄지 정합니다. 접촉 Trigger가 따로 있으면 꺼두는 편이 안전합니다.")]
    private bool applyDirectMeleeDamage;
    [SerializeField, Min(0)] private int meleeDamage = 1;
    [SerializeField, Min(0f)] private float directMeleeDamageRadius = 1.2f;

    [Header("Spray")]
    [SerializeField, Min(0f), Tooltip("독 분사를 시작할 최소 거리입니다. 이보다 가까우면 근접 공격을 우선합니다.")]
    private float sprayMinDistance = 2f;
    [SerializeField, Min(0f), Tooltip("독 분사를 시도할 최대 거리입니다.")]
    private float sprayMaxDistance = 9f;
    [SerializeField, Tooltip("켜면 독 분사가 Spray Min/Max Distance 안에서만 나갑니다. 꺼두면 route pause마다 거리와 무관하게 발사합니다.")]
    private bool requireSprayRange;
    [SerializeField, Min(0f), Tooltip("독 분사 후 다음 독 분사까지 기다리는 시간입니다.")]
    private float sprayCooldown = 3f;
    [SerializeField, Min(0.01f), Tooltip("VFX를 켜두는 시간입니다.")]
    private float sprayDuration = 0.2f;
    [SerializeField, Tooltip("켜면 RoutePause가 거의 끝나 다시 움직이기 직전에 독 분사를 시작합니다.")]
    private bool sprayNearRoutePauseEnd = true;
    [SerializeField, Min(0f), Tooltip("RoutePause 종료 몇 초 전부터 독 분사를 허용할지 정합니다. 값이 작을수록 더 늦게 쏩니다.")]
    private float sprayBeforeRouteResumeSeconds = 0.3f;
    [SerializeField, Range(0f, 1f), Tooltip("독 분사가 실제 피해를 줄 확률입니다.")]
    private float sprayHitChance = 0.33f;
    [SerializeField, Min(0)] private int sprayDamage = 1;
    [SerializeField, Min(0f), Tooltip("독 분사 시작 전 플레이어를 바라보는 준비 시간입니다.")]
    private float faceTargetDuration = 0.08f;
    [SerializeField, Tooltip("Stream 자식의 Z 스케일을 0에서 원래 길이까지 늘려 분사처럼 보이게 합니다.")]
    private bool scaleStreamZ = true;
    [SerializeField, Tooltip("VFX가 실제로 뻗는 로컬 축입니다. 독침이 빗나가면 Back/Right/Left 등을 바꿔 축을 맞춥니다.")]
    private SprayAimAxis sprayAimAxis = SprayAimAxis.Forward;
    [SerializeField, Tooltip("독침 발사 경로를 막는 장애물 레이어입니다. 비어 있으면 Obstacle 레이어를 자동 사용합니다.")]
    private LayerMask sprayObstacleMask;
    [SerializeField, Min(0.01f), Tooltip("독침 발사 전 Obstacle 확인에 사용할 SphereCast 반지름입니다.")]
    private float sprayObstacleCheckRadius = 0.15f;
    [SerializeField, Min(0f), Tooltip("플레이어 발쪽이 막혔을 때 한 번 더 조준할 높이입니다.")]
    private float sprayRetryTargetLift = 0.8f;

    [Header("Combat Surface")]
    [SerializeField, Tooltip("전투 중 한 방향 중력입니다. Surface normal은 이 값의 반대 방향을 사용합니다.")]
    private Vector3 combatGravityDirection = Vector3.down;
    [SerializeField, Tooltip("NavTarget 회전만으로 부족할 때 몬스터 최상위도 플레이어 방향으로 돌립니다. route 복귀가 흔들리면 끕니다.")]
    private bool rotateMonsterRoot;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 거미 공격 단계입니다. 런타임 확인용입니다.")]
    private AttackDebugStatus attackDebugStatus = AttackDebugStatus.Ready;
    [SerializeField, Tooltip("다음 근접 공격까지 남은 시간입니다.")]
    private float meleeCooldownRemaining;
    [SerializeField, Tooltip("다음 독 분사까지 남은 시간입니다.")]
    private float sprayCooldownRemaining;
    [SerializeField, Tooltip("마지막 독 분사가 실제 피해를 줬는지 표시합니다.")]
    private bool lastSprayHit;
    [SerializeField, Tooltip("현재 독 분사 조건을 검사한 플레이어와의 거리입니다.")]
    private float sprayTargetDistance = -1f;
    [SerializeField, Tooltip("현재 RoutePause 상태인지 표시합니다.")]
    private bool isRoutePauseActive;
    [SerializeField, Tooltip("독 분사 기준으로 본 RoutePause 남은 시간입니다.")]
    private float routePauseRemainingForSpray;
    [SerializeField, Tooltip("현재 pause에서 이미 독 분사를 했는지 표시합니다.")]
    private bool alreadySprayedThisPause;
    [SerializeField, Tooltip("독 분사 VFX 축과 플레이어 방향이 얼마나 일치하는지 표시합니다. 1에 가까울수록 정확합니다.")]
    private float sprayAimDot;
    [SerializeField, Tooltip("마지막 독침 발사 시도가 Obstacle에 막혔는지 표시합니다.")]
    private bool sprayBlockedByObstacle;

    private Sequence attackSequence;
    private Vector3 streamOriginalScale = Vector3.one;
    private float nextMeleeTime;
    private float nextSprayTime;
    private bool sprayedDuringCurrentRoutePause;
    private bool sprayBlockedDuringCurrentRoutePause;

    private void Awake()
    {
        ResolveSceneReferences();
        ResolveDefaultLayers();
        PrepareSprayVfx();
    }

    private void Reset()
    {
        ResolveSceneReferences();
        ResolveDefaultLayers();
    }

    private void Update()
    {
        if (attackSequence != null && attackSequence.IsActive())
        {
            return;
        }

        if (!CanRunAttackLogic())
        {
            return;
        }

        meleeCooldownRemaining = Mathf.Max(0f, nextMeleeTime - Time.time);
        sprayCooldownRemaining = Mathf.Max(0f, nextSprayTime - Time.time);

        Transform combatTarget = stateMachine.Target;
        if (stateMachine.State == MonsterState.Attack && combatTarget != null && meleeCooldownRemaining <= 0f)
        {
            StartMeleeAttack(combatTarget);
            return;
        }

        Transform sprayTarget = GetSprayTarget();
        if (sprayTarget == null)
        {
            attackDebugStatus = AttackDebugStatus.NoTarget;
            return;
        }

        sprayTargetDistance = Vector3.Distance(GetOriginPosition(), sprayTarget.position);
        if (CanSpray(sprayTargetDistance))
        {
            if (!TryGetClearSprayAimPosition(sprayTarget, out Vector3 sprayAimPosition))
            {
                sprayBlockedByObstacle = true;
                sprayBlockedDuringCurrentRoutePause = true;
                attackDebugStatus = AttackDebugStatus.SprayBlockedByObstacle;
                return;
            }

            sprayBlockedByObstacle = false;
            StartSprayAttack(sprayTarget, sprayAimPosition);
            return;
        }

        if (routeMover == null || !routeMover.IsRoutePauseActive)
        {
            sprayedDuringCurrentRoutePause = false;
            sprayBlockedDuringCurrentRoutePause = false;
            routePauseRemainingForSpray = 0f;
        }

        attackDebugStatus = meleeCooldownRemaining > 0f || sprayCooldownRemaining > 0f
            ? AttackDebugStatus.Cooldown
            : AttackDebugStatus.Ready;
    }

    private void OnDisable()
    {
        KillAttackSequence(false);
        SetRouteMoverPaused(false);
        SetSprayVfxActive(false);
    }

    /// <summary>
    /// 리스폰 때 진행 중인 DOTween 공격, 독 VFX, 쿨다운, route pause 잠금을 모두 정리합니다.
    /// </summary>
    public void ResetMonsterRuntime()
    {
        ResolveSceneReferences();
        ResolveDefaultLayers();
        KillAttackSequence(false);
        SetRouteMoverPaused(false);
        SetSprayVfxActive(false);

        nextMeleeTime = 0f;
        nextSprayTime = 0f;
        meleeCooldownRemaining = 0f;
        sprayCooldownRemaining = 0f;
        sprayedDuringCurrentRoutePause = false;
        sprayBlockedDuringCurrentRoutePause = false;
        lastSprayHit = false;
        sprayBlockedByObstacle = false;
        attackDebugStatus = AttackDebugStatus.Ready;
    }

    public void OnMonsterDied()
    {
        KillAttackSequence(false);
        SetRouteMoverPaused(false);
        SetSprayVfxActive(false);
        attackDebugStatus = AttackDebugStatus.Cooldown;
    }

    /// <summary>
    /// Inspector 참조가 비어 있으면 거미 계층에서 NavTarget, StateMachine, RouteMover, VFX를 찾습니다.
    /// </summary>
    private void ResolveSceneReferences()
    {
        navTarget ??= transform.Find("NavTarget");
        navTarget ??= transform.Find("Nav Target");

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        routeMover ??= GetComponent<SpiderSurfaceRouteMover>();
        routeMover ??= GetComponentInParent<SpiderSurfaceRouteMover>();
        routeMover ??= GetComponentInChildren<SpiderSurfaceRouteMover>();

        audioFeedback ??= GetComponent<MonsterAudioFeedback>();
        audioFeedback ??= GetComponentInParent<MonsterAudioFeedback>();
        audioFeedback ??= GetComponentInChildren<MonsterAudioFeedback>();

        if (sprayVfxObject == null)
        {
            Transform vfx = FindChildRecursive(transform, "VFX_Pressure_Pee_Attack");
            sprayVfxObject = vfx != null ? vfx.gameObject : null;
        }

        if (streamTransform == null && sprayVfxObject != null)
        {
            streamTransform = FindChildContainsRecursive(sprayVfxObject.transform, "Stream");
        }
    }

    private void PrepareSprayVfx()
    {
        if (sprayVfxObject == null)
        {
            return;
        }

        DisableCylinderRenderers(sprayVfxObject.transform);

        if (streamTransform != null)
        {
            streamOriginalScale = streamTransform.localScale;
        }

        SetSprayVfxActive(false);
    }

    private bool CanRunAttackLogic()
    {
        if (navTarget == null)
        {
            attackDebugStatus = AttackDebugStatus.NoNavTarget;
            return false;
        }

        if (stateMachine == null)
        {
            attackDebugStatus = AttackDebugStatus.NoStateMachine;
            return false;
        }

        return stateMachine.State != MonsterState.Dead;
    }

    private bool CanSpray(float distance)
    {
        isRoutePauseActive = routeMover != null && routeMover.IsRoutePauseActive;
        routePauseRemainingForSpray = routeMover != null ? routeMover.RoutePauseRemaining : 0f;
        alreadySprayedThisPause = sprayedDuringCurrentRoutePause;

        if (sprayVfxObject == null)
        {
            attackDebugStatus = AttackDebugStatus.NoSprayVfx;
            return false;
        }

        if (!isRoutePauseActive)
        {
            attackDebugStatus = AttackDebugStatus.WaitingForRoutePause;
            return false;
        }

        // RoutePause 초반에는 다리와 VFX 축이 아직 정리되는 중이라, 거의 끝날 때까지 기다립니다.
        if (sprayNearRoutePauseEnd && routePauseRemainingForSpray > sprayBeforeRouteResumeSeconds)
        {
            attackDebugStatus = AttackDebugStatus.WaitingForRoutePauseEnd;
            return false;
        }

        if (sprayedDuringCurrentRoutePause)
        {
            attackDebugStatus = AttackDebugStatus.AlreadySprayedThisPause;
            return false;
        }

        if (sprayBlockedDuringCurrentRoutePause)
        {
            attackDebugStatus = AttackDebugStatus.SprayBlockedByObstacle;
            return false;
        }

        if (sprayCooldownRemaining > 0f)
        {
            attackDebugStatus = AttackDebugStatus.Cooldown;
            return false;
        }

        bool inSprayRange = distance >= sprayMinDistance && distance <= sprayMaxDistance;
        if (requireSprayRange && !inSprayRange)
        {
            attackDebugStatus = AttackDebugStatus.OutOfSprayRange;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 웅크림 3회 후 플레이어 발 방향으로 돌진하고, 바로 뒤로 빠져 다음 상태 복귀가 튀지 않게 합니다.
    /// </summary>
    private void StartMeleeAttack(Transform target)
    {
        Vector3 startPosition = navTarget.position;
        Vector3 surfaceNormal = GetCombatSurfaceNormal();
        Vector3 targetFeetPosition = GetTargetFeetPosition(target);
        Vector3 loweredTargetPosition = targetFeetPosition + surfaceNormal * meleeTargetSurfaceOffset;
        Vector3 attackDirection = GetPlanarDirectionToTarget(loweredTargetPosition, startPosition, surfaceNormal);
        Vector3 crouchPosition = startPosition - surfaceNormal * crouchDistance;
        Vector3 lungePosition = loweredTargetPosition + attackDirection * meleePassThroughDistance;
        Vector3 recoverPosition = startPosition - attackDirection * recoverBackDistance;
        bool doSecondLunge = Random.value <= secondLungeChance;

        KillAttackSequence(false);
        SetRouteMoverPaused(true);
        attackSequence = DOTween.Sequence()
            .SetTarget(this)
            .AppendCallback(() =>
            {
                attackDebugStatus = AttackDebugStatus.MeleeWindup;
                FaceTarget(target);
            });

        for (int i = 0; i < 3; i++)
        {
            attackSequence
                .Append(navTarget.DOMove(crouchPosition, crouchHalfDuration).SetEase(Ease.InOutSine))
                .Append(navTarget.DOMove(startPosition, crouchHalfDuration).SetEase(Ease.InOutSine));
        }

        attackSequence
            .AppendCallback(() =>
            {
                attackDebugStatus = AttackDebugStatus.MeleeLunge;
                audioFeedback?.PlayBodySlam();
            })
            .Append(navTarget.DOMove(lungePosition, lungeDuration).SetEase(Ease.InQuad))
            .AppendCallback(() => TryApplyDirectMeleeDamage(target))
            .AppendCallback(() => attackDebugStatus = AttackDebugStatus.MeleeRecover);

        if (doSecondLunge)
        {
            attackSequence
                .AppendInterval(secondLungeDelay)
                .AppendCallback(() => FaceTarget(target))
                .AppendCallback(() => audioFeedback?.PlayBodySlam())
                .Append(navTarget.DOMove(recoverPosition, lungeDuration).SetEase(Ease.InQuad))
                .AppendCallback(() => TryApplyDirectMeleeDamage(target));
        }
        else
        {
            attackSequence
                .Append(navTarget.DOMove(recoverPosition, recoverDuration).SetEase(Ease.OutQuad))
                .AppendCallback(() => TryApplyDirectMeleeDamage(target));
        }

        attackSequence.OnComplete(() =>
        {
            nextMeleeTime = Time.time + meleeCooldown;
            meleeCooldownRemaining = meleeCooldown;
            SetRouteMoverPaused(false);
            attackDebugStatus = AttackDebugStatus.Cooldown;
        });
    }

    /// <summary>
    /// 거미가 먼저 플레이어 발쪽을 바라본 뒤, 0.2초 동안 독 분사 VFX를 켜고 확률 피해를 적용합니다.
    /// </summary>
    private void StartSprayAttack(Transform target, Vector3 aimPosition)
    {
        KillAttackSequence(false);
        SetRouteMoverPaused(true);
        lastSprayHit = false;

        attackSequence = DOTween.Sequence()
            .SetTarget(this)
            .AppendCallback(() =>
            {
                attackDebugStatus = AttackDebugStatus.Spray;
                FaceTarget(target);
                AimSprayVfx(aimPosition);
            })
            .Append(DOVirtual.Float(0f, 1f, faceTargetDuration, _ =>
            {
                FaceTarget(target);
                AimSprayVfx(aimPosition);
            }))
            .AppendCallback(() =>
            {
                FaceTarget(target);
                AimSprayVfx(aimPosition);
                SetSprayVfxActive(true);
                PlaySprayParticles();
                audioFeedback?.PlayRangedAttack();
                sprayedDuringCurrentRoutePause = true;
                TryApplySprayDamage(target);
            });

        if (scaleStreamZ && streamTransform != null)
        {
            Vector3 zeroLengthScale = streamOriginalScale;
            zeroLengthScale.z = 0f;
            streamTransform.localScale = zeroLengthScale;
            attackSequence.Append(streamTransform.DOScale(streamOriginalScale, sprayDuration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => AimSprayVfx(aimPosition)));
        }
        else
        {
            attackSequence.Append(DOVirtual.Float(0f, 1f, sprayDuration, _ => AimSprayVfx(aimPosition)));
        }

        attackSequence.OnComplete(() =>
        {
            SetSprayVfxActive(false);
            nextSprayTime = Time.time + sprayCooldown;
            sprayCooldownRemaining = sprayCooldown;
            SetRouteMoverPaused(false);
            attackDebugStatus = AttackDebugStatus.Cooldown;
        });
    }

    private void FaceTarget(Transform target)
    {
        Vector3 surfaceNormal = GetCombatSurfaceNormal();
        Vector3 direction = GetPlanarDirectionToTarget(GetTargetFeetPosition(target), GetOriginPosition(), surfaceNormal);
        Quaternion targetRotation = Quaternion.LookRotation(direction, surfaceNormal);

        navTarget.rotation = targetRotation;

        if (rotateMonsterRoot)
        {
            transform.rotation = targetRotation;
        }
    }

    private void AimSprayVfx(Vector3 aimPosition)
    {
        if (sprayVfxObject == null)
        {
            return;
        }

        Vector3 direction = aimPosition - sprayVfxObject.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = navTarget.forward;
        }

        Vector3 targetDirection = direction.normalized;
        Quaternion forwardRotation = Quaternion.LookRotation(targetDirection, GetCombatSurfaceNormal());
        Quaternion axisCorrection = Quaternion.FromToRotation(GetLocalAimAxis(sprayAimAxis), Vector3.forward);
        sprayVfxObject.transform.rotation = forwardRotation * axisCorrection;
        sprayAimDot = Vector3.Dot(GetWorldAimAxis(sprayVfxObject.transform, sprayAimAxis), targetDirection);
    }

    private bool TryGetClearSprayAimPosition(Transform target, out Vector3 aimPosition)
    {
        aimPosition = GetTargetFeetPosition(target);
        if (sprayObstacleMask.value == 0 || sprayVfxObject == null)
        {
            return true;
        }

        if (HasClearSprayPath(aimPosition))
        {
            return true;
        }

        // 발쪽이 막히면 몸통 높이 정도로 한 번만 올려서 재시도합니다.
        Vector3 retryPosition = aimPosition + GetCombatSurfaceNormal() * sprayRetryTargetLift;
        if (HasClearSprayPath(retryPosition))
        {
            aimPosition = retryPosition;
            return true;
        }

        return false;
    }

    private bool HasClearSprayPath(Vector3 aimPosition)
    {
        Vector3 origin = sprayVfxObject.transform.position;
        Vector3 toTarget = aimPosition - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.0001f)
        {
            return true;
        }

        return !Physics.SphereCast(
            origin,
            sprayObstacleCheckRadius,
            toTarget / distance,
            out _,
            distance,
            sprayObstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private static Vector3 GetLocalAimAxis(SprayAimAxis axis)
    {
        return axis switch
        {
            SprayAimAxis.Back => Vector3.back,
            SprayAimAxis.Right => Vector3.right,
            SprayAimAxis.Left => Vector3.left,
            SprayAimAxis.Up => Vector3.up,
            SprayAimAxis.Down => Vector3.down,
            _ => Vector3.forward
        };
    }

    private static Vector3 GetWorldAimAxis(Transform source, SprayAimAxis axis)
    {
        return axis switch
        {
            SprayAimAxis.Back => -source.forward,
            SprayAimAxis.Right => source.right,
            SprayAimAxis.Left => -source.right,
            SprayAimAxis.Up => source.up,
            SprayAimAxis.Down => -source.up,
            _ => source.forward
        };
    }

    private void TryApplySprayDamage(Transform target)
    {
        if (Random.value > sprayHitChance)
        {
            return;
        }

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        lastSprayHit = true;
        playerHealth.ApplyDamage(sprayDamage);
    }

    private void TryApplyDirectMeleeDamage(Transform target)
    {
        if (!applyDirectMeleeDamage)
        {
            return;
        }

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        if (Vector3.Distance(GetOriginPosition(), target.position) <= directMeleeDamageRadius)
        {
            playerHealth.ApplyDamage(meleeDamage);
        }
    }

    private Vector3 GetOriginPosition()
    {
        return navTarget != null ? navTarget.position : transform.position;
    }

    private Transform GetSprayTarget()
    {
        if (stateMachine != null && stateMachine.Target != null)
        {
            return stateMachine.Target;
        }

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        return playerHealth != null ? playerHealth.transform : null;
    }

    private Vector3 GetTargetFeetPosition(Transform target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            Bounds bounds = targetCollider.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        return target.position;
    }

    private Vector3 GetCombatSurfaceNormal()
    {
        Vector3 gravityDirection = combatGravityDirection.sqrMagnitude < Mathf.Epsilon
            ? Vector3.down
            : combatGravityDirection.normalized;
        return -gravityDirection;
    }

    private Vector3 GetPlanarDirectionToTarget(Vector3 targetPosition, Vector3 origin, Vector3 surfaceNormal)
    {
        Vector3 direction = Vector3.ProjectOnPlane(targetPosition - origin, surfaceNormal);
        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        Vector3 fallback = Vector3.ProjectOnPlane(navTarget.forward, surfaceNormal);
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : transform.forward;
    }

    private void SetRouteMoverPaused(bool paused)
    {
        if (routeMover != null)
        {
            routeMover.SetMovementLocked(paused);
        }
    }

    private void SetSprayVfxActive(bool active)
    {
        if (sprayVfxObject != null)
        {
            sprayVfxObject.SetActive(active);
        }
    }

    private void PlaySprayParticles()
    {
        if (sprayVfxObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = sprayVfxObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }
    }

    private void DisableCylinderRenderers(Transform root)
    {
        foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (meshRenderer.name.Contains("SM_Cylinder_Long_01"))
            {
                meshRenderer.enabled = false;
            }
        }
    }

    private void ResolveDefaultLayers()
    {
        if (sprayObstacleMask.value == 0)
        {
            sprayObstacleMask = LayerMask.GetMask("Obstacle");
        }
    }

    private void KillAttackSequence(bool complete)
    {
        if (attackSequence == null)
        {
            return;
        }

        attackSequence.Kill(complete);
        attackSequence = null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildContainsRecursive(Transform root, string namePart)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name.Contains(namePart))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildContainsRecursive(root.GetChild(i), namePart);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        meleeCooldown = Mathf.Max(0f, meleeCooldown);
        crouchHalfDuration = Mathf.Max(0.01f, crouchHalfDuration);
        crouchDistance = Mathf.Max(0f, crouchDistance);
        lungeDuration = Mathf.Max(0.01f, lungeDuration);
        recoverDuration = Mathf.Max(0.01f, recoverDuration);
        meleeStopDistance = Mathf.Max(0f, meleeStopDistance);
        meleePassThroughDistance = Mathf.Max(0f, meleePassThroughDistance);
        recoverBackDistance = Mathf.Max(0f, recoverBackDistance);
        meleeTargetSurfaceOffset = Mathf.Max(0f, meleeTargetSurfaceOffset);
        secondLungeDelay = Mathf.Max(0f, secondLungeDelay);
        meleeDamage = Mathf.Max(0, meleeDamage);
        directMeleeDamageRadius = Mathf.Max(0f, directMeleeDamageRadius);
        sprayMinDistance = Mathf.Max(0f, sprayMinDistance);
        sprayMaxDistance = Mathf.Max(sprayMinDistance, sprayMaxDistance);
        sprayCooldown = Mathf.Max(0f, sprayCooldown);
        sprayDuration = Mathf.Max(0.01f, sprayDuration);
        sprayBeforeRouteResumeSeconds = Mathf.Max(0f, sprayBeforeRouteResumeSeconds);
        sprayObstacleCheckRadius = Mathf.Max(0.01f, sprayObstacleCheckRadius);
        sprayRetryTargetLift = Mathf.Max(0f, sprayRetryTargetLift);
        sprayDamage = Mathf.Max(0, sprayDamage);
        faceTargetDuration = Mathf.Max(0f, faceTargetDuration);
        playerTag = string.IsNullOrWhiteSpace(playerTag) ? "Player" : playerTag;
        if (combatGravityDirection.sqrMagnitude < Mathf.Epsilon)
        {
            combatGravityDirection = Vector3.down;
        }
    }
}
