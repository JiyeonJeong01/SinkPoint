using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainTitleScene : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Original_GamePlayScene";

    [Header("Panels")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject exitNoticePanel;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingProgressSlider;
    [SerializeField] private Text loadingPercentText;
    [SerializeField, Min(0f)] private float minimumLoadingPanelTime = 0.2f;

    [Header("Button Feedback")]
    [SerializeField, Tooltip("타이틀 UI 버튼 클릭 시 재생할 사운드입니다.")]
    private AudioClip buttonClickSound;
    [SerializeField, Tooltip("비워두면 같은 오브젝트의 AudioSource를 자동으로 사용합니다.")]
    private AudioSource buttonAudioSource;
    [SerializeField, Range(0f, 1f)]
    private float buttonClickVolume = 0.8f;
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0.01f)] private float pressedScale = 0.94f;
    [SerializeField, Min(0f)] private float buttonTweenDuration = 0.12f;
    [SerializeField] private Ease buttonEase = Ease.OutCubic;

    private readonly Dictionary<RectTransform, Vector3> buttonBaseScales = new Dictionary<RectTransform, Vector3>();
    private bool isLoading;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResolveAudioReferences();
        RegisterButtonTweens();
        CloseAllPanels();
        SetLoadingVisible(false);
    }

    private void OnDestroy()
    {
        foreach (RectTransform buttonTransform in buttonBaseScales.Keys)
        {
            if (buttonTransform != null)
            {
                buttonTransform.DOKill();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
        }
    }

    public void StartGame()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError($"[{nameof(MainTitleScene)}] Game scene name is empty.", this);
            return;
        }

        if (loadingPanel == null)
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        StartCoroutine(LoadGameSceneAsync());
    }

    public void ShowHelp()
    {
        ShowOnly(helpPanel);
    }

    public void ShowCredits()
    {
        ShowOnly(creditsPanel);
    }

    public void ExitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowOnly(exitNoticePanel);
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CloseAllPanels()
    {
        if (isLoading)
        {
            return;
        }

        SetPanelActive(helpPanel, false);
        SetPanelActive(creditsPanel, false);
        SetPanelActive(exitNoticePanel, false);
    }

    private void ShowOnly(GameObject panel)
    {
        CloseAllPanels();
        SetPanelActive(panel, true);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private IEnumerator LoadGameSceneAsync()
    {
        CloseAllPanels();
        isLoading = true;
        SetLoadingVisible(true);
        SetLoadingProgress(0f);

        float loadingStartedAt = Time.unscaledTime;
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        if (operation == null)
        {
            Debug.LogError($"[{nameof(MainTitleScene)}] Failed to load scene: {gameSceneName}", this);
            isLoading = false;
            SetLoadingVisible(false);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            SetLoadingProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }

        SetLoadingProgress(1f);

        float elapsed = Time.unscaledTime - loadingStartedAt;
        if (elapsed < minimumLoadingPanelTime)
        {
            yield return new WaitForSecondsRealtime(minimumLoadingPanelTime - elapsed);
        }

        operation.allowSceneActivation = true;
    }

    private void SetLoadingVisible(bool visible)
    {
        SetPanelActive(loadingPanel, visible);
    }

    private void SetLoadingProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (loadingProgressSlider != null)
        {
            loadingProgressSlider.value = progress;
        }

        if (loadingPercentText != null)
        {
            loadingPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }

    private void RegisterButtonTweens()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            RectTransform buttonTransform = button.GetComponent<RectTransform>();
            if (buttonTransform == null || buttonBaseScales.ContainsKey(buttonTransform))
            {
                continue;
            }

            buttonBaseScales.Add(buttonTransform, buttonTransform.localScale);

            EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, _ => TweenButton(button, buttonTransform, hoverScale));
            AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, _ => TweenButton(button, buttonTransform, 1f));
            AddEventTrigger(eventTrigger, EventTriggerType.PointerDown, _ => TweenButton(button, buttonTransform, pressedScale));
            AddEventTrigger(eventTrigger, EventTriggerType.PointerUp, _ => TweenButton(button, buttonTransform, hoverScale));
            button.onClick.AddListener(PlayButtonClickSound);
        }
    }

    private void TweenButton(Button button, RectTransform buttonTransform, float scaleMultiplier)
    {
        if (button == null || buttonTransform == null || !button.interactable)
        {
            return;
        }

        if (!buttonBaseScales.TryGetValue(buttonTransform, out Vector3 baseScale))
        {
            baseScale = buttonTransform.localScale;
            buttonBaseScales[buttonTransform] = baseScale;
        }

        buttonTransform.DOKill();
        buttonTransform
            .DOScale(baseScale * scaleMultiplier, buttonTweenDuration)
            .SetEase(buttonEase)
            .SetUpdate(true);
    }

    private static void AddEventTrigger(
        EventTrigger eventTrigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(callback);
        eventTrigger.triggers.Add(entry);
    }

    private void ResolveAudioReferences()
    {
        buttonAudioSource ??= GetComponent<AudioSource>();
        if (buttonAudioSource == null)
        {
            buttonAudioSource = gameObject.AddComponent<AudioSource>();
        }

        buttonAudioSource.playOnAwake = false;
        buttonAudioSource.spatialBlend = 0f;
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && buttonAudioSource != null)
        {
            buttonAudioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (buttonClickSound == null)
        {
            buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audios/Menu/Menu_Buttons_1.wav");
        }
#endif
    }
}
