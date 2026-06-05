using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화창 UI 싱글턴.
/// 씬의 Canvas 아래에 배치하고 Inspector에서 각 필드를 연결하세요.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    /// <summary>대화창이 열려 있는 동안 플레이어 입력을 막는 데 사용합니다.</summary>
    public static bool IsOpen { get; private set; }

    [Header("패널")]
    [SerializeField]
    private CanvasGroup panelGroup;

    [Header("텍스트")]
    [SerializeField]
    private TextMeshProUGUI speakerNameText;

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [Header("초상화")]
    [SerializeField]
    private Image portraitImage;

    [Header("계속 힌트 (▼ A키)")]
    [SerializeField]
    private GameObject continueHint;

    [Header("타이핑 속도 (초/글자)")]
    [SerializeField]
    private float charDelay = 0.04f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsOpen = false;
    }

    private DialogueLine[] lines;
    private int currentIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private System.Action onFinished;

    void Awake()
    {
        Instance = this;
        SetPanelVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
        }
    }

    // ── 외부 API ──────────────────────────────────────────

    /// <summary>대화를 시작합니다.</summary>
    public void StartDialogue(DialogueData data, System.Action onComplete = null)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
            return;

        lines = data.lines;
        currentIndex = 0;
        onFinished = onComplete;
        IsOpen = true;
        SetPanelVisible(true);
        ShowLine(lines[0]);
    }

    /// <summary>대화를 강제로 닫습니다.</summary>
    public void Close()
    {
        StopAllCoroutines();
        isTyping = false;
        IsOpen = false;
        SetPanelVisible(false);
        onFinished?.Invoke();
        onFinished = null;
    }

    // ── 입력 처리 (NpcController가 매 Update에서 호출) ────

    public void HandleInput()
    {
        if (!IsOpen)
            return;

        var interactKey = InputManager.Instance?.Interact ?? KeyCode.A;
        if (!Input.GetKeyDown(interactKey))
            return;

        if (isTyping)
        {
            // 타이핑 중이면 전체 텍스트 즉시 표시
            SkipTyping();
        }
        else
        {
            // 다음 줄로 이동
            currentIndex++;
            if (currentIndex < lines.Length)
                ShowLine(lines[currentIndex]);
            else
                Close();
        }
    }

    // ── 내부 ──────────────────────────────────────────────

    void ShowLine(DialogueLine line)
    {
        // 이름
        if (speakerNameText != null)
            speakerNameText.text = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;

        // 초상화
        if (portraitImage != null && line.portrait != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.enabled = true;
        }

        // 타이핑 효과
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        if (continueHint != null)
            continueHint.SetActive(false);
        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char c in fullText)
        {
            if (dialogueText != null)
                dialogueText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        if (continueHint != null)
            continueHint.SetActive(true);
    }

    void SkipTyping()
    {
        StopCoroutine(typingCoroutine);
        isTyping = false;
        if (dialogueText != null)
            dialogueText.text = lines[currentIndex].text;
        if (continueHint != null)
            continueHint.SetActive(true);
    }

    void SetPanelVisible(bool visible)
    {
        if (panelGroup == null)
            return;
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
        if (continueHint != null)
            continueHint.SetActive(false);
    }
}
