using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬에 배치한 AudioSource 하나로 BGM을 반복 재생합니다.
/// 타이틀/게임플레이 씬에 빈 오브젝트를 만들고 이 컴포넌트만 붙이면 됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneBgmPlayer : MonoBehaviour
{
    private enum BgmProfile
    {
        Custom,
        MainTitle,
        Gameplay
    }

    [SerializeField, Tooltip("Reset/OnValidate 때 기본 BGM 클립을 자동으로 채우는 프리셋입니다.")]
    private BgmProfile profile = BgmProfile.Custom;
    [SerializeField, Tooltip("씬 시작 시 반복 재생할 BGM입니다.")]
    private AudioClip bgmClip;
    [SerializeField, Tooltip("비워두면 같은 오브젝트의 AudioSource를 자동으로 사용합니다.")]
    private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)]
    private float volume = 0.55f;
    [SerializeField, Tooltip("씬 시작 시 자동으로 재생합니다.")]
    private bool playOnStart = true;

    private void Awake()
    {
        InferProfileIfNeeded();
        ApplyProfileDefaults();
        ResolveReferences();
        ConfigureSource();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Reset()
    {
        ResolveReferences();
        InferProfileIfNeeded();
        ApplyProfileDefaults();
    }

    public void Play()
    {
        if (audioSource == null || bgmClip == null)
        {
            Debug.LogWarning($"[{nameof(SceneBgmPlayer)}] {name} requires AudioSource and BGM Clip.", this);
            return;
        }

        audioSource.clip = bgmClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void ResolveReferences()
    {
        audioSource ??= GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        InferProfileIfNeeded();
        ApplyProfileDefaults();
    }

    private void InferProfileIfNeeded()
    {
        if (profile != BgmProfile.Custom)
        {
            return;
        }

        string key = $"{gameObject.scene.name} {name}".ToLowerInvariant();
        if (key.Contains("title") || key.Contains("main"))
        {
            profile = BgmProfile.MainTitle;
        }
        else if (key.Contains("gameplay") || key.Contains("play"))
        {
            profile = BgmProfile.Gameplay;
        }
    }

    private void ApplyProfileDefaults()
    {
#if UNITY_EDITOR
        switch (profile)
        {
            case BgmProfile.MainTitle:
                bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Caves and Dungeons/Call of the Depths/Call_of_the_Depths_Loop_A.wav");
                break;
            case BgmProfile.Gameplay:
                bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Caves and Dungeons/An Unwelcome Presence/An_Unwelcome_Presence_Loop_A.wav");
                break;
        }
#endif
    }
}
