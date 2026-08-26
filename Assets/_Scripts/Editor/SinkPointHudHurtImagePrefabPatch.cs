using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SinkPointHudHurtImagePrefabPatch
{
    private const string HudPrefabPath = "Assets/_Custom/Prefabs/UI/InGame HUD Canvas.prefab";
    private const string HurtSpritePath = "Assets/_Custom/Textures/Hurt.png";

    [MenuItem("SinkPoint/Apply HUD Hurt Image")]
    public static void Apply()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            InGameHudCanvas hudCanvas = root.GetComponent<InGameHudCanvas>();
            if (hudCanvas == null)
            {
                Debug.LogError($"[SinkPointHudHurtImagePrefabPatch] {HudPrefabPath} has no InGameHudCanvas.");
                return;
            }

            Image hurtImage = FindChildImage(root.transform, "Hurt");
            if (hurtImage == null)
            {
                GameObject hurtObject = new GameObject("Hurt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                hurtObject.transform.SetParent(root.transform, false);
                hurtObject.transform.SetAsLastSibling();
                hurtImage = hurtObject.GetComponent<Image>();
            }

            RectTransform hurtRect = hurtImage.GetComponent<RectTransform>();
            Stretch(hurtRect);

            Sprite hurtSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HurtSpritePath);
            if (hurtSprite != null)
            {
                hurtImage.sprite = hurtSprite;
            }
            else
            {
                Debug.LogWarning($"[SinkPointHudHurtImagePrefabPatch] Hurt sprite not found: {HurtSpritePath}");
            }

            hurtImage.raycastTarget = false;
            hurtImage.type = Image.Type.Simple;
            hurtImage.preserveAspect = false;
            Color color = hurtImage.color;
            color.a = 0f;
            hurtImage.color = color;

            SerializedObject serializedHud = new SerializedObject(hudCanvas);
            serializedHud.FindProperty("hurtImage").objectReferenceValue = hurtImage;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SinkPointHudHurtImagePrefabPatch] Applied Hurt image to {HudPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Image FindChildImage(Transform root, string objectName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == objectName)
            {
                return images[i];
            }
        }

        return null;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }
}
