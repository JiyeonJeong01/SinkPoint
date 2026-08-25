using UnityEngine;

/// <summary>
/// 몬스터가 발사하는 단순 투사체입니다.
/// VFX가 붙은 prefab에 함께 붙이거나, 공격 스크립트가 생성 직후 자동으로 붙여 사용할 수 있습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f), Tooltip("투사체 이동 속도입니다.")]
    private float speed = 12f;
    [SerializeField, Min(0f), Tooltip("이 시간이 지나면 자동으로 사라집니다.")]
    private float lifetime = 4f;
    [SerializeField, Min(0f), Tooltip("빠른 투사체가 벽을 관통하지 않도록 이동 경로를 이 반지름으로 검사합니다.")]
    private float collisionRadius = 0.15f;

    [Header("Damage")]
    [SerializeField, Min(0), Tooltip("플레이어에게 줄 피해량입니다.")]
    private int damage = 1;
    [SerializeField, Tooltip("충돌 판정에 사용할 레이어입니다. 비워두면 전체를 검사합니다.")]
    private LayerMask collisionMask = ~0;
    [SerializeField, Tooltip("이 레이어들은 부딪혀도 무시합니다. 비워두면 MonsterAttack, Monster를 자동 사용합니다.")]
    private LayerMask ignoredLayers;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("마지막 충돌로 사라졌는지 확인합니다.")]
    private bool destroyedByHit;

    private Transform ownerRoot;
    private Vector3 direction = Vector3.forward;
    private float despawnTime;
    private bool initialized;

    /// <summary>
    /// 발사 직후 공격자가 이동 방향, 피해량, 무시할 소유자 계층을 지정합니다.
    /// </summary>
    public void Initialize(Transform owner, Vector3 fireDirection, int projectileDamage, float projectileSpeed, float projectileLifetime)
    {
        ownerRoot = owner;
        direction = fireDirection.sqrMagnitude < 0.0001f ? transform.forward : fireDirection.normalized;
        damage = Mathf.Max(0, projectileDamage);
        speed = Mathf.Max(0f, projectileSpeed);
        lifetime = Mathf.Max(0f, projectileLifetime);
        despawnTime = Time.time + lifetime;
        initialized = true;
        ResolveDefaultLayers();

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    private void Awake()
    {
        ResolveDefaultLayers();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            direction = transform.forward;
            despawnTime = Time.time + lifetime;
        }
    }

    private void Update()
    {
        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
            return;
        }

        MoveAndCheckCollision(Time.deltaTime);
    }

    /// <summary>
    /// 이동 전 SphereCast를 먼저 날려 충돌한 대상이 있으면 데미지/소멸 처리를 합니다.
    /// </summary>
    private void MoveAndCheckCollision(float deltaTime)
    {
        float distance = speed * deltaTime;
        if (distance <= 0f)
        {
            return;
        }

        if (Physics.SphereCast(
            transform.position,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Collide))
        {
            if (ShouldIgnore(hit.collider))
            {
                transform.position += direction * distance;
                return;
            }

            TryApplyDamage(hit.collider);
            destroyedByHit = true;
            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldIgnore(other))
        {
            return;
        }

        TryApplyDamage(other);
        destroyedByHit = true;
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider hitCollider = collision.collider;
        if (ShouldIgnore(hitCollider))
        {
            return;
        }

        TryApplyDamage(hitCollider);
        destroyedByHit = true;
        Destroy(gameObject);
    }

    private bool ShouldIgnore(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return true;
        }

        if (ownerRoot != null && hitCollider.transform.IsChildOf(ownerRoot))
        {
            return true;
        }

        int layerMask = 1 << hitCollider.gameObject.layer;
        return (ignoredLayers.value & layerMask) != 0;
    }

    private void TryApplyDamage(Collider hitCollider)
    {
        PlayerHealth playerHealth = hitCollider != null ? hitCollider.GetComponentInParent<PlayerHealth>() : null;
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        playerHealth.ApplyDamage(damage);
    }

    private void ResolveDefaultLayers()
    {
        if (ignoredLayers.value == 0)
        {
            ignoredLayers = LayerMask.GetMask("MonsterAttack", "Monster");
        }
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        lifetime = Mathf.Max(0f, lifetime);
        collisionRadius = Mathf.Max(0f, collisionRadius);
        damage = Mathf.Max(0, damage);
    }
}
