using UnityEngine;

/// <summary>
/// 몬스터가 몸으로 부딪혔을 때 플레이어에게 쿨타임 기반 접촉 피해를 적용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterDamageOnContact : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField] private bool showDebugLog;

    private float nextDamageTime;
    private MonsterHealth monsterHealth;

    public int Damage => damage;

    private void Awake()
    {
        monsterHealth = GetComponentInParent<MonsterHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    /// <summary>
    /// 접촉한 Collider가 플레이어인지 확인하고, 쿨타임이 끝났으면 공격 이벤트 자리에 진입합니다.
    /// </summary>
    private void TryDamage(Collider other)
    {
        if (other == null || Time.time < nextDamageTime || monsterHealth == null || monsterHealth.IsDead)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        nextDamageTime = Time.time + cooldown;
        playerHealth.ApplyDamage(damage);

        if (showDebugLog)
        {
            Debug.Log($"[MonsterDamageOnContact] Hit player for {damage} damage.", this);
        }
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        cooldown = Mathf.Max(0f, cooldown);
    }
}
