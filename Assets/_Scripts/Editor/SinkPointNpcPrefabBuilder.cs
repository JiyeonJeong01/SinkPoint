using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SinkPointNpcPrefabBuilder
{
    private const string CharacterVisualPrefabPath =
        "Assets/Toon_Soldiers/ToonSoldiers_Armies/prefabs/army/TS-Armies_Recon_B.prefab";
    private const string RootFolder = "Assets/_Custom/Prefabs";
    private const string NpcFolder = RootFolder + "/NPC";
    private const string UiFolder = RootFolder + "/UI";
    private const string NpcPrefabPath = NpcFolder + "/NPC.prefab";
    private const string NpcCanvasPrefabPath = UiFolder + "/NPC Canvas.prefab";
    private const string NpcIdleControllerPath = "Assets/Toon_Soldiers/ToonSoldiers_Armies/sample_scene/sample_animator_guard.controller";

    private static readonly string[] PlayerOnlyComponentTypeNames =
    {
        "GrapplingHook",
        "PlayerFootsteps",
        nameof(PlayerAnimationController),
        nameof(PlayerCombatController),
        nameof(ThirdPersonCameraController),
        nameof(PlayerController),
        nameof(PlayerHealth),
        nameof(PlayerInput)
    };

    [MenuItem("SinkPoint/Generate MVP NPC Prefabs")]
    public static void Generate()
    {
        EnsureFolders();

        NpcDialogueCanvas canvasPrefab = CreateNpcCanvasPrefab();
        CreateNpcPrefab(canvasPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SinkPointNpcPrefabBuilder] Generated {NpcPrefabPath} and {NpcCanvasPrefabPath}.");
    }

    [MenuItem("SinkPoint/Repair NPC Prefab Independence")]
    public static void RepairNpcPrefabIndependence()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(NpcPrefabPath);
        try
        {
            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.UnpackPrefabInstance(
                    root,
                    PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);

                PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        root = PrefabUtility.LoadPrefabContents(NpcPrefabPath);
        try
        {
            RemovePlayerOnlyComponents(root);
            RemovePlayerPresentationExtras(root);
            RestoreCharacterVisualPrefabConnection(root);
            ConfigureNpcAnimators(root);

            PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SinkPointNpcPrefabBuilder] Repaired {NpcPrefabPath} as a standalone NPC prefab.");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets/_Custom", "Prefabs");
        CreateFolderIfMissing(RootFolder, "NPC");
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

    private static NpcDialogueCanvas CreateNpcCanvasPrefab()
    {
        GameObject root = new GameObject("NPC Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform questionMark = CreateQuestionMark(root.transform);
        GameObject dialoguePanel = CreateDialoguePanel(root.transform, out Text bodyText, out GameObject advancePrompt);

        NpcDialogueCanvas dialogueCanvas = root.AddComponent<NpcDialogueCanvas>();
        dialogueCanvas.EditorConfigure(canvas, questionMark, dialoguePanel, bodyText, advancePrompt);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, NpcCanvasPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<NpcDialogueCanvas>();
    }

    private static RectTransform CreateQuestionMark(Transform parent)
    {
        GameObject imageObject = CreateUiObject("QuestionMark Image", parent, typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(72f, 72f);

        Image image = imageObject.GetComponent<Image>();
        image.color = new Color(1f, 0.86f, 0.18f, 0.95f);

        Text questionText = CreateText("?", imageObject.transform, 46, TextAnchor.MiddleCenter);
        questionText.color = Color.black;
        Stretch(questionText.rectTransform, Vector2.zero, Vector2.zero);

        return rect;
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
        panelImage.color = new Color(0.03f, 0.035f, 0.04f, 0.86f);

        GameObject textPanel = CreateUiObject("Text Panel", panel.transform, typeof(Image));
        RectTransform textPanelRect = textPanel.GetComponent<RectTransform>();
        Stretch(textPanelRect, new Vector2(42f, 42f), new Vector2(-42f, -58f));

        Image textPanelImage = textPanel.GetComponent<Image>();
        textPanelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.72f);

        bodyText = CreateText(string.Empty, textPanel.transform, 30, TextAnchor.MiddleLeft);
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

        Text promptText = CreateText("SPACE", advancePrompt.transform, 18, TextAnchor.MiddleCenter);
        promptText.color = new Color(0.9f, 0.94f, 1f, 1f);
        Stretch(promptText.rectTransform, Vector2.zero, Vector2.zero);

        panel.SetActive(false);
        return panel;
    }

    private static void CreateNpcPrefab(NpcDialogueCanvas canvasPrefab)
    {
        GameObject root = new GameObject("NPC");
        CreateCharacterVisual(root.transform);

        Transform anchor = new GameObject("QuestionMarkAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 1.8f, 0f);

        NpcInteraction interaction = root.GetComponent<NpcInteraction>();
        if (interaction == null)
        {
            interaction = root.AddComponent<NpcInteraction>();
        }

        interaction.EditorConfigure(canvasPrefab, anchor);

        GameObject trigger = new GameObject("MapBoxTrigger", typeof(BoxCollider), typeof(Rigidbody), typeof(NpcMapBoxTrigger));
        trigger.transform.SetParent(root.transform, false);
        trigger.transform.localScale = Vector3.one * 2f;

        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = Vector3.one;

        Rigidbody triggerBody = trigger.GetComponent<Rigidbody>();
        triggerBody.isKinematic = true;
        triggerBody.useGravity = false;

        trigger.GetComponent<NpcMapBoxTrigger>().EditorConfigure(interaction);

        PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateCharacterVisual(Transform root)
    {
        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root, false);

        InstantiateCharacterVisual(visualRoot.transform);
        ConfigureNpcAnimators(root.gameObject);
    }

    private static void RestoreCharacterVisualPrefabConnection(GameObject root)
    {
        Transform visualRoot = root.transform.Find("VisualRoot");
        if (visualRoot == null)
        {
            visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(root.transform, false);
        }

        Transform existingVisual = visualRoot.Find("TS-Armies_Recon_B");
        if (existingVisual != null)
        {
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existingVisual.gameObject);
            if (sourcePath == CharacterVisualPrefabPath)
            {
                return;
            }

            Object.DestroyImmediate(existingVisual.gameObject, true);
        }

        InstantiateCharacterVisual(visualRoot);
    }

    private static void InstantiateCharacterVisual(Transform visualRoot)
    {

        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterVisualPrefabPath);
        if (visualPrefab == null)
        {
            throw new System.InvalidOperationException(
                $"NPC character visual prefab was not found at {CharacterVisualPrefabPath}.");
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, visualRoot.transform);
        visual.name = "TS-Armies_Recon_B";
        visual.transform.localPosition = new Vector3(0f, -0.235f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.3f;
    }

    private static void RemovePlayerOnlyComponents(GameObject root)
    {
        foreach (string typeName in PlayerOnlyComponentTypeNames)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    Object.DestroyImmediate(component, true);
                }
            }
        }

        RemoveRootComponent<CapsuleCollider>(root);
        RemoveRootComponent<Rigidbody>(root);
    }

    private static void RemovePlayerPresentationExtras(GameObject root)
    {
        AudioReverbFilter reverbFilter = root.GetComponent<AudioReverbFilter>();
        if (reverbFilter != null)
        {
            Object.DestroyImmediate(reverbFilter, true);
        }

        foreach (AudioSource audioSource in root.GetComponents<AudioSource>())
        {
            Object.DestroyImmediate(audioSource, true);
        }

        Transform grappleRope = root.transform.Find("GrappleRope");
        if (grappleRope != null)
        {
            Object.DestroyImmediate(grappleRope.gameObject, true);
        }
    }

    private static void RemoveRootComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component, true);
        }
    }

    private static void ConfigureNpcAnimators(GameObject root)
    {
        RuntimeAnimatorController idleController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcIdleControllerPath);

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.applyRootMotion = false;

            if (idleController != null)
            {
                animator.runtimeAnimatorController = idleController;
            }
        }
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

    private static Text CreateText(string text, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject("Text", parent, typeof(Text));
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
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
}
