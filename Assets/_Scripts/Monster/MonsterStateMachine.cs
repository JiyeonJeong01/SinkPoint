using UnityEngine;

/// <summary>
/// 몬스터 상태 전환의 공통 뼈대입니다.
/// 이동 방식은 각 Mover가 담당하고, 이 컴포넌트는 감지 결과에 따라 RouteMove/Chase/Attack 정도만 결정합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterTargetSensor))]
public sealed class MonsterStateMachine : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attackRange = 1.5f;
    [SerializeField] private MonsterState initialState = MonsterState.RouteMove;

    private MonsterTargetSensor targetSensor;
    private MonsterHealth health;
    private MonsterState state;

    public MonsterState State => state;
    public Transform Target => targetSensor != null ? targetSensor.CurrentTarget : null;
    public float AttackRange => attackRange;

    private void Awake()
    {
        ResolveSceneReferences();
        state = initialState;

        if (health != null)
        {
            health.Died += OnDied;
        }
    }

    /// <summary>
    /// Inspector 배치가 조금 달라도 같은 몬스터 계층 안에서 필요한 컴포넌트를 자동으로 찾습니다.
    /// </summary>
    private void ResolveSceneReferences()
    {
        targetSensor = GetComponent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInParent<MonsterTargetSensor>();
        targetSensor ??= GetComponentInChildren<MonsterTargetSensor>();

        health = GetComponent<MonsterHealth>();
        health ??= GetComponentInParent<MonsterHealth>();
        health ??= GetComponentInChildren<MonsterHealth>();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }

    private void Update()
    {
        if (state == MonsterState.Dead || state == MonsterState.Falling)
        {
            return;
        }

        UpdateCombatState();
    }

    /// <summary>
    /// 대상 감지 상태와 공격 거리를 기준으로 큰 상태만 갱신합니다.
    /// </summary>
    private void UpdateCombatState()
    {
        Transform target = Target;
        if (target == null)
        {
            state = initialState;
            return;
        }

        float sqrAttackRange = attackRange * attackRange;
        state = (target.position - transform.position).sqrMagnitude <= sqrAttackRange
            ? MonsterState.Attack
            : MonsterState.Chase;
    }

    /// <summary>
    /// 표면을 잃거나 중력 반전으로 현재 면을 못 쓰게 됐을 때 외부 Mover가 호출합니다.
    /// </summary>
    public void EnterFalling()
    {
        if (state != MonsterState.Dead)
        {
            state = MonsterState.Falling;
        }
    }

    /// <summary>
    /// 낙하 후 새 표면에 붙었을 때 외부 Mover가 일반 이동 상태로 복귀시킵니다.
    /// </summary>
    public void ExitFalling()
    {
        if (state == MonsterState.Falling)
        {
            state = initialState;
        }
    }

    private void OnDied(MonsterHealth monsterHealth)
    {
        state = MonsterState.Dead;
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
    }
}
