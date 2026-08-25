using DG.Tweening;
using UnityEngine;

/// <summary>
/// 지네가 Attack 상태일 때 Nav Target을 들어올린 뒤 플레이어 쪽으로 짧게 돌진하고 빠지는 공격 연출을 수행합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CentipedeLungeAttack : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum AttackDebugStatus
    {
        Ready,
        NoNavTarget,
        NoStateMachine,
        NoTarget,
        WaitingForAttackState,
        Cooldown,
        Windup,
        Lunge,
        Recover
    }

    [Header("References")]
    [SerializeField] private Transform navTarget;
    [SerializeField] private GravityState gravityState;
    [SerializeField] private MonsterStateMachine stateMachine;
    [SerializeField, Tooltip("공격/이동/사망 사운드를 재생합니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterAudioFeedback audioFeedback;

    [Header("Timing")]
    [SerializeField, Min(0f), Tooltip("공격이 한 번 끝난 뒤 다음 공격까지 기다리는 시간입니다.")]
    private float cooldown = 1.2f;
    [SerializeField, Min(0.01f), Tooltip("머리를 들어올려 공격 준비 자세를 만드는 시간입니다.")]
    private float windupDuration = 0.18f;
    [SerializeField, Min(0.01f), Tooltip("플레이어를 향해 빠르게 돌진하는 시간입니다.")]
    private float lungeDuration = 0.12f;
    [SerializeField, Min(0.01f), Tooltip("돌진 후 뒤로 빠져 다시 추적 자세로 돌아가는 시간입니다.")]
    private float recoverDuration = 0.22f;

    [Header("Shape")]
    [SerializeField, Min(0f), Tooltip("공격 준비 때 NavTarget을 현재 바닥 normal 방향으로 들어올리는 높이입니다.")]
    private float windupLiftHeight = 0.5f;
    [SerializeField, Min(0f), Tooltip("공격 준비 때 살짝 뒤로 빠지는 거리입니다. 0이면 제자리에서 머리만 듭니다.")]
    private float windupPullBackDistance = 0.15f;
    [SerializeField, Min(0f), Tooltip("돌진 도착점이 플레이어 중심에서 얼마나 떨어질지 정합니다. 0에 가까울수록 거의 플레이어 위치까지 갑니다.")]
    private float lungeStopDistance = 0.05f;
    [SerializeField, Min(0f), Tooltip("돌진 후 뒤로 빠지는 거리입니다.")]
    private float recoverBackDistance = 0.45f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 공격 연출 단계입니다. 런타임 확인용입니다.")]
    private AttackDebugStatus attackDebugStatus = AttackDebugStatus.Ready;
    [SerializeField, Tooltip("다음 공격 가능 시간까지 남은 초입니다.")]
    private float cooldownRemaining;
    [SerializeField, Tooltip("이번 공격에서 계산된 돌진 도착 월드 좌표입니다.")]
    private Vector3 lastLungePosition;
    [SerializeField, Tooltip("돌진이 끝난 뒤 빠져나올 월드 좌표입니다. 이 지점이 공격 후 TargetDistance에 큰 영향을 줍니다.")]
    private Vector3 lastRecoverPosition;

    private Sequence attackSequence;
    private float nextAttackTime;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void Reset()
    {
        ResolveSceneReferences();
    }

    private void Update()
    {
        if (!CanStartAttack())
        {
            return;
        }

        StartLungeAttack();
    }

    private void OnDisable()
    {
        KillAttackSequence(false);
    }

    public void ResetMonsterRuntime()
    {
        ResolveSceneReferences();
        KillAttackSequence(false);
        nextAttackTime = 0f;
        cooldownRemaining = 0f;
        attackDebugStatus = AttackDebugStatus.Ready;
    }

    public void OnMonsterDied()
    {
        KillAttackSequence(false);
        attackDebugStatus = AttackDebugStatus.Cooldown;
    }

    /// <summary>
    /// Inspector 참조가 비어 있어도 지네 prefab 계층에서 필요한 참조를 자동으로 찾습니다.
    /// </summary>
    private void ResolveSceneReferences()
    {
        if (navTarget == null)
        {
            navTarget = transform.Find("Nav Target");
        }

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        audioFeedback ??= GetComponent<MonsterAudioFeedback>();
        audioFeedback ??= GetComponentInParent<MonsterAudioFeedback>();
        audioFeedback ??= GetComponentInChildren<MonsterAudioFeedback>();

        if (gravityState == null)
        {
            gravityState = FindGravityState();
        }
    }

    private GravityState FindGravityState()
    {
        GameObject gravitySystem = GameObject.Find("GravitySystem");
        if (gravitySystem != null && gravitySystem.TryGetComponent(out GravityState namedGravityState))
        {
            return namedGravityState;
        }

        return FindFirstObjectByType<GravityState>();
    }

    /// <summary>
    /// 현재 상태와 쿨다운을 보고 새 돌진 공격을 시작할 수 있는지 판단합니다.
    /// </summary>
    private bool CanStartAttack()
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

        if (stateMachine.Target == null)
        {
            attackDebugStatus = AttackDebugStatus.NoTarget;
            return false;
        }

        if (attackSequence != null && attackSequence.IsActive())
        {
            return false;
        }

        cooldownRemaining = Mathf.Max(0f, nextAttackTime - Time.time);
        if (cooldownRemaining > 0f)
        {
            attackDebugStatus = AttackDebugStatus.Cooldown;
            return false;
        }

        if (stateMachine.State != MonsterState.Attack)
        {
            attackDebugStatus = AttackDebugStatus.WaitingForAttackState;
            return false;
        }

        return true;
    }

    /// <summary>
    /// NavTarget을 들어올리는 준비 동작, 빠른 돌진, 뒤로 빠지는 회복 동작을 하나의 DOTween 시퀀스로 실행합니다.
    /// </summary>
    private void StartLungeAttack()
    {
        Transform target = stateMachine.Target;
        Vector3 startPosition = navTarget.position;
        Vector3 surfaceNormal = GetSurfaceNormal();
        Vector3 attackDirection = GetPlanarDirectionToTarget(target.position, startPosition, surfaceNormal);

        Vector3 planarTargetPosition = startPosition + Vector3.ProjectOnPlane(target.position - startPosition, surfaceNormal);
        Vector3 windupPosition = startPosition + surfaceNormal * windupLiftHeight - attackDirection * windupPullBackDistance;
        Vector3 lungePosition = planarTargetPosition - attackDirection * lungeStopDistance;
        Vector3 recoverPosition = startPosition - attackDirection * recoverBackDistance;
        lastLungePosition = lungePosition;
        lastRecoverPosition = recoverPosition;

        KillAttackSequence(false);
        attackSequence = DOTween.Sequence()
            .SetTarget(this)
            .AppendCallback(() => attackDebugStatus = AttackDebugStatus.Windup)
            .Append(navTarget.DOMove(windupPosition, windupDuration).SetEase(Ease.OutBack))
            .AppendCallback(() =>
            {
                attackDebugStatus = AttackDebugStatus.Lunge;
                audioFeedback?.PlayBodySlam();
            })
            .Append(navTarget.DOMove(lungePosition, lungeDuration).SetEase(Ease.InQuad))
            .AppendCallback(() => attackDebugStatus = AttackDebugStatus.Recover)
            .Append(navTarget.DOMove(recoverPosition, recoverDuration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                nextAttackTime = Time.time + cooldown;
                cooldownRemaining = cooldown;
                attackDebugStatus = AttackDebugStatus.Cooldown;
            });
    }

    private Vector3 GetSurfaceNormal()
    {
        Vector3 normal = gravityState != null ? -gravityState.Direction : Vector3.up;
        return normal.sqrMagnitude < Mathf.Epsilon ? Vector3.up : normal.normalized;
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

    private void KillAttackSequence(bool complete)
    {
        if (attackSequence == null)
        {
            return;
        }

        attackSequence.Kill(complete);
        attackSequence = null;
    }

    private void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        windupDuration = Mathf.Max(0.01f, windupDuration);
        lungeDuration = Mathf.Max(0.01f, lungeDuration);
        recoverDuration = Mathf.Max(0.01f, recoverDuration);
        windupLiftHeight = Mathf.Max(0f, windupLiftHeight);
        windupPullBackDistance = Mathf.Max(0f, windupPullBackDistance);
        lungeStopDistance = Mathf.Max(0f, lungeStopDistance);
        recoverBackDistance = Mathf.Max(0f, recoverBackDistance);
    }
}
