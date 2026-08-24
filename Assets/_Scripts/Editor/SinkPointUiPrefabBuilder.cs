using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SinkPointUiPrefabBuilder
{
    private const string RootFolder = "Assets/_Custom/Prefab";
    private const string UiFolder = RootFolder + "/UI";
    private const string HudPrefabPath = UiFolder + "/InGame HUD Canvas.prefab";
    private const string MonsterCanvasPrefabPath = UiFolder + "/Monster Canvas.prefab";

    [MenuItem("SinkPoint/Generate MVP UI Prefabs")]
    public static void Generate()
    {
        EnsureFolders();
        CreateHudPrefab();
        CreateMonsterCanvasPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SinkPointUiPrefabBuilder] Generated {HudPrefabPath} and {MonsterCanvasPrefabPath}.");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets/_Custom", "Prefab");
        CreateFolderIfMissing(RootFolder, "UI");
    }

    private static void CreateFolderIfMissing(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void CreateHudPrefab()
    {
        GameObject root = new GameObject("InGame HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Text hpText = CreateHudText("HP Text", root.transform, "HP 3 / 3", TextAnchor.UpperLeft);
        RectTransform hpRect = hpText.rectTransform;
        hpRect.anchorMin = new Vector2(0f, 1f);
        hpRect.anchorMax = new Vector2(0f, 1f);
        hpRect.pivot = new Vector2(0f, 1f);
        hpRect.anchoredPosition = new Vector2(32f, -28f);
        hpRect.sizeDelta = new Vector2(360f, 52f);

        Text monsterText = CreateHudText("Monster Count Text", root.transform, "MONSTERS 0", TextAnchor.UpperRight);
        RectTransform monsterRect = monsterText.rectTransform;
        monsterRect.anchorMin = new Vector2(1f, 1f);
        monsterRect.anchorMax = new Vector2(1f, 1f);
        monsterRect.pivot = new Vector2(1f, 1f);
        monsterRect.anchoredPosition = new Vector2(-32f, -28f);
        monsterRect.sizeDelta = new Vector2(460f, 52f);

        InGameHudCanvas hudCanvas = root.AddComponent<InGameHudCanvas>();
        hudCanvas.EditorConfigure(hpText, monsterText);

        PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateMonsterCanvasPrefab()
    {
        GameObject root = new GameObject("Monster Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        root.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(180f, 28f);

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        Slider slider = CreateHealthSlider(root.transform);

        MonsterHealthCanvas monsterCanvas = root.AddComponent<MonsterHealthCanvas>();
        monsterCanvas.EditorConfigure(slider);

        PrefabUtility.SaveAsPrefabAsset(root, MonsterCanvasPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Text CreateHudText(string name, Transform parent, string text, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(Text));
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = 30;
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = alignment;
        uiText.color = new Color(0.94f, 0.96f, 1f, 1f);
        uiText.raycastTarget = false;
        return uiText;
    }

    private static Slider CreateHealthSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("HP Slider", parent, typeof(Slider));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        Stretch(sliderRect, Vector2.zero, Vector2.zero);

        GameObject background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.86f);
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.78f, 0.1f, 0.08f, 0.95f);
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
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
}
