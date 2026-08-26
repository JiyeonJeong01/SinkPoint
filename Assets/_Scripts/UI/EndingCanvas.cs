using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EndingCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject topPanel;
    [SerializeField] private Text topText;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject advancePrompt;

    [Header("Dialogue")]
    [SerializeField] private string objectiveText = "[조사 목표 도달]";
    [TextArea]
    [SerializeField] private string[] endingLines =
    {
        "중력 변칙의 중심부를 확인했다.",
        "에너지 반응이 임계값을 넘어서고 있다.",
        "데이터를 전송한다.",
        "지상 복귀 후 상세 보고를 진행하겠다."
    };

    [Header("Typing")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 35f;

    private PlayerInput lockedInput;
    private Action ended;
    private Coroutine typingRoutine;
    private int lineIndex;
    private bool sequenceActive;
    private bool typing;
    private string currentLine = string.Empty;

    private void Awake()
    {
        SetVisible(false);
        SetAdvancePromptVisible(false);
    }

    private void Update()
    {
        if (!sequenceActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    public void BeginEnding()
    {
        BeginEnding(FindFirstObjectByType<PlayerInput>(), null);
    }

    public void BeginEnding(PlayerInput playerInput, Action onEnded = null)
    {
        if (sequenceActive)
        {
            return;
        }

        lockedInput = playerInput;
        ended = onEnded;
        lineIndex = 0;
        sequenceActive = true;

        if (topText != null)
        {
            topText.text = objectiveText;
        }

        if (lockedInput != null)
        {
            lockedInput.SetCutsceneInput();
        }

        SetVisible(true);
        ShowLine(GetLine(lineIndex));
    }

    private void AdvanceDialogue()
    {
        if (typing)
        {
            FinishTypingImmediately();
            return;
        }

        lineIndex++;
        if (lineIndex >= endingLines.Length)
        {
            EndEnding();
            return;
        }

        ShowLine(GetLine(lineIndex));
    }

    private void ShowLine(string line)
    {
        currentLine = line;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeLine(currentLine));
    }

    private IEnumerator TypeLine(string line)
    {
        typing = true;
        SetAdvancePromptVisible(false);

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;
        for (int i = 0; i < line.Length; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.text = line.Substring(0, i + 1);
            }

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return null;
            }
        }

        typing = false;
        typingRoutine = null;
        SetAdvancePromptVisible(true);
    }

    private void FinishTypingImmediately()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        typing = false;
        if (dialogueText != null)
        {
            dialogueText.text = currentLine;
        }

        SetAdvancePromptVisible(true);
    }

    private void EndEnding()
    {
        sequenceActive = false;
        FinishTypingImmediately();
        SetVisible(false);

        if (lockedInput != null)
        {
            lockedInput.SetGameplayInput();
        }

        lockedInput = null;
        Action callback = ended;
        ended = null;
        callback?.Invoke();
    }

    private string GetLine(int index)
    {
        if (endingLines == null || endingLines.Length == 0)
        {
            return "...";
        }

        return endingLines[Mathf.Clamp(index, 0, endingLines.Length - 1)] ?? string.Empty;
    }

    private void SetVisible(bool visible)
    {
        SetPanelActive(topPanel, visible);
        SetPanelActive(dialoguePanel, visible);
    }

    private void SetAdvancePromptVisible(bool visible)
    {
        SetPanelActive(advancePrompt, visible);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(GameObject top, Text topLabel, GameObject dialogue, Text body, GameObject prompt)
    {
        topPanel = top;
        topText = topLabel;
        dialoguePanel = dialogue;
        dialogueText = body;
        advancePrompt = prompt;
    }
#endif
}
