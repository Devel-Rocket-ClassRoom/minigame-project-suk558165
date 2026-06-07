using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("말하는 NPC 이름 (비우면 기존 이름 유지)")]
    public string speakerName;

    [TextArea(2, 5)]
    public string text;

    [Tooltip("초상화 스프라이트 (없으면 기존 유지)")]
    public Sprite portrait;
}

[CreateAssetMenu(menuName = "Game/DialogueData", fileName = "NewDialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}
