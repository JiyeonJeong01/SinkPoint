using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SinkPointEndingCanvasPrefabBuilder
{
    private const string UiFolder = "Assets/_Custom/Prefabs/UI";
    private const string EndingCanvasPrefabPath = UiFolder + "/Ending Canvas.prefab";

    [MenuItem("SinkPoint/Generate Ending Canvas Prefab")]
    public static void Generate()
    {
        EnsureFolder("Assets/_Custom", "Prefabs");
        EnsureFolder("Assets/_Custom/Prefabs", "UI");

        GameObject root = new GameObject("Ending Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject topPanel = CreateTopPanel(root.transform, out Text topText);
        GameObject dialoguePanel = CreateDialoguePanel(root.transform, out Text bodyText, out GameObject advancePrompt);

        EndingCanvas endingCanvas = root.AddComponent<EndingCanvas>();
        endingCanvas.EditorConfigure(topPanel, topText, dialoguePanel, bodyText, advancePrompt);

        PrefabUtility.SaveAsPrefabAsset(root, EndingCanvasPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SinkPointEndingCanvasPrefabBuilder] Generated {EndingCanvasPrefabPath}.");
    }

    private static GameObject CreateTopPanel(Transform parent, out Text topText)
    {
        GameObject panel = CreateUiObject("Top Objective Panel", parent, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -56f);
        panelRect.sizeDelta = new Vector2(520f, 58f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.88f);

        topText = CreateText("[조사 목표 도달]", panel.transform, 28, TextAnchor.MiddleCenter, FontStyle.Bold);
        topText.color = new Color(0.96f, 0.92f, 0.72f, 1f);
        Stretch(topText.rectTransform, new Vector2(24f, 8f), new Vector2(-24f, -8f));

        return panel;
    }

    private static GameObject CreateDialoguePanel(Transform parent, out Text bodyText, out GameObject advancePrompt)
    {
        GameObject panel = CreateUiObject("Dialogue Panel", parent, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 64f);
        panelRect.sizeDelta = new Vector2(1260f, 230f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.035f, 0.04f, 0.9f);

        GameObject textPanel = CreateUiObject("Text Panel", panel.transform, typeof(Image));
        Image textPanelImage = textPanel.GetComponent<Image>();
        textPanelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.78f);
        Stretch(textPanel.GetComponent<RectTransform>(), new Vector2(42f, 42f), new Vector2(-42f, -58f));

        bodyText = CreateText(string.Empty, textPanel.transform, 30, TextAnchor.MiddleLeft, FontStyle.Normal);
        bodyText.color = new Color(0.94f, 0.96f, 1f, 1f);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(bodyText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));

        advancePrompt = CreateUiObject("Space Advance Image", panel.transform, typeof(Image));
        RectTransform promptRect = advancePrompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(1f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(1f, 0f);
        promptRect.anchoredPosition = new Vector2(-42f, 22f);
        promptRect.sizeDelta = new Vector2(210f, 36f);

        Image promptImage = advancePrompt.GetComponent<Image>();
        promptImage.color = new Color(1f, 1f, 1f, 0.16f);

        Text promptText = CreateText("SPACE", advancePrompt.transform, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
        promptText.color = new Color(0.9f, 0.94f, 1f, 1f);
        Stretch(promptText.rectTransform, Vector2.zero, Vector2.zero);

        return panel;
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
