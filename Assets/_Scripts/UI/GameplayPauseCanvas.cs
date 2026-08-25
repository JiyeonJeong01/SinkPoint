using UnityEngine;
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

    private bool isOpen;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        ResolveReferences();
        SetMenuOpen(false, false);
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
        Time.timeScale = previousTimeScale;

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
        Time.timeScale = previousTimeScale;

#if UNITY_EDITOR
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
        }
        else
        {
            Time.timeScale = previousTimeScale;
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
