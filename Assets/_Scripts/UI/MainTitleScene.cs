using System.Collections;
using UnityEngine;
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

    private bool isLoading;

    private void Awake()
    {
        CloseAllPanels();
        SetLoadingVisible(false);
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
}
