using UnityEngine;

/// <summary>
/// 몬스터가 플레이어를 감지하고 추적 대상으로 제공하는 컴포넌트입니다.
/// 실제 시야각/은신 판정이 생기기 전까지는 거리 기반 감지만 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterTargetSensor : MonoBehaviour
{
    [SerializeField, Tooltip("감지 거리 계산 기준점입니다. 비워두면 자식 Nav Target을 찾고, 없으면 이 오브젝트 기준으로 봅니다.")]
    private Transform sensingOrigin;
    [SerializeField] private Transform explicitTarget;
    [SerializeField, Min(0f)] private float detectionRadius = 12f;
    [SerializeField, Min(0f)] private float loseRadius = 16f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Tooltip("켜면 탐지 범위 밖에서 맞아도 일단 플레이어를 추적 대상으로 잡습니다.")]
    private bool aggroOnDamaged = true;
    [SerializeField, Min(0f), Tooltip("피격 후 탐지 범위와 무관하게 Chase를 유지할 시간입니다. 이후 Lose Radius 밖이면 대상을 잃습니다.")]
    private float damagedAggroSeconds = 1.5f;

    private Transform currentTarget;
    private MonsterHealth health;
    private float forceTargetUntil;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    private Transform SensingOrigin => sensingOrigin != null ? sensingOrigin : transform;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
        }
    }

    private void Reset()
    {
        ResolveSceneReferences();
    }

    private void Start()
    {
        ResolveInitialTarget();
    }

    /// <summary>
    /// Inspector 참조가 비어 있으면 지네처럼 실제 이동 기준이 되는 Nav Target을 감지 기준으로 사용합니다.
    /// </summary>
    private void ResolveSceneReferences()
    {
        if (sensingOrigin == null)
        {
            Transform navTarget = FindNavTarget();
            if (navTarget != null)
            {
                sensingOrigin = navTarget;
            }
        }

        MonsterHealth foundHealth = GetComponent<MonsterHealth>();
        foundHealth ??= GetComponentInParent<MonsterHealth>();
        foundHealth ??= GetComponentInChildren<MonsterHealth>();
        if (health != foundHealth)
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            health = foundHealth;
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Damaged += OnDamaged;
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

        navTarget = transform.Find("NavTarget");
        if (navTarget != null)
        {
            return navTarget;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform found = FindNavTargetRecursive(transform.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindNavTargetRecursive(Transform root)
    {
        if (root.name == "Nav Target" || root.name == "NavTarget")
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNavTargetRecursive(root.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void Update()
    {
        RefreshTarget();
    }

    /// <summary>
    /// Inspector에 대상이 비어 있으면 Player 태그 오브젝트를 한 번 찾아 초기 추적 대상으로 보관합니다.
    /// 감지 반경 밖이면 RefreshTarget에서 다시 null 처리됩니다.
    /// </summary>
    private void ResolveInitialTarget()
    {
        if (explicitTarget != null || string.IsNullOrWhiteSpace(playerTag))
        {
            return;
        }

        Transform player = FindPlayerTarget();
        if (player != null)
        {
            currentTarget = player;
        }
    }

    /// <summary>
    /// 현재 추적 대상이 있으면 유지 가능 여부를 확인하고, 없으면 주변 플레이어를 찾습니다.
    /// </summary>
    public void RefreshTarget()
    {
        if (explicitTarget != null)
        {
            currentTarget = explicitTarget;
            return;
        }

        if (currentTarget != null)
        {
            if (Time.time < forceTargetUntil)
            {
                return;
            }

            float sqrLoseRadius = loseRadius * loseRadius;
            if ((currentTarget.position - SensingOrigin.position).sqrMagnitude <= sqrLoseRadius)
            {
                return;
            }

            currentTarget = null;
        }

        Transform player = FindPlayerTarget();

        if (player == null)
        {
            return;
        }

        float sqrDetectionRadius = detectionRadius * detectionRadius;
        if ((player.position - SensingOrigin.position).sqrMagnitude <= sqrDetectionRadius)
        {
            currentTarget = player;
        }
    }

    /// <summary>
    /// 우선 Player 태그를 찾고, 테스트 씬처럼 태그가 빠진 경우에는 PlayerInput 컴포넌트를 가진 오브젝트를 사용합니다.
    /// </summary>
    private Transform FindPlayerTarget()
    {
        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }
        }

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        return playerInput != null ? playerInput.transform : null;
    }

    private void OnDamaged(MonsterHealth monsterHealth, int amount)
    {
        if (!aggroOnDamaged)
        {
            return;
        }

        Transform player = FindPlayerTarget();
        if (player == null)
        {
            return;
        }

        currentTarget = player;
        forceTargetUntil = Time.time + damagedAggroSeconds;
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        loseRadius = Mathf.Max(detectionRadius, loseRadius);
        damagedAggroSeconds = Mathf.Max(0f, damagedAggroSeconds);
    }
}
