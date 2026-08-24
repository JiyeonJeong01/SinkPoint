using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InGameHudCanvas : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text monsterCountText;

    private PlayerHealth boundPlayerHealth;

    private void Awake()
    {
        ResolveReferences();
        RefreshHp();
    }

    private void OnDestroy()
    {
        UnbindPlayerHealth();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public void BindPlayerHealth(PlayerHealth playerHealth)
    {
        if (boundPlayerHealth == playerHealth)
        {
            RefreshHp();
            return;
        }

        UnbindPlayerHealth();
        boundPlayerHealth = playerHealth;

        if (boundPlayerHealth != null)
        {
            boundPlayerHealth.Damaged += OnPlayerDamaged;
            boundPlayerHealth.Died += OnPlayerDied;
        }

        RefreshHp();
    }

    public void RefreshHp()
    {
        if (boundPlayerHealth != null)
        {
            SetHp(boundPlayerHealth.CurrentHealth, boundPlayerHealth.MaxHealth);
        }
    }

    public void SetHp(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, maxHp);
            hpSlider.value = Mathf.Clamp(currentHp, 0, hpSlider.maxValue);
        }
    }

    public void SetRemainingMonsterCount(int count)
    {
        if (monsterCountText != null)
        {
            monsterCountText.text = $"MONSTERS {Mathf.Max(0, count)}";
        }
    }

    private void ResolveReferences()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponentInChildren<Slider>(true);
        }
    }

    private void UnbindPlayerHealth()
    {
        if (boundPlayerHealth == null)
        {
            return;
        }

        boundPlayerHealth.Damaged -= OnPlayerDamaged;
        boundPlayerHealth.Died -= OnPlayerDied;
        boundPlayerHealth = null;
    }

    private void OnPlayerDamaged(PlayerHealth playerHealth, int amount)
    {
        RefreshHp();
    }

    private void OnPlayerDied(PlayerHealth playerHealth)
    {
        RefreshHp();
    }

#if UNITY_EDITOR
    public void EditorConfigure(Text hpLabel, Text monsterCountLabel)
    {
        monsterCountText = monsterCountLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
    }

    public void EditorConfigure(Slider slider, Text monsterCountLabel)
    {
        hpSlider = slider;
        monsterCountText = monsterCountLabel;
        SetHp(3, 3);
        SetRemainingMonsterCount(0);
    }
#endif
}
