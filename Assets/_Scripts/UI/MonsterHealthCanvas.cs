using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MonsterHealthCanvas : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private bool faceMainCamera = true;

    private Camera mainCamera;
    private MonsterHealth monsterHealth;

    private void Awake()
    {
        monsterHealth = GetComponentInParent<MonsterHealth>();
        mainCamera = Camera.main;
        Refresh();

        if (monsterHealth != null)
        {
            monsterHealth.Damaged += OnMonsterDamaged;
            monsterHealth.Died += OnMonsterDied;
        }
    }

    private void LateUpdate()
    {
        if (!faceMainCamera)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    private void OnDestroy()
    {
        if (monsterHealth != null)
        {
            monsterHealth.Damaged -= OnMonsterDamaged;
            monsterHealth.Died -= OnMonsterDied;
        }
    }

    public void SetHealth(int currentHp, int maxHp)
    {
        if (hpSlider == null)
        {
            return;
        }

        hpSlider.maxValue = Mathf.Max(1, maxHp);
        hpSlider.value = Mathf.Clamp(currentHp, 0, hpSlider.maxValue);
    }

    private void Refresh()
    {
        if (monsterHealth != null)
        {
            SetHealth(monsterHealth.CurrentHealth, monsterHealth.MaxHealth);
        }
        else
        {
            SetHealth(1, 1);
        }
    }

    private void OnMonsterDamaged(MonsterHealth health, int amount)
    {
        Refresh();
    }

    private void OnMonsterDied(MonsterHealth health)
    {
        Refresh();
    }

#if UNITY_EDITOR
    public void EditorConfigure(Slider slider)
    {
        hpSlider = slider;
        SetHealth(1, 1);
    }
#endif
}
