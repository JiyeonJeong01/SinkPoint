using UnityEngine;

/// <summary>
/// NavTarget을 현재 표면 위에서 이동시키는 공통 기반 클래스입니다.
/// Spider처럼 procedural leg가 root.up을 기준으로 발을 찾는 몬스터는 navTarget의 up 방향을 표면 normal에 맞춰야 합니다.
/// </summary>
public abstract class MonsterNavTargetMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform navTarget;
    [SerializeField] protected GravityState gravityState;
    [SerializeField] protected MonsterStateMachine stateMachine;

    [Header("Movement")]
    [SerializeField, Min(0f)] protected float moveSpeed = 2f;
    [SerializeField, Min(0f)] protected float rotationSpeed = 360f;
    [SerializeField, Min(0f)] protected float stoppingDistance = 0.35f;

    protected Vector3 currentSurfaceNormal = Vector3.up;

    protected virtual void Awake()
    {
        ResolveSceneReferences();
    }

    protected virtual void Reset()
    {
        ResolveSceneReferences();
    }

    /// <summary>
    /// Inspector 참조가 비어 있으면 같은 몬스터 계층과 씬의 중력 상태에서 필요한 값을 찾습니다.
    /// 자식 이름이 특수한 NavTarget인 경우는 각 구체 Mover가 추가로 처리합니다.
    /// </summary>
    protected virtual void ResolveSceneReferences()
    {
        navTarget ??= FindNavTarget(transform);
        navTarget ??= transform;

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>();

        if (gravityState == null)
        {
            gravityState = FindGravityState();
        }
    }

    protected static GravityState FindGravityState()
    {
        GameObject gravitySystem = GameObject.Find("GravitySystem");
        if (gravitySystem != null && gravitySystem.TryGetComponent(out GravityState namedGravityState))
        {
            return namedGravityState;
        }

        return FindFirstObjectByType<GravityState>();
    }

    protected static Transform FindNavTarget(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform navTarget = root.Find("Nav Target");
        if (navTarget != null)
        {
            return navTarget;
        }

        return root.Find("NavTarget");
    }

    /// <summary>
    /// 지정된 지점을 향해 NavTarget을 한 프레임 이동시킵니다.
    /// 이동 방향은 surfaceNormal 평면 위로 투영해서 벽/천장에서도 표면을 따라 움직이게 합니다.
    /// </summary>
    protected bool MoveNavTargetToward(Vector3 destination, Vector3 surfaceNormal, float deltaTime)
    {
        if (navTarget == null)
        {
            return false;
        }

        // 새로 받은 surfaceNormal 값이 사실상 0 벡터면, 잘못된 값으로 보고 이전 표면 방향을 유지합니다.
        currentSurfaceNormal = surfaceNormal.sqrMagnitude < Mathf.Epsilon
            ? currentSurfaceNormal
            : surfaceNormal.normalized;

        Vector3 toDestination = destination - navTarget.position;
        Vector3 moveDirection = Vector3.ProjectOnPlane(toDestination, currentSurfaceNormal);

        // 목적지에 거의 도착했으면 더 이동하지 않고, 회전만 표면 normal에 맞춰 정리한 뒤 도착했다고 알려준다.
        if (moveDirection.magnitude <= stoppingDistance)
        {
            AlignNavTarget(currentSurfaceNormal, deltaTime, moveDirection);
            return true;
        }

        navTarget.position += moveDirection.normalized * moveSpeed * deltaTime;
        AlignNavTarget(currentSurfaceNormal, deltaTime, moveDirection);
        return false;
    }

    /// <summary>
    /// 표면이 바뀌는 waypoint 전환처럼 평면 투영 이동이 목적지 접근을 막을 때 사용합니다.
    /// 위치는 목적지로 직접 이동하되, up 방향은 새 표면 normal에 맞춥니다.
    /// </summary>
    protected bool MoveNavTargetDirectlyToward(Vector3 destination, Vector3 surfaceNormal, float deltaTime)
    {
        if (navTarget == null)
        {
            return false;
        }

        currentSurfaceNormal = surfaceNormal.sqrMagnitude < Mathf.Epsilon
            ? currentSurfaceNormal
            : surfaceNormal.normalized;

        Vector3 toDestination = destination - navTarget.position;
        if (toDestination.magnitude <= stoppingDistance)
        {
            AlignNavTarget(currentSurfaceNormal, deltaTime, toDestination);
            return true;
        }

        navTarget.position += toDestination.normalized * moveSpeed * deltaTime;
        AlignNavTarget(currentSurfaceNormal, deltaTime, toDestination);
        return false;
    }

    /// <summary>
    /// NavTarget의 up 방향을 현재 표면 normal로 부드럽게 맞춥니다.
    /// 기존 진행 방향이 표면 normal과 거의 평행하면 LookRotation이 불안정하므로 보조 forward를 다시 계산합니다.
    /// </summary>
    protected void AlignNavTarget(Vector3 surfaceNormal, float deltaTime)
    {
        AlignNavTarget(surfaceNormal, deltaTime, navTarget != null ? navTarget.forward : Vector3.forward);
    }

    /// <summary>
    /// NavTarget의 up은 표면 normal에 맞추고, forward는 실제 이동 방향을 바라보게 합니다.
    /// </summary>
    protected void AlignNavTarget(Vector3 surfaceNormal, float deltaTime, Vector3 desiredForward)
    {
        if (navTarget == null)
        {
            return;
        }

        Vector3 up = surfaceNormal.normalized;
        Vector3 forward = Vector3.ProjectOnPlane(desiredForward, up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(navTarget.forward, up);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, up);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.Cross(up, Vector3.right);
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, up);
        navTarget.rotation = Quaternion.RotateTowards(
            navTarget.rotation,
            targetRotation,
            rotationSpeed * deltaTime);
    }

    protected virtual void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
    }
}
