using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public sealed class MonsterHealthCanvas : MonoBehaviour, IMonsterResettable
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private bool faceMainCamera = true;
    [SerializeField, Min(0f), Tooltip("피격 후 HP 슬라이더가 깎이는 연출 시간입니다.")]
    private float hpTweenDuration = 0.18f;
    [SerializeField, Tooltip("죽었을 때 빈 HP 바를 바로 숨길지 정합니다.")]
    private bool hideOnDeath = true;

    private Camera mainCamera;
    private MonsterHealth monsterHealth;
    private CanvasGroup canvasGroup;
    private Tween hpTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        monsterHealth = GetComponentInParent<MonsterHealth>();
        mainCamera = Camera.main;
        RefreshImmediate();
        SetVisible(false);

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
        KillHpTween();

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

    /// <summary>
    /// 몬스터 리스폰 시 HP를 즉시 최대치로 되돌리고, 다시 맞기 전까지 HP 바를 숨깁니다.
    /// </summary>
    public void ResetMonsterRuntime()
    {
        RefreshImmediate();
        SetVisible(false);
    }

    private void RefreshImmediate()
    {
        KillHpTween();

        if (monsterHealth != null)
        {
            SetHealth(monsterHealth.CurrentHealth, monsterHealth.MaxHealth);
        }
        else
        {
            SetHealth(1, 1);
        }
    }

    private Tween TweenHealthTo(int currentHp, int maxHp)
    {
        if (hpSlider == null)
        {
            return null;
        }

        hpSlider.maxValue = Mathf.Max(1, maxHp);
        float targetValue = Mathf.Clamp(currentHp, 0, hpSlider.maxValue);

        KillHpTween();
        hpTween = hpSlider
            .DOValue(targetValue, hpTweenDuration)
            .SetEase(Ease.OutCubic)
            .SetTarget(this);

        return hpTween;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnMonsterDamaged(MonsterHealth health, int amount)
    {
        SetVisible(true);
        TweenHealthTo(health.CurrentHealth, health.MaxHealth);
    }

    private void OnMonsterDied(MonsterHealth health)
    {
        SetVisible(true);
        Tween tween = TweenHealthTo(health.CurrentHealth, health.MaxHealth);
        if (hideOnDeath && tween != null)
        {
            tween.OnComplete(() => SetVisible(false));
        }
    }

    private void KillHpTween()
    {
        if (hpTween == null)
        {
            return;
        }

        hpTween.Kill();
        hpTween = null;
    }

#if UNITY_EDITOR
    public void EditorConfigure(Slider slider)
    {
        hpSlider = slider;
        SetHealth(1, 1);
    }
#endif
}
