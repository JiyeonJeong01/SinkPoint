using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SinkPointMainTitleSceneBuilder
{
    private const string ScenePath = "Assets/_Scenes/MainTitleScene.unity";
    private const string GameplayScenePath = "Assets/_Scenes/Original_GamePlayScene.unity";

    [MenuItem("SinkPoint/Generate Main Title Scene")]
    public static void Generate()
    {
        EnsureScenesFolder();

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "MainTitleScene";
        SceneManager.SetActiveScene(scene);

        CreateSceneContent();

        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();

        if (previousActiveScene.IsValid())
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SinkPointMainTitleSceneBuilder] Generated {ScenePath}.");
    }

    private static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "_Scenes");
        }
    }

    private static void CreateSceneContent()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.018f, 0.022f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.GetComponent<Light>().type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();

        GameObject canvasObject = new GameObject("Main Title Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainTitleScene));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        CreateBackground(canvasRect);

        Text title = CreateText("SINKPOINT", canvasRect, 84, TextAnchor.MiddleCenter, FontStyle.Bold);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -150f);
        titleRect.sizeDelta = new Vector2(900f, 120f);

        Text subtitle = CreateText("GRAVITY ANOMALY RESPONSE", canvasRect, 24, TextAnchor.MiddleCenter, FontStyle.Normal);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -255f);
        subtitleRect.sizeDelta = new Vector2(720f, 42f);

        MainTitleScene controller = canvasObject.GetComponent<MainTitleScene>();

        Button startButton = CreateButton("GameStart Button", canvasRect, "GAME START");
        PlaceMenuButton(startButton.GetComponent<RectTransform>(), 0f);
        AddButtonListener(startButton, controller.StartGame);

        Button creditsButton = CreateButton("Credits Button", canvasRect, "CREDITS");
        PlaceMenuButton(creditsButton.GetComponent<RectTransform>(), -88f);
        AddButtonListener(creditsButton, controller.ShowCredits);

        Button exitButton = CreateButton("Exit Button", canvasRect, "EXIT");
        PlaceMenuButton(exitButton.GetComponent<RectTransform>(), -176f);
        AddButtonListener(exitButton, controller.ExitGame);

        Button helpButton = CreateIconButton("Help Button", canvasRect, "?");
        RectTransform helpRect = helpButton.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(1f, 1f);
        helpRect.anchorMax = new Vector2(1f, 1f);
        helpRect.pivot = new Vector2(1f, 1f);
        helpRect.anchoredPosition = new Vector2(-36f, -32f);
        helpRect.sizeDelta = new Vector2(58f, 58f);
        AddButtonListener(helpButton, controller.ShowHelp);

        GameObject helpPanel = CreateModal(canvasRect, "Help Panel", "HELP", "WASD 이동\nMouse 시점 전환\nLeft Click 사격\nRight Click 그래플\nI 상호작용\nSpace 대화 넘기기");
        GameObject creditsPanel = CreateModal(canvasRect, "Credits Panel", "CREDITS", "SinkPoint MVP\nDesign / Programming / Assembly\nOpenAI Codex collaboration");
        GameObject exitPanel = CreateModal(canvasRect, "Exit Notice Panel", "EXIT", "Web build에서는 브라우저가 직접 종료되지 않습니다.\n플레이를 끝내려면 브라우저 탭을 닫아주세요.");

        controller.GetType().GetField("helpPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(controller, helpPanel);
        controller.GetType().GetField("creditsPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(controller, creditsPanel);
        controller.GetType().GetField("exitNoticePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(controller, exitPanel);

        helpPanel.SetActive(false);
        creditsPanel.SetActive(false);
        exitPanel.SetActive(false);
    }

    private static void CreateBackground(RectTransform parent)
    {
        GameObject background = CreateUiObject("Background", parent, typeof(Image));
        RectTransform rect = background.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.zero);

        Image image = background.GetComponent<Image>();
        image.color = new Color(0.02f, 0.024f, 0.03f, 1f);
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.13f, 0.14f, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText(label, buttonObject.transform, 28, TextAnchor.MiddleCenter, FontStyle.Bold);
        text.color = new Color(0.95f, 0.97f, 1f, 1f);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Button CreateIconButton(string name, Transform parent, string label)
    {
        Button button = CreateButton(name, parent, label);
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.fontSize = 34;
        }

        return button;
    }

    private static void PlaceMenuButton(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(360f, 64f);
    }

    private static GameObject CreateModal(RectTransform parent, string name, string title, string body)
    {
        GameObject root = CreateUiObject(name, parent, typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, Vector2.zero, Vector2.zero);

        Image blocker = root.GetComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.62f);
        blocker.raycastTarget = true;

        GameObject panel = CreateUiObject("Panel", root.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(720f, 430f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.085f, 0.095f, 0.98f);

        Text titleText = CreateText(title, panel.transform, 36, TextAnchor.UpperLeft, FontStyle.Bold);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, -92f);
        titleRect.offsetMax = new Vector2(-100f, -30f);

        Text bodyText = CreateText(body, panel.transform, 25, TextAnchor.UpperLeft, FontStyle.Normal);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(44f, 86f);
        bodyRect.offsetMax = new Vector2(-44f, -116f);

        Button closeButton = CreateIconButton("Close Button", panel.transform, "X");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);
        closeRect.sizeDelta = new Vector2(52f, 52f);

        MainTitleScene controller = parent.GetComponent<MainTitleScene>();
        if (controller != null)
        {
            AddButtonListener(closeButton, controller.CloseAllPanels);
        }

        return root;
    }

    private static Text CreateText(string text, Transform parent, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject("Text", parent, typeof(Text));
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;
        return uiText;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        foreach (System.Type component in components)
        {
            gameObject.AddComponent(component);
        }

        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void AddButtonListener(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        AddBuildScene(scenes, ScenePath);
        AddBuildScene(scenes, GameplayScenePath);

        foreach (EditorBuildSettingsScene existingScene in EditorBuildSettings.scenes)
        {
            if (existingScene == null || existingScene.path == ScenePath || existingScene.path == GameplayScenePath)
            {
                continue;
            }

            scenes.Add(existingScene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
