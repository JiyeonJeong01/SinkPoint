using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NpcDialogueCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform questionMarkImage;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject advancePrompt;

    [Header("Typing")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 35f;

    private readonly WaitForSecondsRealtime typingDelay = new WaitForSecondsRealtime(0.02f);

    private PlayerInput lockedInput;
    private Action ended;
    private Coroutine typingRoutine;
    private string[] lines = Array.Empty<string>();
    private int lineIndex;
    private bool dialogueActive;
    private bool typing;
    private string currentLine = string.Empty;

    private void Awake()
    {
        canvas ??= GetComponent<Canvas>();
        SetQuestionMarkVisible(false);
        SetDialogueVisible(false);
    }

    private void Update()
    {
        if (!dialogueActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    public void BindNpc(Transform anchor)
    {
    }

    public void SetQuestionMarkVisible(bool visible)
    {
        if (questionMarkImage != null)
        {
            questionMarkImage.gameObject.SetActive(visible);
        }
    }

    public void BeginDialogue(string[] dialogueLines, PlayerInput playerInput, Action onEnded)
    {
        lines = dialogueLines != null && dialogueLines.Length > 0
            ? dialogueLines
            : new[] { "..." };

        lockedInput = playerInput;
        ended = onEnded;
        lineIndex = 0;
        dialogueActive = true;

        if (lockedInput != null)
        {
            lockedInput.SetDialogueInput();
        }

        SetQuestionMarkVisible(false);
        SetDialogueVisible(true);
        ShowLine(lines[lineIndex]);
    }

    private void AdvanceDialogue()
    {
        if (typing)
        {
            FinishTypingImmediately();
            return;
        }

        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine(lines[lineIndex]);
    }

    private void ShowLine(string line)
    {
        currentLine = line ?? string.Empty;

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
                yield return typingDelay;
            }
        }

        typing = false;
        SetAdvancePromptVisible(true);
        typingRoutine = null;
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

    private void EndDialogue()
    {
        dialogueActive = false;
        FinishTypingImmediately();
        SetDialogueVisible(false);

        if (lockedInput != null)
        {
            lockedInput.SetGameplayInput();
        }

        lockedInput = null;
        Action callback = ended;
        ended = null;
        callback?.Invoke();
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(visible);
        }
    }

    private void SetAdvancePromptVisible(bool visible)
    {
        if (advancePrompt != null)
        {
            advancePrompt.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        Canvas ownerCanvas,
        RectTransform questionMark,
        GameObject panel,
        Text bodyText,
        GameObject prompt)
    {
        canvas = ownerCanvas;
        questionMarkImage = questionMark;
        dialoguePanel = panel;
        dialogueText = bodyText;
        advancePrompt = prompt;
    }
#endif
}
