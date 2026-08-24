using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MainTitleScene : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Original_GamePlayScene";

    [Header("Panels")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject exitNoticePanel;

    private void Awake()
    {
        CloseAllPanels();
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
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError($"[{nameof(MainTitleScene)}] Game scene name is empty.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
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
}
