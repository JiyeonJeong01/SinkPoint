using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 몬스터 사망 시 렌더러 머티리얼을 디졸브 머티리얼로 교체하고, 연출이 끝나면 몬스터를 비활성화합니다.
/// 리스폰 때 진행 중인 디졸브를 끊고 원래 머티리얼/렌더러 상태를 복구합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterDissolveOnDeath : MonoBehaviour, IMonsterDeathHandler, IMonsterResettable
{
    [Serializable]
    private struct RendererState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public bool originalEnabled;
    }

    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseColorMapId = Shader.PropertyToID("_BaseColorMap");

    [Header("Dissolve")]
    [SerializeField, Tooltip("사망 연출에 사용할 Ultimate 10 Plus Shaders의 Dissolve 머티리얼입니다.")]
    private Material dissolveMaterialTemplate;

    [SerializeField, Tooltip("비워두면 디졸브 머티리얼에 들어있는 _NoiseTex를 그대로 사용합니다.")]
    private Texture noiseTexture;

    [SerializeField, Min(0.01f), Tooltip("_Cutoff가 0에서 1까지 올라가며 사라지는 시간입니다.")]
    private float dissolveDuration = 1.1f;

    [SerializeField, Tooltip("디졸브가 끝난 뒤 렌더러를 꺼서 완전히 보이지 않게 합니다.")]
    private bool disableRenderersOnComplete = true;

    [SerializeField, Tooltip("디졸브가 끝난 뒤 몬스터 루트 오브젝트를 비활성화합니다. 리스폰을 위해 Destroy보다 이 방식을 권장합니다.")]
    private bool deactivateMonsterOnComplete = true;

    [Header("Renderer Search")]
    [SerializeField, Tooltip("비워두면 자식 MeshRenderer/SkinnedMeshRenderer를 자동으로 찾습니다.")]
    private Renderer[] renderers;

    [SerializeField, Tooltip("꺼져 있는 자식 렌더러도 원본 상태로 기록할지 정합니다.")]
    private bool includeInactiveRenderers = true;

    [Header("Debug")]
    [SerializeField, Tooltip("머티리얼 누락이나 렌더러 누락 같은 설정 문제를 Console에 표시합니다.")]
    private bool showDebugLog = true;

    private RendererState[] rendererStates = Array.Empty<RendererState>();
    private Material[] runtimeDissolveMaterials = Array.Empty<Material>();
    private Monster monsterRoot;
    private Tween dissolveTween;
    private bool initialized;
    private bool dissolving;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        KillDissolveTween();
        DestroyRuntimeMaterials();
    }

    /// <summary>
    /// Monster 공통 사망 훅에서 호출됩니다.
    /// 원본 머티리얼을 보존한 뒤 디졸브 머티리얼 인스턴스로만 값을 변경합니다.
    /// </summary>
    public void OnMonsterDied()
    {
        EnsureInitialized();

        if (dissolving)
        {
            return;
        }

        if (dissolveMaterialTemplate == null)
        {
            Warn("Dissolve Material Template이 비어 있어서 디졸브 없이 사망 비활성화만 진행합니다.");
            CompleteDeathVisual();
            return;
        }

        if (rendererStates.Length == 0)
        {
            Warn("디졸브할 MeshRenderer/SkinnedMeshRenderer를 찾지 못했습니다.");
            CompleteDeathVisual();
            return;
        }

        dissolving = true;
        ApplyDissolveMaterials();

        dissolveTween = DOVirtual
            .Float(0f, 1f, dissolveDuration, SetCutoff)
            .SetEase(Ease.InOutSine)
            .SetTarget(this)
            .OnComplete(CompleteDeathVisual);
    }

    /// <summary>
    /// 플레이어 리스폰/Zone 재시작 시 호출됩니다.
    /// 사망 연출 중이어도 즉시 끊고 원본 렌더링 상태로 되돌립니다.
    /// </summary>
    public void ResetMonsterRuntime()
    {
        EnsureInitialized();
        KillDissolveTween();
        dissolving = false;
        RestoreOriginalRendererState();
        DestroyRuntimeMaterials();

        GameObject root = ResolveMonsterRootObject();
        if (root != null && !root.activeSelf)
        {
            root.SetActive(true);
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        ResolveReferences();
        CaptureOriginalRendererState();
        initialized = true;
    }

    private void ResolveReferences()
    {
        monsterRoot = GetComponent<Monster>();
        monsterRoot ??= GetComponentInParent<Monster>();

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        }

#if UNITY_EDITOR
        if (dissolveMaterialTemplate == null)
        {
            dissolveMaterialTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Ultimate 10 Plus Shaders/Materials/Unique/Dissolve.mat");
        }
#endif
    }

    private void CaptureOriginalRendererState()
    {
        if (renderers == null)
        {
            rendererStates = Array.Empty<RendererState>();
            return;
        }

        int validCount = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsMonsterBodyRenderer(renderers[i]))
            {
                validCount++;
            }
        }

        rendererStates = new RendererState[validCount];
        int index = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (!IsMonsterBodyRenderer(targetRenderer))
            {
                continue;
            }

            rendererStates[index++] = new RendererState
            {
                renderer = targetRenderer,
                originalMaterials = targetRenderer.sharedMaterials,
                originalEnabled = targetRenderer.enabled
            };
        }
    }

    private void ApplyDissolveMaterials()
    {
        DestroyRuntimeMaterials();

        int materialCount = 0;
        for (int i = 0; i < rendererStates.Length; i++)
        {
            materialCount += rendererStates[i].originalMaterials?.Length ?? 0;
        }

        runtimeDissolveMaterials = new Material[materialCount];
        int createdIndex = 0;

        for (int i = 0; i < rendererStates.Length; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer == null)
            {
                continue;
            }

            Material[] originals = state.originalMaterials;
            if (originals == null || originals.Length == 0)
            {
                continue;
            }

            Material[] dissolveMaterials = new Material[originals.Length];
            for (int j = 0; j < originals.Length; j++)
            {
                Material dissolveMaterial = BuildDissolveMaterial(originals[j]);
                dissolveMaterials[j] = dissolveMaterial;
                runtimeDissolveMaterials[createdIndex++] = dissolveMaterial;
            }

            state.renderer.enabled = true;
            state.renderer.sharedMaterials = dissolveMaterials;
        }

        SetCutoff(0f);
    }

    private Material BuildDissolveMaterial(Material originalMaterial)
    {
        Material dissolveMaterial = new Material(dissolveMaterialTemplate);
        Texture mainTexture = GetMainTexture(originalMaterial);
        Texture selectedNoiseTexture = noiseTexture != null
            ? noiseTexture
            : GetTextureIfExists(dissolveMaterialTemplate, NoiseTexId);

        if (mainTexture != null && dissolveMaterial.HasProperty(MainTexId))
        {
            dissolveMaterial.SetTexture(MainTexId, mainTexture);
        }

        if (selectedNoiseTexture != null && dissolveMaterial.HasProperty(NoiseTexId))
        {
            dissolveMaterial.SetTexture(NoiseTexId, selectedNoiseTexture);
        }

        if (originalMaterial != null && dissolveMaterial.HasProperty(ColorId))
        {
            if (originalMaterial.HasProperty(BaseColorId))
            {
                dissolveMaterial.SetColor(ColorId, originalMaterial.GetColor(BaseColorId));
            }
            else if (originalMaterial.HasProperty(ColorId))
            {
                dissolveMaterial.SetColor(ColorId, originalMaterial.GetColor(ColorId));
            }
        }

        if (dissolveMaterial.HasProperty(CutoffId))
        {
            dissolveMaterial.SetFloat(CutoffId, 0f);
        }

        return dissolveMaterial;
    }

    private void SetCutoff(float value)
    {
        for (int i = 0; i < runtimeDissolveMaterials.Length; i++)
        {
            Material material = runtimeDissolveMaterials[i];
            if (material != null && material.HasProperty(CutoffId))
            {
                material.SetFloat(CutoffId, value);
            }
        }
    }

    private void CompleteDeathVisual()
    {
        dissolving = false;
        dissolveTween = null;

        if (disableRenderersOnComplete)
        {
            SetRenderersEnabled(false);
        }

        if (deactivateMonsterOnComplete)
        {
            GameObject root = ResolveMonsterRootObject();
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    private void RestoreOriginalRendererState()
    {
        for (int i = 0; i < rendererStates.Length; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer == null)
            {
                continue;
            }

            state.renderer.sharedMaterials = state.originalMaterials;
            state.renderer.enabled = state.originalEnabled;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < rendererStates.Length; i++)
        {
            if (rendererStates[i].renderer != null)
            {
                rendererStates[i].renderer.enabled = enabled;
            }
        }
    }

    private void KillDissolveTween()
    {
        if (dissolveTween == null)
        {
            return;
        }

        dissolveTween.Kill();
        dissolveTween = null;
    }

    private void DestroyRuntimeMaterials()
    {
        if (runtimeDissolveMaterials == null)
        {
            runtimeDissolveMaterials = Array.Empty<Material>();
            return;
        }

        for (int i = 0; i < runtimeDissolveMaterials.Length; i++)
        {
            Material material = runtimeDissolveMaterials[i];
            if (material == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }

        runtimeDissolveMaterials = Array.Empty<Material>();
    }

    private GameObject ResolveMonsterRootObject()
    {
        if (monsterRoot != null)
        {
            return monsterRoot.gameObject;
        }

        Monster parentMonster = GetComponentInParent<Monster>();
        return parentMonster != null ? parentMonster.gameObject : gameObject;
    }

    private static bool IsMonsterBodyRenderer(Renderer targetRenderer)
    {
        return targetRenderer is MeshRenderer || targetRenderer is SkinnedMeshRenderer;
    }

    private static Texture GetMainTexture(Material material)
    {
        Texture texture = GetTextureIfExists(material, MainTexId);
        texture ??= GetTextureIfExists(material, BaseMapId);
        texture ??= GetTextureIfExists(material, BaseColorMapId);
        return texture;
    }

    private static Texture GetTextureIfExists(Material material, int propertyId)
    {
        return material != null && material.HasProperty(propertyId)
            ? material.GetTexture(propertyId)
            : null;
    }

    private void Warn(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning($"[MonsterDissolveOnDeath] {message}", this);
        }
    }
}
