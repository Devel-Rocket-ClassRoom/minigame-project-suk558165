using UnityEngine;

/// <summary>
/// NPC에 붙이는 컴포넌트.
/// DialogueData ScriptableObject를 연결하고,
/// 씬에 DialogueUI가 있어야 대화창이 표시됩니다.
/// </summary>
public class NpcController : MonoBehaviour
{
    [Header("대화 데이터")]
    public DialogueData dialogueData;

    [Header("강화창 (설정 시 대화 대신 강화창을 연다)")]
    public UpgradeShopUI upgradeShopUI;

    [Header("상호작용 범위")]
    public float interactRange = 2f;

    [Header("힌트 오브젝트 (말풍선 등, 없어도 됨)")]
    public GameObject hintObject;

    private Transform player;
    private bool isTalking;

    void Start()
    {
        player = PlayerRef.Transform;
        if (hintObject != null)
            hintObject.SetActive(false);
    }

    void Update()
    {
        if (player == null)
        {
            if (PlayerRef.Exists)
                player = PlayerRef.Transform;
            return;
        }

        // 대화 중일 때 입력을 DialogueUI로 전달
        if (isTalking)
        {
            DialogueUI.Instance?.HandleInput();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        // 강화 NPC: 대화 대신 강화창을 토글한다.
        if (upgradeShopUI != null)
        {
            if (hintObject != null)
                hintObject.SetActive(inRange && !UpgradeShopUI.IsOpen);

            if (!inRange)
                return;

            var key = InputManager.Instance?.Interact ?? KeyCode.A;
            if (Input.GetKeyDown(key))
            {
                if (UpgradeShopUI.IsOpen)
                    upgradeShopUI.Close();
                else
                    upgradeShopUI.Open();
            }
            return;
        }

        if (hintObject != null)
            hintObject.SetActive(inRange && !DialogueUI.IsOpen);

        if (!inRange || DialogueUI.IsOpen)
            return;

        var interactKey = InputManager.Instance?.Interact ?? KeyCode.A;
        if (Input.GetKeyDown(interactKey))
            StartTalk();
    }

    void StartTalk()
    {
        if (DialogueUI.Instance == null || dialogueData == null)
            return;

        isTalking = true;
        if (hintObject != null)
            hintObject.SetActive(false);

        DialogueUI.Instance.StartDialogue(dialogueData, OnDialogueFinished);
    }

    void OnDialogueFinished()
    {
        isTalking = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
