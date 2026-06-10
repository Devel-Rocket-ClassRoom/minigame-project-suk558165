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

    [SerializeField]
    private Step[] steps;

    [SerializeField]
    private TutorialStepUI stepUI;

    public System.Action onTutorialComplete;

    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    public void Begin(GameObject player = null)
    {
        if (stepUI == null)
            stepUI = TutorialStepUI.Instance;
        if (player != null)
        {
            playerCombat = player.GetComponent<PlayerCombat>();
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        StartCoroutine(RunSteps());
    }

    IEnumerator RunSteps()
    {
        foreach (var step in steps)
        {
            if (stepUI != null)
                stepUI.Show($"{step.message}  0 / 5");

            yield return WaitForInput(step.requiredInput, step.message);

            if (stepUI != null)
                yield return stepUI.Hide();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.tutorialCompleted = true;
            SaveManager.Instance.Save();
        }

        if (stepUI != null)
        {
            stepUI.Show("튜토리얼이 완료되었습니다\n3초 뒤에 이동합니다");
            yield return new WaitForSeconds(3f);
            yield return stepUI.Hide();
        }

        onTutorialComplete?.Invoke();
    }

    IEnumerator WaitForInput(TutorialInput input, string baseMessage)
    {
        int count = 0;
        bool prevActive = false;
        while (count < 5)
        {
            yield return null;

            bool counted;
            if (UsesAnimationGate(input))
            {
                // 공격/대시는 애니메이션(동작)이 끝나는 순간을 1회로 인정.
                // 연타해도 동작 중에는 카운트가 올라가지 않는다.
                bool active = IsActionInProgress(input);
                counted = prevActive && !active;
                prevActive = active;
            }
            else
            {
                counted = CheckInput(input);
            }

            if (counted)
            {
                count++;
                if (stepUI != null)
                    stepUI.UpdateText($"{baseMessage}  {count} / 5");
            }
        }
    }

    bool UsesAnimationGate(TutorialInput input) =>
        input == TutorialInput.Attack || input == TutorialInput.Dash;

    bool IsActionInProgress(TutorialInput input)
    {
        switch (input)
        {
            case TutorialInput.Attack:
                return playerCombat != null && playerCombat.IsAttacking;
            case TutorialInput.Dash:
                return playerMovement != null && playerMovement.IsDashing;
            default:
                return false;
        }
    }

    bool CheckInput(TutorialInput input)
    {
        switch (input)
        {
            case TutorialInput.MoveLeftRight:
                return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
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
