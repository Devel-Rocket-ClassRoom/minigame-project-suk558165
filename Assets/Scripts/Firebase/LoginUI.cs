using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Firebase;

/// <summary>
/// 로그인 화면 UI. 이메일/회원가입/익명/로그아웃 4종 처리.
///
/// 동작:
///  - FirebaseAuthManager.OnSignedIn/OnSignedOut 이벤트 구독 → 자동으로 패널 보이기/숨기기
///  - 버튼 클릭 → 매니저의 SignIn/SignUp/SignInAnonymously/SignOut 호출
///  - 결과 (bool success, string error) 받아서 성공 시 패널 닫고, 실패 시 에러 메시지 표시
///
/// 입력 검증:
///  - 이메일/비밀번호 빈 칸 체크
///  - 형식/비밀번호 길이 검증은 Firebase가 서버 측에서 처리 → 한글 메시지로 변환되어 전달됨
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    [Tooltip("로그인 폼 전체를 감싸는 패널. 로그인되면 자동으로 비활성화됨")]
    GameObject loginPanel;

    [Header("Form")]
    [SerializeField] TMP_InputField emailInput;
    [SerializeField]
    [Tooltip("Content Type을 Password로 설정해야 가려짐")]
    TMP_InputField passwordInput;
    [SerializeField]
    [Tooltip("에러 메시지 표시용. 입력 오류/로그인 실패 시 빨간 글씨로 표시")]
    TextMeshProUGUI errorText;

    [Header("Buttons")]
    [SerializeField] Button loginButton;
    [SerializeField] Button signupButton;
    [SerializeField] Button anonymousButton;
    [SerializeField]
    [Tooltip("로그인 상태일 때만 보임. 없으면 타이틀의 로그아웃 버튼만 사용")]
    Button logoutButton;

    void Start()
    {
        StartAsync().Forget();
    }

    async UniTaskVoid StartAsync()
    {
        // 버튼 클릭 이벤트 연결
        loginButton.onClick.AddListener(OnLogin);
        signupButton.onClick.AddListener(OnSignup);
        anonymousButton.onClick.AddListener(OnAnonymous);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogout);

        // 인증 상태 확정 전까지 패널 숨김 (깜빡임 방지)
        if (loginPanel != null) loginPanel.SetActive(false);
        if (logoutButton != null) logoutButton.gameObject.SetActive(false);
        ClearError();

        // 이벤트를 먼저 구독 — 타이밍 가정 없이 어떤 시점에 OnSignedIn이 발동돼도 받음
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnSignedIn += _ => RefreshUI();
            FirebaseAuthManager.Instance.OnSignedOut += RefreshUI;
        }

        // Firebase 초기화 완료까지 대기 — 이후 Auth.Start가 자동 로그인 체크하고 이벤트 발동
        if (FirebaseInitializer.Instance != null)
            await FirebaseInitializer.Instance.WaitUntilReadyAsync();

        // FirebaseAuthManager.Start가 Init 대기 후 자동 로그인 상태를 확정하는데,
        // 이 시점엔 아직 _auth 세팅이 안 됐을 수 있음 → IsLoggedIn 안정될 때까지 한 프레임 대기
        await UniTask.NextFrame();

        RefreshUI();
    }

    /// <summary>로그인 상태에 따라 패널/로그아웃 버튼 표시 토글.</summary>
    void RefreshUI()
    {
        var auth = FirebaseAuthManager.Instance;
        bool logged = auth != null && auth.IsLoggedIn;

        // 로그인되면 로그인 폼 숨김, 안 됐으면 표시
        if (loginPanel != null) loginPanel.SetActive(!logged);
        // 로그아웃 버튼은 반대 (로그인된 상태에서만 노출)
        if (logoutButton != null) logoutButton.gameObject.SetActive(logged);
    }

    // ── UI 버튼은 async void가 위험하므로 sync wrapper를 거치고
    //    실제 비동기 로직은 UniTaskVoid로 분리 (.Forget()으로 fire-and-forget) ──
    void OnLogin() => OnLoginAsync().Forget();
    void OnSignup() => OnSignupAsync().Forget();
    void OnAnonymous() => OnAnonymousAsync().Forget();

    async UniTaskVoid OnLoginAsync()
    {
        if (!ValidateInput(out var email, out var pw)) return;
        SetInteractable(false); // 응답 올 때까지 버튼 비활성 (중복 클릭 방지)
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
        // SignOut() 호출 → AuthManager의 StateChanged 발동 → OnSignedOut → RefreshUI 자동 호출됨
        FirebaseAuthManager.Instance.SignOut();
    }

    /// <summary>이메일/비밀번호 입력값 검증. 통과하면 out 변수로 반환.</summary>
    bool ValidateInput(out string email, out string pw)
    {
        email = emailInput.text.Trim(); // 양쪽 공백 제거
        pw = passwordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            ShowError("이메일과 비밀번호를 입력해주세요.");
            return false;
        }
        return true;
    }

    /// <summary>Auth 호출 결과 처리. 성공이면 패널 닫고, 실패면 에러 표시.</summary>
    void HandleResponse(bool success, string error)
    {
        if (success)
        {
            ClearError();
            RefreshUI(); // OnSignedIn 이벤트로도 호출되지만 명시적으로 한 번 더
        }
        else
        {
            ShowError(error); // AuthManager에서 이미 한글로 변환된 메시지
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
