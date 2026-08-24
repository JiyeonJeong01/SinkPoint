using UnityEngine;

/// <summary>
/// 몬스터 상태 전환의 공통 뼈대입니다.
/// 이동 방식은 각 Mover가 담당하고, 이 컴포넌트는 감지 결과에 따라 RouteMove/Chase/Attack 정도만 결정합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterTargetSensor))]
public sealed class MonsterStateMachine : MonoBehaviour
{
    [SerializeField, Tooltip("공격 거리 판정에 사용할 기준 Transform입니다. 비워두면 이 오브젝트 기준이고, 지네처럼 NavTarget이 실제 이동 기준이면 Nav Target을 넣습니다.")]
    private Transform distanceOrigin;
    [SerializeField, Min(0f)] private float attackRange = 1.5f;
    [SerializeField] private MonsterState initialState = MonsterState.RouteMove;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("켜면 몬스터 상태가 실제로 바뀌는 순간에만 Console에 이전 상태와 새 상태를 출력합니다.")]
    private bool logStateChanges;
    [SerializeField, Tooltip("현재 몬스터 상태입니다. 런타임 확인용이며, 상태 변경은 코드 흐름으로 처리합니다.")]
    private MonsterState currentState;
    [SerializeField, Tooltip("현재 타겟까지의 거리입니다. 타겟이 없으면 -1로 표시합니다.")]
    private float targetDistance = -1f;
    [SerializeField, Tooltip("현재 타겟 거리가 Attack Range 안에 들어왔는지 표시합니다.")]
    private bool isTargetInAttackRange;
    [SerializeField, Tooltip("Sensor가 놓친 경우에도 디버그 거리 표시용으로 찾은 플레이어입니다. 상태 전환에는 Sensor Target만 사용합니다.")]
    private Transform debugDistanceTarget;

    private MonsterTargetSensor targetSensor;
    private MonsterHealth health;

    public MonsterState State => currentState;
    public Transform Target => targetSensor != null ? targetSensor.CurrentTarget : null;
    public float AttackRange => attackRange;
    public Transform DistanceOrigin => distanceOrigin != null ? distanceOrigin : transform;

    private void Awake()
    {
        ResolveSceneReferences();
        currentState = initialState;

        if (health != null)
        {
            health.Died += OnDied;
        }
    }

    private void Reset()
    {
        ResolveSceneReferences();
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

        if (distanceOrigin == null)
        {
            Transform navTarget = FindNavTarget();
            if (navTarget != null)
            {
                distanceOrigin = navTarget;
            }
        }
    }

    private Transform FindNavTarget()
    {
        Transform navTarget = transform.Find("Nav Target");
        if (navTarget != null)
        {
            return navTarget;
        }

        return transform.Find("NavTarget");
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
        if (currentState == MonsterState.Dead || currentState == MonsterState.Falling)
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
            debugDistanceTarget = FindDebugDistanceTarget();
            targetDistance = debugDistanceTarget != null
                ? Vector3.Distance(debugDistanceTarget.position, DistanceOrigin.position)
                : -1f;
            isTargetInAttackRange = false;
            SetState(initialState);
            return;
        }

        Transform origin = DistanceOrigin;
        debugDistanceTarget = target;
        targetDistance = Vector3.Distance(target.position, origin.position);
        float sqrAttackRange = attackRange * attackRange;
        isTargetInAttackRange = targetDistance * targetDistance <= sqrAttackRange;
        SetState(isTargetInAttackRange ? MonsterState.Attack : MonsterState.Chase);
    }

    /// <summary>
    /// 표면을 잃거나 중력 반전으로 현재 면을 못 쓰게 됐을 때 외부 Mover가 호출합니다.
    /// </summary>
    public void EnterFalling()
    {
        if (currentState != MonsterState.Dead)
        {
            SetState(MonsterState.Falling);
        }
    }

    /// <summary>
    /// 낙하 후 새 표면에 붙었을 때 외부 Mover가 일반 이동 상태로 복귀시킵니다.
    /// </summary>
    public void ExitFalling()
    {
        if (currentState == MonsterState.Falling)
        {
            SetState(initialState);
        }
    }

    private void OnDied(MonsterHealth monsterHealth)
    {
        SetState(MonsterState.Dead);
    }

    /// <summary>
    /// 상태 변경을 한 지점으로 모아 중복 로그를 막고, 실제 전환이 일어날 때만 후처리합니다.
    /// </summary>
    private void SetState(MonsterState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        MonsterState previousState = currentState;
        currentState = nextState;

        if (logStateChanges)
        {
            Debug.Log($"[{nameof(MonsterStateMachine)}] {name}: {previousState} -> {currentState}", this);
        }
    }

    private Transform FindDebugDistanceTarget()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.transform;
        }

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        return playerInput != null ? playerInput.transform : null;
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
    }
}
