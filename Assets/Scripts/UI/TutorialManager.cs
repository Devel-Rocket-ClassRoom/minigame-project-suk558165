using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("화면에 표시할 안내 텍스트")]
        public string message;
        [Tooltip("이 입력이 감지되면 통과 (None이면 아무 키)")]
        public TutorialInput requiredInput;
    }

    public enum TutorialInput
    {
        MoveLeftRight,
        Jump,
        Dash,
        Attack,
    }

    [SerializeField] private Step[] steps;
    [SerializeField] private TutorialStepUI stepUI;

    public System.Action onTutorialComplete;

    public void Begin()
    {
        StartCoroutine(RunSteps());
    }

    IEnumerator RunSteps()
    {
        foreach (var step in steps)
        {
            if (stepUI != null)
                stepUI.Show(step.message);

            yield return WaitForInput(step.requiredInput);

            if (stepUI != null)
                yield return stepUI.Hide();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.tutorialCompleted = true;
            SaveManager.Instance.Save();
        }

        onTutorialComplete?.Invoke();
    }

    IEnumerator WaitForInput(TutorialInput input)
    {
        while (!CheckInput(input))
            yield return null;
    }

    bool CheckInput(TutorialInput input)
    {
        switch (input)
        {
            case TutorialInput.MoveLeftRight:
                return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
            case TutorialInput.Jump:
                return Input.GetButtonDown("Jump");
            case TutorialInput.Dash:
                var dashKey = InputManager.Instance?.Dash ?? KeyCode.Z;
                return Input.GetKeyDown(dashKey);
            case TutorialInput.Attack:
                var attackKey = InputManager.Instance?.Attack ?? KeyCode.X;
                return Input.GetKeyDown(attackKey);
            default:
                return Input.anyKeyDown;
        }
    }
}
