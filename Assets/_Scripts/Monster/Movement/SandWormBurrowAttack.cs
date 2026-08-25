using DG.Tweening;
using UnityEngine;

/// <summary>
/// Inversion Zone SandWorm의 잠복 공격을 담당합니다.
/// 평소 이동은 지네처럼 다른 이동 컴포넌트가 NavTarget을 끌고 가고, 이 컴포넌트는 일정 주기마다 이동을 잠깐 멈춘 뒤
/// 현재 중력 기준 지면 아래로 파고들고, 저장한 플레이어 위치 근처의 현재 바닥을 다시 찾아 솟구치며 몸빵 피해를 줍니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SandWormBurrowAttack : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum BurrowDebugStatus
    {
        Ready,
        NoNavTarget,
        NoTarget,
        Dead,
        Cooldown,
        Burrowing,
        Underground,
        FallingToCurrentGround,
        Emerging,
        Recovering,
        GroundNotFound
    }

    [Header("References")]
    [SerializeField, Tooltip("앞쪽을 끌어당기는 기준 Transform입니다. 비워두면 NavTarget/Nav Target을 찾습니다.")]
    private Transform navTarget;
    [SerializeField, Tooltip("플레이어 탐지 결과를 읽습니다. 비워두면 같은 계층에서 찾습니다.")]
    private MonsterTargetSensor targetSensor;
    [SerializeField, Tooltip("현재 중력 방향을 읽습니다. 비워두면 GravitySystem 또는 GravityState를 찾습니다.")]
    private GravityState gravityState;
    [SerializeField, Tooltip("사망 상태를 확인합니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterStateMachine stateMachine;
    [SerializeField, Tooltip("사망 이벤트를 직접 받아 잠복 공격을 중단합니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterHealth monsterHealth;
    [SerializeField, Tooltip("잠복 중 멈출 이동 컴포넌트입니다. 비워두면 같은 오브젝트의 CentipedeFloorMover를 자동으로 사용합니다.")]
    private Behaviour[] movementBehaviours;
    [Header("Timing")]
    [SerializeField, Min(0f), Tooltip("잠복 공격이 한 번 끝난 뒤 다음 공격까지 기다리는 시간입니다.")]
    private float attackInterval = 4f;
    [SerializeField, Min(0.01f), Tooltip("지면 아래로 빠르게 파고드는 시간입니다.")]
    private float burrowDuration = 0.12f;
    [SerializeField, Min(0f), Tooltip("NavTarget이 먼저 깊게 들어간 뒤 몸 전체가 따라올 시간을 줍니다.")]
    private float bodyFollowUndergroundDelay = 0.8f;
    [SerializeField, Min(0f), Tooltip("땅 밑에서 대기하는 시간입니다.")]
    private float undergroundDuration = 0.45f;
    [SerializeField, Min(0.01f), Tooltip("현재 중력 기준 바닥으로 위치를 보정하는 시간입니다.")]
    private float fallToGroundDuration = 0.25f;
    [SerializeField, Min(0.01f), Tooltip("땅 밖으로 솟구치는 시간입니다.")]
    private float emergeDuration = 0.16f;
    [SerializeField, Min(0f), Tooltip("등장 후 다시 기본 이동으로 돌아가기 전 잠깐 멈추는 시간입니다.")]
    private float recoverDuration = 0.25f;

    [Header("Shape")]
    [SerializeField, Min(0f), Tooltip("지면 아래로 얼마나 깊게 들어갈지 정합니다.")]
    private float burrowDepth = 2.2f;
    [SerializeField, Min(0f), Tooltip("몸 전체가 사라져야 하므로 실제 잠복 깊이는 이 값보다 작아지지 않습니다.")]
    private float minimumFullBodyBurrowDepth = 14f;
    [SerializeField, Min(0f), Tooltip("잠복할 때 표면 진행 방향으로 추가 이동할 거리입니다. 길수록 몸통이 실제로 빨려 들어갑니다.")]
    private float burrowForwardDragDistance = 18f;
    [SerializeField, Min(0f), Tooltip("등장 시작 위치를 지면 아래로 얼마나 둘지 정합니다.")]
    private float emergeStartDepth = 1.8f;
    [SerializeField, Min(0f), Tooltip("몸 전체가 지면 아래에서 나오도록 실제 등장 시작 깊이는 이 값보다 작아지지 않습니다.")]
    private float minimumFullBodyEmergeDepth = 12f;
    [SerializeField, Min(0f), Tooltip("등장 완료 위치를 지면 위로 얼마나 띄울지 정합니다. 몸빵 느낌을 키우려면 조금 올립니다.")]
    private float emergeSurfaceOffset = 1.2f;
    [SerializeField, Min(0f), Tooltip("머리만 빼꼼 나오지 않도록 실제 등장 높이가 이 값보다 작아지지 않게 합니다.")]
    private float minimumEmergeSurfaceOffset = 1.2f;
    [SerializeField, Min(0f), Tooltip("플레이어 위치 정중앙 대신 주변에서 나올 거리입니다. 0이면 플레이어 위치 기준으로 나옵니다.")]
    private float emergeSideOffset = 0.8f;

    [Header("Ground Probe")]
    [SerializeField, Tooltip("현재 바닥을 찾을 레이어입니다. 비워두면 WalkableSurface를 사용하고, 없으면 전체 레이어를 검사합니다.")]
    private LayerMask groundMask;
    [SerializeField, Min(0f), Tooltip("Raycast 시작점을 중력 반대 방향으로 띄우는 거리입니다. 충분히 크게 두면 뒤집힌 중력에서도 바닥을 더 잘 찾습니다.")]
    private float groundRayStartOffset = 8f;
    [SerializeField, Min(0.01f), Tooltip("현재 중력 방향으로 바닥을 찾을 최대 거리입니다.")]
    private float groundRayDistance = 40f;
    [SerializeField, Min(0f), Tooltip("첫 Raycast가 실패했을 때 플레이어 주변으로 추가 검사할 반경입니다.")]
    private float groundProbeSpread = 2f;

    [Header("Damage")]
    [SerializeField, Min(0), Tooltip("등장 몸빵으로 줄 피해량입니다.")]
    private int damage = 1;
    [SerializeField, Min(0f), Tooltip("등장 순간 플레이어가 이 거리 안에 있으면 피해를 줍니다.")]
    private float damageRadius = 1.6f;
    [SerializeField, Range(0f, 1f), Tooltip("등장 공격이 실제 피해로 이어질 확률입니다. 0.33이면 약 3번 중 1번만 맞습니다.")]
    private float hitChance = 0.33f;
    [SerializeField, Tooltip("켜면 한 번의 등장 공격당 한 번만 피해를 줍니다.")]
    private bool damageOncePerAttack = true;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 잠복 공격 단계입니다. 런타임 확인용입니다.")]
    private BurrowDebugStatus burrowDebugStatus = BurrowDebugStatus.Ready;
    [SerializeField, Tooltip("다음 잠복 공격까지 남은 시간입니다.")]
    private float cooldownRemaining;
    [SerializeField, Tooltip("등장 직전에 다시 읽은 플레이어 위치입니다.")]
    private Vector3 capturedTargetPosition;
    [SerializeField, Tooltip("마지막으로 찾은 현재 중력 기준 바닥 위치입니다.")]
    private Vector3 lastGroundPoint;
    [SerializeField, Tooltip("마지막 바닥 Raycast가 성공했는지 표시합니다.")]
    private bool lastGroundHit;
    [SerializeField, Tooltip("피해가 실제로 들어갔는지 표시합니다.")]
    private bool lastDamageApplied;
    [SerializeField, Tooltip("씬에서 선택했을 때 잠복/등장 위치와 바닥 검사 방향을 표시합니다.")]
    private bool drawDebugGizmos = true;

    private Sequence burrowSequence;
    private float nextAttackTime;
    private bool damageAppliedThisAttack;
    private Vector3 lastProbeOrigin;
    private Vector3 lastProbeDirection = Vector3.down;
    private Vector3 fallStartPosition;
    private Vector3 emergeStartPosition;
    private Vector3 emergeEndPosition;
    private Vector3 emergeGravityDirection = Vector3.down;
    private void Awake()
    {
        ResolveReferences();
        ResolveDefaultLayers();
        RegisterHealthEvent();
    }

    private void OnDestroy()
    {
        UnregisterHealthEvent();
    }

    private void Reset()
    {
        ResolveReferences();
        ResolveDefaultLayers();
    }

    private void Update()
    {
        if (!CanStartBurrow())
        {
            return;
        }

        StartBurrowAttack(targetSensor.CurrentTarget);
    }

    private void OnDisable()
    {
        KillBurrowSequence(false);
        SetMovementEnabled(!IsDead());
    }

    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        ResolveDefaultLayers();
        RegisterHealthEvent();
        KillBurrowSequence(false);
        SetMovementEnabled(true);
        nextAttackTime = 0f;
        cooldownRemaining = 0f;
        damageAppliedThisAttack = false;
        lastDamageApplied = false;
        burrowDebugStatus = BurrowDebugStatus.Ready;
    }

    public void OnMonsterDied()
    {
        KillBurrowSequence(false);
        SetMovementEnabled(false);
        burrowDebugStatus = BurrowDebugStatus.Dead;
    }

    private void ResolveReferences()
    {
        navTarget ??= FindChildRecursive(transform, "NavTarget");
        navTarget ??= FindChildRecursive(transform, "Nav Target");

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        monsterHealth ??= GetComponent<MonsterHealth>();
        monsterHealth ??= GetComponentInParent<MonsterHealth>();
        monsterHealth ??= GetComponentInChildren<MonsterHealth>();

        targetSensor ??= GetComponent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInParent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInChildren<MonsterTargetSensor>();

        if (gravityState == null)
        {
            GameObject gravitySystem = GameObject.Find("GravitySystem");
            if (gravitySystem != null)
            {
                gravitySystem.TryGetComponent(out gravityState);
            }
        }

        gravityState ??= FindFirstObjectByType<GravityState>();

        if (movementBehaviours == null || movementBehaviours.Length == 0)
        {
            CentipedeFloorMover floorMover = GetComponent<CentipedeFloorMover>();
            floorMover ??= GetComponentInParent<CentipedeFloorMover>();
            floorMover ??= GetComponentInChildren<CentipedeFloorMover>();
            movementBehaviours = floorMover != null
                ? new Behaviour[] { floorMover }
                : System.Array.Empty<Behaviour>();
        }
    }

    private void ResolveDefaultLayers()
    {
        if (groundMask.value == 0)
        {
            int walkableMask = LayerMask.GetMask("WalkableSurface");
            groundMask = walkableMask != 0 ? walkableMask : ~0;
        }
    }

    private bool CanStartBurrow()
    {
        if (IsDead())
        {
            KillBurrowSequence(false);
            SetMovementEnabled(false);
            burrowDebugStatus = BurrowDebugStatus.Dead;
            return false;
        }

        if (navTarget == null)
        {
            burrowDebugStatus = BurrowDebugStatus.NoNavTarget;
            return false;
        }

        if (targetSensor == null || targetSensor.CurrentTarget == null)
        {
            burrowDebugStatus = BurrowDebugStatus.NoTarget;
            return false;
        }

        if (burrowSequence != null && burrowSequence.IsActive())
        {
            return false;
        }

        cooldownRemaining = Mathf.Max(0f, nextAttackTime - Time.time);
        if (cooldownRemaining > 0f)
        {
            burrowDebugStatus = BurrowDebugStatus.Cooldown;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 땅속으로 먼저 사라진 뒤, 등장 직전에 플레이어 위치와 현재 중력 방향을 다시 읽어 바닥을 찾습니다.
    /// </summary>
    private void StartBurrowAttack(Transform target)
    {
        if (IsDead())
        {
            return;
        }

        capturedTargetPosition = target.position;
        damageAppliedThisAttack = false;
        lastDamageApplied = false;

        Vector3 initialGravityDirection = GetGravityDirection();
        Vector3 initialSurfaceNormal = -initialGravityDirection;
        Vector3 burrowDirection = GetBurrowForwardDirection(initialSurfaceNormal);
        Vector3 burrowPosition = navTarget.position
            + initialGravityDirection * GetEffectiveBurrowDepth()
            + burrowDirection * burrowForwardDragDistance;

        KillBurrowSequence(false);
        SetMovementEnabled(false);

        burrowSequence = DOTween.Sequence()
            .SetTarget(this)
            .AppendCallback(() =>
            {
                burrowDebugStatus = BurrowDebugStatus.Burrowing;
                AlignNavTargetToGravity(initialGravityDirection);
            })
            .Append(navTarget.DOMove(burrowPosition, burrowDuration).SetEase(Ease.InCubic))
            .AppendInterval(bodyFollowUndergroundDelay)
            .AppendCallback(() => burrowDebugStatus = BurrowDebugStatus.Underground)
            .AppendInterval(undergroundDuration)
            .AppendCallback(() => PrepareEmergePositions(target))
            .Append(DOVirtual.Float(0f, 1f, fallToGroundDuration, MoveTowardEmergeStart).SetEase(Ease.InOutSine))
            .AppendCallback(() =>
            {
                burrowDebugStatus = BurrowDebugStatus.Emerging;
                FaceTargetOnSurface(target != null ? target.position : capturedTargetPosition, emergeGravityDirection);
            })
            .Append(DOVirtual.Float(0f, 1f, emergeDuration, MoveTowardEmergeEnd).SetEase(Ease.OutBack))
            .AppendCallback(() => TryApplyEmergeDamage(target))
            .AppendCallback(() => burrowDebugStatus = BurrowDebugStatus.Recovering)
            .AppendInterval(recoverDuration)
            .OnComplete(() =>
            {
                if (IsDead())
                {
                    SetMovementEnabled(false);
                    burrowDebugStatus = BurrowDebugStatus.Dead;
                    return;
                }

                nextAttackTime = Time.time + attackInterval;
                cooldownRemaining = attackInterval;
                SetMovementEnabled(true);
                burrowDebugStatus = BurrowDebugStatus.Cooldown;
            });
    }

    private void PrepareEmergePositions(Transform target)
    {
        if (IsDead())
        {
            return;
        }

        emergeGravityDirection = GetGravityDirection();
        Vector3 currentSurfaceNormal = -emergeGravityDirection;
        capturedTargetPosition = target != null ? target.position : capturedTargetPosition;
        Vector3 emergeCenter = GetEmergeCenter(capturedTargetPosition, currentSurfaceNormal);

        if (!TryFindCurrentGround(emergeCenter, emergeGravityDirection, currentSurfaceNormal, out Vector3 groundPoint))
        {
            lastGroundHit = false;
            burrowDebugStatus = BurrowDebugStatus.GroundNotFound;
            groundPoint = emergeCenter;
        }

        lastGroundPoint = groundPoint;
        fallStartPosition = navTarget.position;
        emergeStartPosition = groundPoint + emergeGravityDirection * GetEffectiveEmergeStartDepth();
        emergeEndPosition = groundPoint + currentSurfaceNormal * GetEffectiveEmergeSurfaceOffset();

        burrowDebugStatus = BurrowDebugStatus.FallingToCurrentGround;
        FaceTargetOnSurface(capturedTargetPosition, emergeGravityDirection);
    }

    private void MoveTowardEmergeStart(float t)
    {
        if (navTarget == null || IsDead())
        {
            return;
        }

        navTarget.position = Vector3.LerpUnclamped(fallStartPosition, emergeStartPosition, t);
    }

    private void MoveTowardEmergeEnd(float t)
    {
        if (navTarget == null || IsDead())
        {
            return;
        }

        navTarget.position = Vector3.LerpUnclamped(emergeStartPosition, emergeEndPosition, t);
    }

    private Vector3 GetEmergeCenter(Vector3 targetPosition, Vector3 surfaceNormal)
    {
        if (emergeSideOffset <= 0f || navTarget == null)
        {
            return targetPosition;
        }

        Vector3 approach = Vector3.ProjectOnPlane(navTarget.position - targetPosition, surfaceNormal);
        if (approach.sqrMagnitude < 0.0001f)
        {
            approach = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        }

        if (approach.sqrMagnitude < 0.0001f)
        {
            approach = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }

        return targetPosition + approach.normalized * emergeSideOffset;
    }

    /// <summary>
    /// 저장된 위치 주변 여러 지점에서 현재 중력 방향으로 긴 Raycast를 쏴서, 중력 반전 이후의 실제 바닥을 찾습니다.
    /// </summary>
    private bool TryFindCurrentGround(Vector3 center, Vector3 gravityDirection, Vector3 surfaceNormal, out Vector3 groundPoint)
    {
        Vector3 tangentA = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        if (tangentA.sqrMagnitude < 0.0001f)
        {
            tangentA = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }

        if (tangentA.sqrMagnitude < 0.0001f)
        {
            tangentA = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal);
        }

        tangentA.Normalize();
        Vector3 tangentB = Vector3.Cross(surfaceNormal, tangentA).normalized;
        Vector3[] offsets =
        {
            Vector3.zero,
            tangentA * groundProbeSpread,
            -tangentA * groundProbeSpread,
            tangentB * groundProbeSpread,
            -tangentB * groundProbeSpread
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 origin = center + offsets[i] + surfaceNormal * groundRayStartOffset;
            lastProbeOrigin = origin;
            lastProbeDirection = gravityDirection;
            if (Physics.Raycast(
                origin,
                gravityDirection,
                out RaycastHit hit,
                groundRayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                lastGroundHit = true;
                groundPoint = hit.point;
                return true;
            }
        }

        groundPoint = center;
        return false;
    }

    private void TryApplyEmergeDamage(Transform target)
    {
        if (IsDead())
        {
            return;
        }

        if (damageOncePerAttack && damageAppliedThisAttack)
        {
            return;
        }

        PlayerHealth playerHealth = target != null ? target.GetComponentInParent<PlayerHealth>() : null;
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        if (Vector3.Distance(navTarget.position, playerHealth.transform.position) > damageRadius)
        {
            return;
        }

        if (Random.value > hitChance)
        {
            return;
        }

        damageAppliedThisAttack = true;
        lastDamageApplied = true;
        playerHealth.ApplyDamage(damage);
    }

    private bool IsDead()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return true;
        }

        return stateMachine != null && stateMachine.State == MonsterState.Dead;
    }

    private void RegisterHealthEvent()
    {
        if (monsterHealth == null)
        {
            return;
        }

        monsterHealth.Died -= OnHealthDied;
        monsterHealth.Died += OnHealthDied;
    }

    private void UnregisterHealthEvent()
    {
        if (monsterHealth != null)
        {
            monsterHealth.Died -= OnHealthDied;
        }
    }

    private void OnHealthDied(MonsterHealth health)
    {
        OnMonsterDied();
    }

    private Vector3 GetGravityDirection()
    {
        Vector3 direction = gravityState != null ? gravityState.Direction : Vector3.down;
        return direction.sqrMagnitude < Mathf.Epsilon ? Vector3.down : direction.normalized;
    }

    private float GetEffectiveBurrowDepth()
    {
        return Mathf.Max(burrowDepth, minimumFullBodyBurrowDepth);
    }

    private float GetEffectiveEmergeStartDepth()
    {
        return Mathf.Max(emergeStartDepth, minimumFullBodyEmergeDepth);
    }

    private float GetEffectiveEmergeSurfaceOffset()
    {
        return Mathf.Max(emergeSurfaceOffset, minimumEmergeSurfaceOffset);
    }

    private Vector3 GetBurrowForwardDirection(Vector3 surfaceNormal)
    {
        Vector3 forward = navTarget != null
            ? Vector3.ProjectOnPlane(navTarget.forward, surfaceNormal)
            : Vector3.zero;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f && targetSensor != null && targetSensor.CurrentTarget != null && navTarget != null)
        {
            forward = Vector3.ProjectOnPlane(
                targetSensor.CurrentTarget.position - navTarget.position,
                surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }

        return forward.normalized;
    }

    private void FaceTargetOnSurface(Vector3 targetPosition, Vector3 gravityDirection)
    {
        if (navTarget == null)
        {
            return;
        }

        Vector3 surfaceNormal = -gravityDirection;
        Vector3 forward = Vector3.ProjectOnPlane(targetPosition - navTarget.position, surfaceNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(navTarget.forward, surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }

        navTarget.rotation = Quaternion.LookRotation(forward.normalized, surfaceNormal);
    }

    private void AlignNavTargetToGravity(Vector3 gravityDirection)
    {
        if (navTarget == null)
        {
            return;
        }

        Vector3 surfaceNormal = -gravityDirection;
        Vector3 forward = Vector3.ProjectOnPlane(navTarget.forward, surfaceNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }

        navTarget.rotation = Quaternion.LookRotation(forward.normalized, surfaceNormal);
    }

    private void SetMovementEnabled(bool enabled)
    {
        if (movementBehaviours == null)
        {
            return;
        }

        for (int i = 0; i < movementBehaviours.Length; i++)
        {
            Behaviour behaviour = movementBehaviours[i];
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = enabled;
            }
        }
    }

    private void KillBurrowSequence(bool complete)
    {
        if (burrowSequence == null)
        {
            return;
        }

        burrowSequence.Kill(complete);
        burrowSequence = null;
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

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = lastGroundHit ? Color.green : Color.red;
        Gizmos.DrawSphere(lastGroundPoint, 0.18f);
        Gizmos.DrawLine(lastProbeOrigin, lastProbeOrigin + lastProbeDirection.normalized * groundRayDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(capturedTargetPosition, damageRadius);
    }

    private void OnValidate()
    {
        attackInterval = Mathf.Max(0f, attackInterval);
        burrowDuration = Mathf.Max(0.01f, burrowDuration);
        bodyFollowUndergroundDelay = Mathf.Max(0f, bodyFollowUndergroundDelay);
        undergroundDuration = Mathf.Max(0f, undergroundDuration);
        fallToGroundDuration = Mathf.Max(0.01f, fallToGroundDuration);
        emergeDuration = Mathf.Max(0.01f, emergeDuration);
        recoverDuration = Mathf.Max(0f, recoverDuration);
        burrowDepth = Mathf.Max(0f, burrowDepth);
        minimumFullBodyBurrowDepth = Mathf.Max(0f, minimumFullBodyBurrowDepth);
        burrowForwardDragDistance = Mathf.Max(0f, burrowForwardDragDistance);
        emergeStartDepth = Mathf.Max(0f, emergeStartDepth);
        minimumFullBodyEmergeDepth = Mathf.Max(0f, minimumFullBodyEmergeDepth);
        emergeSurfaceOffset = Mathf.Max(0f, emergeSurfaceOffset);
        minimumEmergeSurfaceOffset = Mathf.Max(0f, minimumEmergeSurfaceOffset);
        emergeSideOffset = Mathf.Max(0f, emergeSideOffset);
        groundRayStartOffset = Mathf.Max(0f, groundRayStartOffset);
        groundRayDistance = Mathf.Max(0.01f, groundRayDistance);
        groundProbeSpread = Mathf.Max(0f, groundProbeSpread);
        damage = Mathf.Max(0, damage);
        damageRadius = Mathf.Max(0f, damageRadius);
        hitChance = Mathf.Clamp01(hitChance);
    }
}
