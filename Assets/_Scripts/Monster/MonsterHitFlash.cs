using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 몬스터가 피해를 받을 때 몸 렌더러를 아주 짧게 흰색으로 바꿔 피격 여부를 보여줍니다.
/// 공유 머티리얼 대신 런타임 머티리얼 인스턴스만 수정해서 프로젝트 에셋에는 영향을 주지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterHitFlash : MonoBehaviour, IMonsterResettable, IMonsterDeathHandler
{
    private struct MaterialColorState
    {
        public Material material;
        public bool hasBaseColor;
        public bool hasColor;
        public Color baseColor;
        public Color color;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("References")]
    [SerializeField, Tooltip("피해 이벤트를 받을 MonsterHealth입니다. 비워두면 같은 몬스터 계층에서 찾습니다.")]
    private MonsterHealth monsterHealth;

    [SerializeField, Tooltip("흰색 플래시를 적용할 몸 렌더러들입니다. 비워두면 자식 MeshRenderer/SkinnedMeshRenderer를 찾습니다.")]
    private Renderer[] renderers;

    [Header("Flash")]
    [SerializeField, Tooltip("피격 순간 바꿀 색입니다.")]
    private Color flashColor = Color.white;

    [SerializeField, Min(0.01f), Tooltip("흰색에서 원래 색으로 돌아오는 시간입니다.")]
    private float flashDuration = 0.12f;

    [SerializeField, Tooltip("꺼져 있는 자식 렌더러도 검색 대상에 포함합니다.")]
    private bool includeInactiveRenderers = true;

    [Header("Debug")]
    [SerializeField, Tooltip("렌더러나 체력 참조가 없을 때 경고를 출력합니다.")]
    private bool showDebugLog;

    private MaterialColorState[] colorStates = Array.Empty<MaterialColorState>();
    private Tween flashTween;
    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        RestoreOriginalColors();
        KillFlashTween();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        KillFlashTween();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public void ResetMonsterRuntime()
    {
        RestoreOriginalColors();
        KillFlashTween();
        ResolveReferences();
        Subscribe();
    }

    public void OnMonsterDied()
    {
        RestoreOriginalColors();
        KillFlashTween();
    }

    private void Subscribe()
    {
        if (subscribed || monsterHealth == null)
        {
            return;
        }

        monsterHealth.Damaged += OnDamaged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || monsterHealth == null)
        {
            return;
        }

        monsterHealth.Damaged -= OnDamaged;
        subscribed = false;
    }

    private void OnDamaged(MonsterHealth health, int amount)
    {
        if (health == null || health.IsDead)
        {
            return;
        }

        PlayFlash();
    }

    /// <summary>
    /// 현재 렌더러의 런타임 머티리얼 색을 저장한 뒤 흰색으로 바꾸고, 짧게 원래 색으로 보간합니다.
    /// </summary>
    private void PlayFlash()
    {
        ResolveReferences();
        if (renderers == null || renderers.Length == 0)
        {
            Warn("피격 플래시를 적용할 렌더러를 찾지 못했습니다.");
            return;
        }

        RestoreOriginalColors();
        KillFlashTween();
        CaptureCurrentColors();

        SetFlashColor(flashColor);
        flashTween = DOVirtual
            .Float(0f, 1f, flashDuration, SetFlashProgress)
            .SetEase(Ease.OutQuad)
            .SetTarget(this)
            .OnComplete(RestoreOriginalColors);
    }

    private void CaptureCurrentColors()
    {
        int materialCount = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (!IsMonsterBodyRenderer(targetRenderer))
            {
                continue;
            }

            materialCount += targetRenderer.materials.Length;
        }

        colorStates = new MaterialColorState[materialCount];
        int index = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (!IsMonsterBodyRenderer(targetRenderer))
            {
                continue;
            }

            Material[] materials = targetRenderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                {
                    continue;
                }

                bool hasBaseColor = material.HasProperty(BaseColorId);
                bool hasColor = material.HasProperty(ColorId);
                colorStates[index++] = new MaterialColorState
                {
                    material = material,
                    hasBaseColor = hasBaseColor,
                    hasColor = hasColor,
                    baseColor = hasBaseColor ? material.GetColor(BaseColorId) : Color.white,
                    color = hasColor ? material.GetColor(ColorId) : Color.white
                };
            }
        }
    }

    private void SetFlashProgress(float progress)
    {
        for (int i = 0; i < colorStates.Length; i++)
        {
            MaterialColorState state = colorStates[i];
            if (state.material == null)
            {
                continue;
            }

            if (state.hasBaseColor)
            {
                state.material.SetColor(BaseColorId, Color.Lerp(flashColor, state.baseColor, progress));
            }

            if (state.hasColor)
            {
                state.material.SetColor(ColorId, Color.Lerp(flashColor, state.color, progress));
            }
        }
    }

    private void SetFlashColor(Color color)
    {
        for (int i = 0; i < colorStates.Length; i++)
        {
            MaterialColorState state = colorStates[i];
            if (state.material == null)
            {
                continue;
            }

            if (state.hasBaseColor)
            {
                state.material.SetColor(BaseColorId, color);
            }

            if (state.hasColor)
            {
                state.material.SetColor(ColorId, color);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < colorStates.Length; i++)
        {
            MaterialColorState state = colorStates[i];
            if (state.material == null)
            {
                continue;
            }

            if (state.hasBaseColor)
            {
                state.material.SetColor(BaseColorId, state.baseColor);
            }

            if (state.hasColor)
            {
                state.material.SetColor(ColorId, state.color);
            }
        }

        colorStates = Array.Empty<MaterialColorState>();
    }

    private void KillFlashTween()
    {
        if (flashTween == null)
        {
            return;
        }

        flashTween.Kill();
        flashTween = null;
    }

    private void ResolveReferences()
    {
        monsterHealth ??= GetComponent<MonsterHealth>();
        monsterHealth ??= GetComponentInParent<MonsterHealth>();
        monsterHealth ??= GetComponentInChildren<MonsterHealth>(true);

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        }
    }

    private static bool IsMonsterBodyRenderer(Renderer targetRenderer)
    {
        return targetRenderer != null
            && (targetRenderer is MeshRenderer || targetRenderer is SkinnedMeshRenderer);
    }

    private void Warn(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning($"[{nameof(MonsterHitFlash)}] {message}", this);
        }
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0.01f, flashDuration);
    }
}
