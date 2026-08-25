using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GravitySourceEndingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private GameObject gravitySource;
    [SerializeField] private GravitySourceOrbTrigger orbTrigger;
    [SerializeField] private EndingCanvas endingCanvas;
    [SerializeField] private InGameHudCanvas hudCanvas;
    [SerializeField] private PlayerInput playerInput;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float sourceMoveDuration = 2.5f;
    [SerializeField, Min(0f)] private float endingFadeDuration = 1.2f;
    [SerializeField] private string mainTitleSceneName = "MainTitleScene";

    private Vector3 sourcePlacedPosition;
    private Quaternion sourcePlacedRotation;
    private bool sourceReady;
    private bool endingRunning;
    private Coroutine sourceMoveRoutine;

    private void Awake()
    {
        ResolveReferences();
        CacheSourcePlacedTransform();
        SetOrbTriggerEnabled(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (monsterManager != null)
        {
            monsterManager.LastMonsterDied -= OnLastMonsterDied;
            monsterManager.LastMonsterDied += OnLastMonsterDied;
        }
    }

    private void OnDisable()
    {
        if (monsterManager != null)
        {
            monsterManager.LastMonsterDied -= OnLastMonsterDied;
        }
    }

    public void HandleOrbTriggered(PlayerInput triggeringPlayerInput)
    {
        if (!sourceReady || endingRunning)
        {
            return;
        }

        StartCoroutine(EndingRoutine(triggeringPlayerInput));
    }

    private void OnLastMonsterDied(Monster monster)
    {
        if (gravitySource == null)
        {
            Debug.LogWarning("[GravitySourceEndingController] GravitySource is not assigned.", this);
            return;
        }

        Vector3 startPosition = monster != null ? monster.transform.position : sourcePlacedPosition;
        if (sourceMoveRoutine != null)
        {
            StopCoroutine(sourceMoveRoutine);
        }

        sourceMoveRoutine = StartCoroutine(MoveSourceRoutine(startPosition));
    }

    private IEnumerator MoveSourceRoutine(Vector3 startPosition)
    {
        sourceReady = false;
        SetOrbTriggerEnabled(false);

        gravitySource.SetActive(true);
        Transform sourceTransform = gravitySource.transform;
        sourceTransform.position = startPosition;
        sourceTransform.rotation = sourcePlacedRotation;

        if (sourceMoveDuration <= 0f)
        {
            sourceTransform.position = sourcePlacedPosition;
            sourceReady = true;
            SetOrbTriggerEnabled(true);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < sourceMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sourceMoveDuration);
            float smoothed = t * t * (3f - 2f * t);
            sourceTransform.position = Vector3.Lerp(startPosition, sourcePlacedPosition, smoothed);
            yield return null;
        }

        sourceTransform.position = sourcePlacedPosition;
        sourceReady = true;
        SetOrbTriggerEnabled(true);
        sourceMoveRoutine = null;
    }

    private IEnumerator EndingRoutine(PlayerInput triggeringPlayerInput)
    {
        endingRunning = true;
        SetOrbTriggerEnabled(false);

        playerInput = triggeringPlayerInput != null ? triggeringPlayerInput : playerInput;
        playerInput ??= FindFirstObjectByType<PlayerInput>();
        hudCanvas ??= FindFirstObjectByType<InGameHudCanvas>();
        endingCanvas ??= FindFirstObjectByType<EndingCanvas>(FindObjectsInactive.Include);

        if (playerInput != null)
        {
            playerInput.SetCutsceneInput();
        }

        bool dialogueEnded = false;
        if (endingCanvas != null)
        {
            endingCanvas.gameObject.SetActive(true);
            endingCanvas.BeginEnding(playerInput, () => dialogueEnded = true);
            yield return new WaitUntil(() => dialogueEnded);
        }
        else
        {
            Debug.LogWarning("[GravitySourceEndingController] EndingCanvas is not assigned.", this);
        }

        if (playerInput != null)
        {
            playerInput.SetCutsceneInput();
        }

        if (hudCanvas != null)
        {
            yield return hudCanvas.FadeScreenRoutine(1f, endingFadeDuration);
        }

        if (!string.IsNullOrWhiteSpace(mainTitleSceneName))
        {
            SceneManager.LoadScene(mainTitleSceneName);
        }
    }

    private void ResolveReferences()
    {
        monsterManager ??= FindFirstObjectByType<MonsterManager>();
        endingCanvas ??= FindFirstObjectByType<EndingCanvas>(FindObjectsInactive.Include);
        hudCanvas ??= FindFirstObjectByType<InGameHudCanvas>();
        playerInput ??= FindFirstObjectByType<PlayerInput>();

        if (gravitySource == null)
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildByName(roots[i].transform, "GravitySource");
                if (found != null)
                {
                    gravitySource = found.gameObject;
                    break;
                }
            }
        }

        if (orbTrigger == null && gravitySource != null)
        {
            Transform orb = FindChildByName(gravitySource.transform, "Orb");
            if (orb != null)
            {
                orbTrigger = orb.GetComponent<GravitySourceOrbTrigger>();
            }
        }
    }

    private void CacheSourcePlacedTransform()
    {
        if (gravitySource == null)
        {
            return;
        }

        Transform sourceTransform = gravitySource.transform;
        sourcePlacedPosition = sourceTransform.position;
        sourcePlacedRotation = sourceTransform.rotation;
    }

    private void SetOrbTriggerEnabled(bool enabled)
    {
        if (orbTrigger != null)
        {
            orbTrigger.SetTriggerEnabled(enabled);
        }
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

#if UNITY_EDITOR
    public void EditorConfigure(
        MonsterManager manager,
        GameObject source,
        GravitySourceOrbTrigger trigger,
        EndingCanvas ending,
        InGameHudCanvas hud,
        PlayerInput input)
    {
        monsterManager = manager;
        gravitySource = source;
        orbTrigger = trigger;
        endingCanvas = ending;
        hudCanvas = hud;
        playerInput = input;
    }
#endif
}
