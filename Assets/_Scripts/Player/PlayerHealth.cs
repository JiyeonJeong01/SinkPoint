using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;

    [Header("Runtime State")]
    [SerializeField, Tooltip("Play Mode에서 확인하는 현재 체력입니다.")]
    private int currentHealth;
    [SerializeField, Tooltip("Play Mode에서 확인하는 사망 상태입니다.")]
    private bool dead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => dead;

    public event Action<PlayerHealth> Died;
    public event Action<PlayerHealth, int> Damaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

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
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
