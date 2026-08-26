using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplayPauseCanvas : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainTitleSceneName = "MainTitleScene";

    [Header("References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button mainTitleButton;
    [SerializeField] private Button exitButton;
    [SerializeField, Tooltip("비워두면 씬에서 자동으로 찾습니다.")]
    private PlayerInput playerInput;

    [Header("Button Feedback")]
    [SerializeField, Tooltip("일시 정지 UI 버튼 클릭 시 재생할 사운드입니다.")]
    private AudioClip buttonClickSound;
    [SerializeField, Tooltip("비워두면 같은 오브젝트의 AudioSource를 자동으로 사용합니다.")]
    private AudioSource buttonAudioSource;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.8f;
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0.01f)] private float pressedScale = 0.94f;
    [SerializeField, Min(0f)] private float buttonTweenDuration = 0.12f;
    [SerializeField] private Ease buttonEase = Ease.OutCubic;

    private readonly Dictionary<RectTransform, Vector3> buttonBaseScales = new Dictionary<RectTransform, Vector3>();
    private bool isOpen;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        transform.localScale = Vector3.one;
        ResolveReferences();
        ResolveAudioReferences();
        RegisterButtonTweens();
        SetMenuOpen(false, false);
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

    private void OnEnable()
    {
        if (mainTitleButton != null)
        {
            mainTitleButton.onClick.AddListener(ReturnToMainTitle);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void OnDisable()
    {
        if (mainTitleButton != null)
        {
            mainTitleButton.onClick.RemoveListener(ReturnToMainTitle);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }

        if (isOpen)
        {
            SetMenuOpen(false, true);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuOpen(!isOpen, true);
        }
    }

    public void ReturnToMainTitle()
    {
        SetMenuOpen(false, false);
        Time.timeScale = 1f;

        if (playerInput != null)
        {
            playerInput.SetGameplayInput();
        }

        if (string.IsNullOrWhiteSpace(mainTitleSceneName))
        {
            Debug.LogError($"[{nameof(GameplayPauseCanvas)}] Main title scene name is empty.", this);
            return;
        }

        SceneManager.LoadScene(mainTitleSceneName);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL is sandboxed by the browser and cannot reliably close its own tab.
        ReturnToMainTitle();
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetMenuOpen(bool open, bool applyInputLock)
    {
        if (open == isOpen)
        {
            SetMenuVisible(open);
            return;
        }

        isOpen = open;

        if (open)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (applyInputLock)
        {
            playerInput ??= FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                if (open)
                {
                    playerInput.SetCutsceneInput();
                }
                else
                {
                    playerInput.SetGameplayInput();
                }
            }
        }

        SetMenuVisible(open);

        if (open && mainTitleButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(mainTitleButton.gameObject);
        }
        else if (!open)
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }
    }

    private void ResolveReferences()
    {
        playerInput ??= FindFirstObjectByType<PlayerInput>();

        if (mainTitleButton == null || exitButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name == "Main Title Button")
                {
                    mainTitleButton ??= button;
                }
                else if (button.name == "Exit Button")
                {
                    exitButton ??= button;
                }
            }
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

#if UNITY_EDITOR
    public void EditorConfigure(GameObject root, Button mainButton, Button quitButton)
    {
        menuRoot = root;
        mainTitleButton = mainButton;
        exitButton = quitButton;
    }
#endif
}
