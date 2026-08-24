using System;
using UnityEngine;

/// <summary>
/// 모든 몬스터가 공유하는 간단한 체력 컴포넌트입니다.
/// 사망 처리의 소유권만 담당하고, 드랍/연출/스폰 정리는 외부 시스템이 이벤트로 붙도록 둡니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField] private bool destroyOnDeath;

    [Header("Runtime State")]
    [SerializeField, Tooltip("Play Mode에서 확인하는 현재 체력입니다.")]
    private int currentHealth;
    [SerializeField, Tooltip("Play Mode에서 확인하는 사망 상태입니다.")]
    private bool dead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => dead;

    public event Action<MonsterHealth> Died;
    public event Action<MonsterHealth, int> Damaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 몬스터에게 피해를 적용합니다. 이미 죽은 몬스터나 0 이하 피해는 무시합니다.
    /// </summary>
    public void ApplyDamage(int amount)
    {
        if (dead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Damaged?.Invoke(this, amount);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 리스폰이나 풀링에서 재사용할 때 체력을 초기값으로 되돌립니다.
    /// </summary>
    public void ResetHealth()
    {
        dead = false;
        currentHealth = maxHealth;
    }

    private void Die()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
