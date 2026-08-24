using System;
using UnityEngine;

/// <summary>
/// 씬에 배치된 몬스터 한 마리를 MonsterManager가 공통으로 다루기 위한 루트 컴포넌트입니다.
/// 체력 사망 이벤트를 외부로 중계하고, 리스폰 때 시작 포즈/체력/물리 상태를 한 번에 되돌립니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class Monster : MonoBehaviour
{
    private struct TransformPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    [Header("Zone")]
    [SerializeField, Tooltip("이 몬스터가 속한 진행 Zone입니다. MonsterManager가 Zone별 생존 수를 셀 때 사용합니다.")]
    private ZoneId zoneId;

    [Header("References")]
    [SerializeField, Tooltip("비워두면 자식/부모에서 자동으로 찾습니다.")]
    private MonsterHealth health;

    [SerializeField, Tooltip("리스폰 때 시작 포즈로 되돌릴 Transform들입니다. 비워두면 자신과 NavTarget을 자동으로 포함합니다.")]
    private Transform[] resetTransforms;

    [Header("Runtime Readout")]
    [SerializeField, Tooltip("런타임에서 이 몬스터가 사망 처리됐는지 확인하는 값입니다.")]
    private bool dead;

    private bool initialized;
    private TransformPose[] initialPoses;
    private Rigidbody[] rigidbodies;

    public ZoneId ZoneId => zoneId;
    public bool IsDead => health != null ? health.IsDead : dead;

    public event Action<Monster> Died;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnHealthDied;
        }
    }

    private void Reset()
    {
        ResolveReferences();
    }

    /// <summary>
    /// MonsterManager가 씬 시작 또는 수동 갱신 시 호출합니다.
    /// 비활성 몬스터도 첫 활성화 전부터 시작 포즈를 기록할 수 있게 방어합니다.
    /// </summary>
    public void InitializeForManager()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 플레이어 리스폰이나 Zone 재시작 때 호출합니다.
    /// 씬에 배치된 원래 위치/회전/스케일로 되돌리고, 체력과 내부 런타임 상태를 초기화합니다.
    /// </summary>
    public void ResetForRespawn()
    {
        EnsureInitialized();
        gameObject.SetActive(true);

        RestoreInitialPoses();
        ClearRigidbodyVelocity();

        if (health != null)
        {
            health.ResetHealth();
        }

        dead = false;
        ResetRuntimeComponents();
    }

    public void SetManagedActive(bool active)
    {
        EnsureInitialized();
        gameObject.SetActive(active);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        ResolveReferences();
        CaptureInitialPoses();
        rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        if (health != null)
        {
            health.Died -= OnHealthDied;
            health.Died += OnHealthDied;
            dead = health.IsDead;
        }

        initialized = true;
    }

    private void ResolveReferences()
    {
        health ??= GetComponent<MonsterHealth>();
        health ??= GetComponentInChildren<MonsterHealth>(true);
        health ??= GetComponentInParent<MonsterHealth>();

        if (resetTransforms == null || resetTransforms.Length == 0)
        {
            Transform navTarget = FindNavTarget(transform);
            resetTransforms = navTarget != null
                ? new[] { transform, navTarget }
                : new[] { transform };
        }
    }

    private void CaptureInitialPoses()
    {
        if (resetTransforms == null)
        {
            initialPoses = Array.Empty<TransformPose>();
            return;
        }

        initialPoses = new TransformPose[resetTransforms.Length];
        for (int i = 0; i < resetTransforms.Length; i++)
        {
            Transform target = resetTransforms[i];
            if (target == null)
            {
                continue;
            }

            initialPoses[i] = new TransformPose
            {
                transform = target,
                localPosition = target.localPosition,
                localRotation = target.localRotation,
                localScale = target.localScale
            };
        }
    }

    private void RestoreInitialPoses()
    {
        if (initialPoses == null)
        {
            return;
        }

        for (int i = 0; i < initialPoses.Length; i++)
        {
            TransformPose pose = initialPoses[i];
            if (pose.transform == null)
            {
                continue;
            }

            pose.transform.SetLocalPositionAndRotation(pose.localPosition, pose.localRotation);
            pose.transform.localScale = pose.localScale;
        }
    }

    private void ClearRigidbodyVelocity()
    {
        if (rigidbodies == null)
        {
            return;
        }

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];
            if (body == null)
            {
                continue;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void ResetRuntimeComponents()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IMonsterResettable resettable)
            {
                resettable.ResetMonsterRuntime();
            }
        }
    }

    private void OnHealthDied(MonsterHealth monsterHealth)
    {
        if (dead)
        {
            return;
        }

        dead = true;
        Died?.Invoke(this);
    }

    private static Transform FindNavTarget(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform navTarget = root.Find("NavTarget");
        if (navTarget != null)
        {
            return navTarget;
        }

        return root.Find("Nav Target");
    }
}

/// <summary>
/// 몬스터 리스폰 때 공격 쿨다운, waypoint index, 코루틴 같은 런타임 상태를 정리해야 하는 컴포넌트가 구현합니다.
/// </summary>
public interface IMonsterResettable
{
    void ResetMonsterRuntime();
}
