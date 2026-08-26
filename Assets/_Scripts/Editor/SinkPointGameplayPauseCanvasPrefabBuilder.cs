using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SinkPointGameplayPauseCanvasPrefabBuilder
{
    private const string UiFolder = "Assets/_Custom/Prefabs/UI";
    private const string PauseCanvasPrefabPath = UiFolder + "/Gameplay Pause Canvas.prefab";

    [MenuItem("SinkPoint/Generate Gameplay Pause Canvas Prefab")]
    public static void Generate()
    {
        EnsureFolder("Assets/_Custom", "Prefabs");
        EnsureFolder("Assets/_Custom/Prefabs", "UI");

        GameObject root = new GameObject("Gameplay Pause Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject menuRoot = CreateMenuRoot(root.transform);
        Button mainTitleButton = CreateButton("Main Title Button", menuRoot.transform, "메인화면으로");
        Button exitButton = CreateButton("Exit Button", menuRoot.transform, "종료하기");

        PlaceButton(mainTitleButton.GetComponent<RectTransform>(), 38f);
        PlaceButton(exitButton.GetComponent<RectTransform>(), -38f);

        GameplayPauseCanvas pauseCanvas = root.AddComponent<GameplayPauseCanvas>();
        pauseCanvas.EditorConfigure(menuRoot, mainTitleButton, exitButton);

        PrefabUtility.SaveAsPrefabAsset(root, PauseCanvasPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SinkPointGameplayPauseCanvasPrefabBuilder] Generated {PauseCanvasPrefabPath}.");
    }

    private static GameObject CreateMenuRoot(Transform parent)
    {
        GameObject blocker = CreateUiObject("Input Blocker", parent, typeof(Image));
        Stretch(blocker.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = CreateUiObject("Menu Panel", blocker.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(520f, 280f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.03f, 0.035f, 0.94f);

        Text title = CreateText("일시 정지", panel.transform, 34, TextAnchor.MiddleCenter, FontStyle.Bold);
        title.color = new Color(0.95f, 0.96f, 1f, 1f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(440f, 52f);

        blocker.SetActive(false);
        return blocker;
    }

    private static Button CreateButton(string name, Transform menuRoot, string label)
    {
        Transform panel = menuRoot.Find("Menu Panel");
        GameObject buttonObject = CreateUiObject(name, panel != null ? panel : menuRoot, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.17f, 0.19f, 0.22f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.27f, 0.3f, 0.34f, 1f);
        colors.pressedColor = new Color(0.1f, 0.11f, 0.13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText(label, buttonObject.transform, 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        text.color = Color.white;
        Stretch(text.rectTransform, new Vector2(18f, 6f), new Vector2(-18f, -6f));

        return button;
    }

    private static void PlaceButton(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
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

    private static Text CreateText(
        string text,
        Transform parent,
        int fontSize,
        TextAnchor alignment,
        FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject("Text", parent, typeof(Text));
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.raycastTarget = false;
        return uiText;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
