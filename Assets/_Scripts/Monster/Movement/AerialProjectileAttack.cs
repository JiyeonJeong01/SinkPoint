using System.Collections;
using UnityEngine;

/// <summary>
/// 공중 몬스터의 원거리 투사체 공격을 담당합니다.
/// 같은 투사체 prefab을 부채꼴 3발 또는 일렬 3발 패턴으로 발사합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AerialProjectileAttack : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private enum AttackPattern
    {
        FanThree,
        LineThree
    }

    private enum AttackDebugStatus
    {
        Ready,
        NoTarget,
        NoFirePoint,
        NoProjectilePrefab,
        Dead,
        Cooldown,
        Firing
    }

    [Header("References")]
    [SerializeField, Tooltip("투사체가 생성될 위치입니다. 비워두면 NavTarget 또는 이 오브젝트를 사용합니다.")]
    private Transform firePoint;
    [SerializeField, Tooltip("발사할 VFX/투사체 prefab입니다. MonsterProjectile이 없어도 런타임에 자동으로 붙입니다.")]
    private GameObject projectilePrefab;
    [SerializeField, Tooltip("플레이어 탐지 결과를 읽습니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterTargetSensor targetSensor;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterStateMachine stateMachine;
    [SerializeField, Tooltip("사망 상태 확인용입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterHealth monsterHealth;
    [SerializeField, Tooltip("투사체가 자기 몸 콜라이더를 무시할 때 기준이 되는 루트입니다. 비워두면 이 Transform을 사용합니다.")]
    private Transform ownerRoot;

    [Header("Attack")]
    [SerializeField, Min(0f), Tooltip("한 번 공격한 뒤 다음 공격까지 기다리는 시간입니다.")]
    private float attackInterval = 2.5f;
    [SerializeField, Min(0f), Tooltip("발사 전 플레이어 방향으로 몸을 돌리는 시간입니다.")]
    private float faceTargetDuration = 0.15f;
    [SerializeField, Min(0f), Tooltip("일렬 3발 발사에서 각 투사체 사이 시간입니다.")]
    private float lineShotInterval = 0.12f;
    [SerializeField, Range(0f, 45f), Tooltip("부채꼴 3발의 좌우 각도입니다.")]
    private float fanAngle = 14f;
    [SerializeField, Tooltip("켜면 부채꼴/일렬 패턴을 번갈아 사용합니다. 끄면 랜덤으로 고릅니다.")]
    private bool alternatePatterns = true;

    [Header("Projectile")]
    [SerializeField, Min(0f), Tooltip("투사체 속도입니다.")]
    private float projectileSpeed = 12f;
    [SerializeField, Min(0f), Tooltip("투사체가 자동으로 사라질 시간입니다.")]
    private float projectileLifetime = 4f;
    [SerializeField, Min(0), Tooltip("투사체가 플레이어에게 줄 피해량입니다.")]
    private int projectileDamage = 1;
    [SerializeField, Min(0f), Tooltip("플레이어 발보다 살짝 위를 조준할 높이입니다.")]
    private float targetAimHeight = 0.8f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 공격 상태입니다.")]
    private AttackDebugStatus attackDebugStatus = AttackDebugStatus.Ready;
    [SerializeField, Tooltip("다음 공격까지 남은 시간입니다.")]
    private float cooldownRemaining;
    [SerializeField, Tooltip("마지막으로 사용한 발사 패턴입니다.")]
    private AttackPattern lastPattern;

    private float nextAttackTime;
    private Coroutine attackRoutine;
    private int patternIndex;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (attackRoutine != null)
        {
            return;
        }

        if (IsDead())
        {
            attackDebugStatus = AttackDebugStatus.Dead;
            return;
        }

        cooldownRemaining = Mathf.Max(0f, nextAttackTime - Time.time);
        if (cooldownRemaining > 0f)
        {
            attackDebugStatus = AttackDebugStatus.Cooldown;
            return;
        }

        Transform target = targetSensor != null ? targetSensor.CurrentTarget : null;
        if (target == null)
        {
            attackDebugStatus = AttackDebugStatus.NoTarget;
            return;
        }

        if (firePoint == null)
        {
            attackDebugStatus = AttackDebugStatus.NoFirePoint;
            return;
        }

        if (projectilePrefab == null)
        {
            attackDebugStatus = AttackDebugStatus.NoProjectilePrefab;
            return;
        }

        attackRoutine = StartCoroutine(FireRoutine(target));
    }

    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        StopAttackRoutine();
        nextAttackTime = 0f;
        cooldownRemaining = 0f;
        attackDebugStatus = AttackDebugStatus.Ready;
    }

    public void OnMonsterDied()
    {
        StopAttackRoutine();
        attackDebugStatus = AttackDebugStatus.Dead;
    }

    private void ResolveReferences()
    {
        firePoint ??= FindChildRecursive(transform, "NavTarget");
        firePoint ??= FindChildRecursive(transform, "Nav Target");
        firePoint ??= transform;

        targetSensor ??= GetComponent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInParent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInChildren<MonsterTargetSensor>();

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        monsterHealth ??= GetComponent<MonsterHealth>();
        monsterHealth ??= GetComponentInParent<MonsterHealth>();
        monsterHealth ??= GetComponentInChildren<MonsterHealth>();

        ownerRoot ??= transform;
    }

    /// <summary>
    /// 플레이어를 잠깐 바라본 뒤, 선택된 패턴에 맞춰 투사체 3발을 생성합니다.
    /// </summary>
    private IEnumerator FireRoutine(Transform target)
    {
        attackDebugStatus = AttackDebugStatus.Firing;
        float faceEndTime = Time.time + faceTargetDuration;
        while (Time.time < faceEndTime)
        {
            if (IsDead() || target == null)
            {
                attackRoutine = null;
                yield break;
            }

            RotateFirePointToward(target.position + Vector3.up * targetAimHeight, Time.deltaTime);
            yield return null;
        }

        Vector3 aimDirection = GetAimDirection(target);
        AttackPattern pattern = PickPattern();
        lastPattern = pattern;

        if (pattern == AttackPattern.FanThree)
        {
            FireFan(aimDirection);
        }
        else
        {
            yield return FireLine(aimDirection);
        }

        nextAttackTime = Time.time + attackInterval;
        cooldownRemaining = attackInterval;
        attackDebugStatus = AttackDebugStatus.Cooldown;
        attackRoutine = null;
    }

    private AttackPattern PickPattern()
    {
        if (!alternatePatterns)
        {
            return Random.value < 0.5f ? AttackPattern.FanThree : AttackPattern.LineThree;
        }

        AttackPattern pattern = patternIndex % 2 == 0 ? AttackPattern.FanThree : AttackPattern.LineThree;
        patternIndex++;
        return pattern;
    }

    private void FireFan(Vector3 centerDirection)
    {
        Vector3 up = Vector3.up;
        FireProjectile(Quaternion.AngleAxis(-fanAngle, up) * centerDirection);
        FireProjectile(centerDirection);
        FireProjectile(Quaternion.AngleAxis(fanAngle, up) * centerDirection);
    }

    private IEnumerator FireLine(Vector3 direction)
    {
        for (int i = 0; i < 3; i++)
        {
            if (IsDead())
            {
                yield break;
            }

            FireProjectile(direction);
            if (lineShotInterval > 0f && i < 2)
            {
                yield return new WaitForSeconds(lineShotInterval);
            }
        }
    }

    private void FireProjectile(Vector3 direction)
    {
        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction, Vector3.up));
        MonsterProjectile projectile = projectileObject.GetComponent<MonsterProjectile>();
        projectile ??= projectileObject.AddComponent<MonsterProjectile>();
        projectile.Initialize(ownerRoot, direction, projectileDamage, projectileSpeed, projectileLifetime);
    }

    private Vector3 GetAimDirection(Transform target)
    {
        Vector3 targetPosition = target.position + Vector3.up * targetAimHeight;
        Vector3 direction = targetPosition - firePoint.position;
        return direction.sqrMagnitude < 0.0001f ? firePoint.forward : direction.normalized;
    }

    private void RotateFirePointToward(Vector3 targetPosition, float deltaTime)
    {
        Vector3 direction = targetPosition - firePoint.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        firePoint.rotation = Quaternion.RotateTowards(firePoint.rotation, targetRotation, 720f * deltaTime);
    }

    private void StopAttackRoutine()
    {
        if (attackRoutine == null)
        {
            return;
        }

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private bool IsDead()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return true;
        }

        return stateMachine != null && stateMachine.State == MonsterState.Dead;
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

    private void OnValidate()
    {
        attackInterval = Mathf.Max(0f, attackInterval);
        faceTargetDuration = Mathf.Max(0f, faceTargetDuration);
        lineShotInterval = Mathf.Max(0f, lineShotInterval);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileLifetime = Mathf.Max(0f, projectileLifetime);
        projectileDamage = Mathf.Max(0, projectileDamage);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
    }
}
