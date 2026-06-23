using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Firebase;

public class LoginUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject loginPanel;

    [Header("Form")]
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] TextMeshProUGUI errorText;

    [Header("Buttons")]
    [SerializeField] Button loginButton;
    [SerializeField] Button signupButton;
    [SerializeField] Button anonymousButton;
    [SerializeField] Button logoutButton;

    void Start()
    {
        loginButton.onClick.AddListener(OnLogin);
        signupButton.onClick.AddListener(OnSignup);
        anonymousButton.onClick.AddListener(OnAnonymous);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogout);

        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnSignedIn += _ => RefreshUI();
            FirebaseAuthManager.Instance.OnSignedOut += RefreshUI;
        }

        ClearError();
        RefreshUI();
    }

    void RefreshUI()
    {
        var auth = FirebaseAuthManager.Instance;
        bool logged = auth != null && auth.IsLoggedIn;

        if (loginPanel != null) loginPanel.SetActive(!logged);
        if (logoutButton != null) logoutButton.gameObject.SetActive(logged);
    }

    void OnLogin() => OnLoginAsync().Forget();
    void OnSignup() => OnSignupAsync().Forget();
    void OnAnonymous() => OnAnonymousAsync().Forget();

    async UniTaskVoid OnLoginAsync()
    {
        if (!ValidateInput(out var email, out var pw)) return;
        SetInteractable(false);
        var (success, error) = await FirebaseAuthManager.Instance.SignInAsync(email, pw);
        HandleResponse(success, error);
        SetInteractable(true);
    }

    async UniTaskVoid OnSignupAsync()
    {
        if (!ValidateInput(out var email, out var pw)) return;
        SetInteractable(false);
        var (success, error) = await FirebaseAuthManager.Instance.SignUpAsync(email, pw);
        HandleResponse(success, error);
        SetInteractable(true);
    }

    async UniTaskVoid OnAnonymousAsync()
    {
        SetInteractable(false);
        var (success, error) = await FirebaseAuthManager.Instance.SignInAnonymouslyAsync();
        HandleResponse(success, error);
        SetInteractable(true);
    }

    void OnLogout()
    {
        FirebaseAuthManager.Instance.SignOut();
    }

    bool ValidateInput(out string email, out string pw)
    {
        email = emailInput.text.Trim();
        pw = passwordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            ShowError("이메일과 비밀번호를 입력해주세요.");
            return false;
        }
        return true;
    }

    void HandleResponse(bool success, string error)
    {
        if (success)
        {
            ClearError();
            RefreshUI();
        }
        else
        {
            ShowError(error);
        }
    }

    void SetInteractable(bool v)
    {
        loginButton.interactable = v;
        signupButton.interactable = v;
        anonymousButton.interactable = v;
    }

    void ShowError(string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.color = Color.red;
    }

    void ClearError()
    {
        if (errorText != null) errorText.text = "";
    }
}
