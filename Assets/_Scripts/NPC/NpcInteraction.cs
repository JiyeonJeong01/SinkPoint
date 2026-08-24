using UnityEngine;

[DisallowMultipleComponent]
public sealed class NpcInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NpcDialogueCanvas canvasPrefab;
    [SerializeField] private Transform overheadAnchor;

    [Header("Dialogue")]
    [TextArea]
    [SerializeField] private string[] firstDialogue =
    {
        "이 갑작스러운 싱크홀까지 내려가겠다니... 고생이 많군.",
        "아래로 내려갈수록 중력 이상 현상은 더 강해질 거야. 발밑과 천장을 둘 다 의심해.",
        "하지만 원인은 최하부에 반드시 있다. 그곳까지 도달해서 근원을 확인해줘."
    };

    [TextArea]
    [SerializeField] private string[] repeatDialogue =
    {
        "어서 가봐. 최하부에 답이 있을 거야."
    };

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showDebugLog;

    private NpcDialogueCanvas activeCanvas;
    private PlayerInput currentPlayerInput;
    private bool playerInside;
    private bool dialogueRunning;
    private bool completedFirstDialogue;

    private Transform OverheadAnchor => overheadAnchor != null ? overheadAnchor : transform;

    private void Awake()
    {
        EnsureCanvasInstance();
        SetQuestionMarkVisible(false);
    }

    private void OnDisable()
    {
        if (dialogueRunning && currentPlayerInput != null)
        {
            currentPlayerInput.SetGameplayInput();
        }

        dialogueRunning = false;
        SetQuestionMarkVisible(false);
    }

    private void Update()
    {
        if (!playerInside || dialogueRunning || currentPlayerInput == null)
        {
            return;
        }

        if (currentPlayerInput.InteractPressed)
        {
            BeginDialogue();
        }
    }

    public void HandlePlayerEntered(Collider other)
    {
        HandlePlayerDetected(other);
    }

    public void HandlePlayerExited(Collider other)
    {
        if (!TryResolvePlayerInput(other, out PlayerInput playerInput) || playerInput != currentPlayerInput)
        {
            return;
        }

        playerInside = false;
        currentPlayerInput = null;

        if (!dialogueRunning)
        {
            SetQuestionMarkVisible(false);
        }
    }

    public bool HandlePlayerDetected(Collider other)
    {
        if (!TryResolvePlayerInput(other, out PlayerInput playerInput))
        {
            return false;
        }

        currentPlayerInput = playerInput;
        playerInside = true;
        SetQuestionMarkVisible(!dialogueRunning);
        return true;
    }

    public void HandlePlayerDetectionLost()
    {
        if (dialogueRunning || !playerInside)
        {
            return;
        }

        playerInside = false;
        currentPlayerInput = null;
        SetQuestionMarkVisible(false);
    }

    private void BeginDialogue()
    {
        EnsureCanvasInstance();
        if (activeCanvas == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[{nameof(NpcInteraction)}] NPC Canvas prefab is not assigned.", this);
            }

            return;
        }

        dialogueRunning = true;
        SetQuestionMarkVisible(false);

        string[] lines = completedFirstDialogue ? repeatDialogue : firstDialogue;
        activeCanvas.BeginDialogue(lines, currentPlayerInput, OnDialogueEnded);
    }

    private void OnDialogueEnded()
    {
        completedFirstDialogue = true;
        dialogueRunning = false;
        SetQuestionMarkVisible(playerInside);
    }

    private void EnsureCanvasInstance()
    {
        if (activeCanvas != null || canvasPrefab == null)
        {
            return;
        }

        activeCanvas = Instantiate(canvasPrefab);
        activeCanvas.name = $"{name} NPC Canvas";
        activeCanvas.BindNpc(OverheadAnchor);
    }

    private void SetQuestionMarkVisible(bool visible)
    {
        if (activeCanvas != null)
        {
            activeCanvas.SetQuestionMarkVisible(visible);
        }
    }

    private bool TryResolvePlayerInput(Collider other, out PlayerInput playerInput)
    {
        playerInput = other != null ? other.GetComponentInParent<PlayerInput>() : null;
        if (playerInput != null)
        {
            return true;
        }

        if (!IsLikelyPlayerCollider(other))
        {
            return false;
        }

        // 플레이어 Collider가 입력 오브젝트와 분리된 테스트 배치를 대비한 MVP fallback입니다.
        playerInput = FindFirstObjectByType<PlayerInput>();
        return playerInput != null;
    }

    private bool IsLikelyPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        Transform root = other.transform.root;
        return other.name.Contains("Player") || (root != null && root.name.Contains("Player"));
    }

#if UNITY_EDITOR
    public void EditorConfigure(NpcDialogueCanvas dialogueCanvasPrefab, Transform anchor)
    {
        canvasPrefab = dialogueCanvasPrefab;
        overheadAnchor = anchor;
    }
#endif
}
