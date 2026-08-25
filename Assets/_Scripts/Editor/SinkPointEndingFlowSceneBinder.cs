using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SinkPointEndingFlowSceneBinder
{
    private const string GameplayScenePath = "Assets/_Scenes/Original_GamePlayScene.unity";
    private const string EndingCanvasPrefabPath = "Assets/_Custom/Prefabs/UI/Ending Canvas.prefab";

    [MenuItem("SinkPoint/Bind Gravity Source Ending Flow")]
    public static void Bind()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != GameplayScenePath)
        {
            scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        MonsterManager monsterManager = FindSceneComponent<MonsterManager>(scene);
        if (monsterManager == null)
        {
            Debug.LogError("[SinkPointEndingFlowSceneBinder] MonsterManager not found.");
            return;
        }

        GameObject gravitySource = FindSceneGameObject(scene, "GravitySource");
        if (gravitySource == null)
        {
            Debug.LogError("[SinkPointEndingFlowSceneBinder] GravitySource not found.");
            return;
        }

        Transform orbTransform = FindChildByName(gravitySource.transform, "Orb");
        if (orbTransform == null)
        {
            Debug.LogError("[SinkPointEndingFlowSceneBinder] GravitySource/Orb not found.");
            return;
        }

        EndingCanvas endingCanvas = FindSceneComponent<EndingCanvas>(scene);
        if (endingCanvas == null)
        {
            endingCanvas = InstantiateEndingCanvas(scene);
        }

        InGameHudCanvas hudCanvas = FindSceneComponent<InGameHudCanvas>(scene);
        PlayerInput playerInput = FindSceneComponent<PlayerInput>(scene);

        GravitySourceEndingController controller =
            monsterManager.GetComponent<GravitySourceEndingController>()
            ?? monsterManager.gameObject.AddComponent<GravitySourceEndingController>();

        Collider orbCollider = orbTransform.GetComponent<Collider>();
        if (orbCollider == null)
        {
            SphereCollider sphereCollider = orbTransform.gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = 1.5f;
            orbCollider = sphereCollider;
        }

        if (orbCollider is MeshCollider meshCollider)
        {
            meshCollider.convex = true;
        }

        orbCollider.isTrigger = true;

        GravitySourceOrbTrigger orbTrigger =
            orbTransform.GetComponent<GravitySourceOrbTrigger>()
            ?? orbTransform.gameObject.AddComponent<GravitySourceOrbTrigger>();

        controller.EditorConfigure(
            monsterManager,
            gravitySource,
            orbTrigger,
            endingCanvas,
            hudCanvas,
            playerInput);
        orbTrigger.EditorConfigure(controller, orbCollider);
        orbTrigger.SetTriggerEnabled(false);

        gravitySource.SetActive(false);

        EditorUtility.SetDirty(monsterManager);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(orbTrigger);
        EditorUtility.SetDirty(orbCollider);
        EditorUtility.SetDirty(gravitySource);
        if (endingCanvas != null)
        {
            EditorUtility.SetDirty(endingCanvas);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SinkPointEndingFlowSceneBinder] Bound GravitySource ending flow.");
    }

    private static EndingCanvas InstantiateEndingCanvas(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EndingCanvasPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[SinkPointEndingFlowSceneBinder] Ending Canvas prefab not found: {EndingCanvasPrefabPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Ending Canvas";
        instance.SetActive(true);
        return instance.GetComponent<EndingCanvas>();
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.scene == scene)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindSceneGameObject(Scene scene, string objectName)
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject gameObject = gameObjects[i];
            if (gameObject != null
                && gameObject.scene == scene
                && gameObject.name == objectName)
            {
                return gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
