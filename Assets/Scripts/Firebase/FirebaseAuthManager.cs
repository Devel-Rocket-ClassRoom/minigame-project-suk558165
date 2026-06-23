using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;

namespace Game.Firebase
{
    /// <summary>
    /// Firebase Authentication 매니저. 회원가입/로그인/익명 로그인/로그아웃을 담당.
    ///
    /// 주요 책임:
    ///  - FirebaseAuth.DefaultInstance 래핑 (직접 호출하지 않고 이 매니저를 통해 접근)
    ///  - StateChanged 이벤트를 OnSignedIn/OnSignedOut으로 노출 (다른 시스템이 구독)
    ///  - 영문 에러를 한글로 변환해 UI에 친화적인 메시지 제공
    ///  - 모든 메서드 반환 타입은 (bool success, string error) 튜플 → UI가 처리 간단
    ///
    /// 호출 순서:
    ///  Awake (싱글톤 등록) → Start (FirebaseInitializer 준비 대기 → _auth 세팅)
    ///
    /// 다른 시스템 연동:
    ///  - SaveManager가 OnSignedIn/OnSignedOut 구독 → 클라우드 동기화/계정 격리
    ///  - LoginUI가 OnSignedIn/OnSignedOut 구독 → 패널 보이기/숨기기
    ///  - GameFlowController가 OnSignedIn → GoToTitle (로그인 강제)
    public class FirebaseAuthManager : MonoBehaviour
    {
        public static FirebaseAuthManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        FirebaseAuth _auth;        // Firebase SDK 본체
        FirebaseUser _user;        // 현재 로그인된 사용자 (null이면 비로그인)
        bool _lastNotifiedSignedIn; // 중복 이벤트 발동 방지용

        public bool IsLoggedIn => _user != null;
        /// <summary>현재 사용자의 고유 ID. DB 경로 키 등에 사용 (users/{UserId}/save).</summary>
        public string UserId => _user?.UserId ?? string.Empty;
        public string Email => _user?.Email;

        /// <summary>로그인 성공/자동 로그인 시 발생. SaveManager 등이 구독.</summary>
        public event Action<FirebaseUser> OnSignedIn;
        /// <summary>로그아웃 시 발생.</summary>
        public event Action OnSignedOut;

        void Awake()
        {
            // 싱글톤 패턴
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async UniTaskVoid Start()
        {
            // FirebaseInitializer가 의존성 체크 + Config 적용을 끝낼 때까지 대기
            if (FirebaseInitializer.Instance == null)
            {
                Debug.LogError("[Auth] FirebaseInitializer 누락 — 씬에 추가 필요");
                return;
            }
            bool ok = await FirebaseInitializer.Instance.WaitUntilReadyAsync();
            if (!ok)
            {
                Debug.LogError("[Auth] Firebase 초기화 실패로 Auth 사용 불가");
                return;
            }

            // SDK 핸들 확보 + 상태 변경 이벤트 구독
            _auth = FirebaseAuth.DefaultInstance;
            _auth.StateChanged += OnAuthStateChanged;

            // 자동 로그인 체크 — 이전 세션에서 로그인했으면 토큰이 로컬에 저장돼 있음
            _user = _auth.CurrentUser;

            // 서버 검증: Firebase Console에서 계정이 삭제됐거나 토큰이 만료된 경우 자동 로그아웃
            if (_user != null)
            {
                try
                {
                    await _user.ReloadAsync();
                    // Reload 성공 = 계정 유효. 그대로 진행
                    Debug.Log($"[Auth] 자동 로그인 검증 성공: {_user.UserId}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Auth] 계정이 더 이상 유효하지 않음 → 로그아웃: {ex.Message}");
                    _auth.SignOut();
                    _user = null;
                }
            }
            else
            {
                Debug.Log("[Auth] 로그인 필요");
            }
            NotifyLoginState();
        }

        void OnDestroy()
        {
            // 이벤트 구독 해제 (메모리 누수 방지)
            if (_auth != null) _auth.StateChanged -= OnAuthStateChanged;
        }

        /// <summary>Firebase SDK가 인증 상태 변경 시 호출 (예: 토큰 갱신, 서버에서 강제 로그아웃 등).</summary>
        void OnAuthStateChanged(object sender, EventArgs e)
        {
            _user = _auth.CurrentUser;
            NotifyLoginState();
        }

        /// <summary>현재 상태에 맞춰 OnSignedIn/OnSignedOut 이벤트 발동. 중복 발동 방지.</summary>
        void NotifyLoginState()
        {
            bool signedIn = IsLoggedIn;
            if (signedIn == _lastNotifiedSignedIn) return; // 같은 상태면 발동 X
            _lastNotifiedSignedIn = signedIn;

            if (signedIn) OnSignedIn?.Invoke(_user);
            else OnSignedOut?.Invoke();
        }

        /// <summary>익명 로그인. 이메일 입력 없이 임시 계정 발급 (재설치 시 데이터 손실 가능).</summary>
        public async UniTask<(bool success, string error)> SignInAnonymouslyAsync()
        {
            if (_auth == null) return (false, "Firebase가 아직 초기화되지 않았습니다.");
            try
            {
                AuthResult result = await _auth.SignInAnonymouslyAsync();
                _user = result.User;
                NotifyLoginState();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ParseFirebaseError(ex.Message));
            }
        }

        /// <summary>이메일 회원가입. 6자 이상 비밀번호 필요. 기존 이메일이면 실패.</summary>
        public async UniTask<(bool success, string error)> SignUpAsync(string email, string password)
        {
            if (_auth == null) return (false, "Firebase가 아직 초기화되지 않았습니다.");
            try
            {
                AuthResult result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                _user = result.User;
                NotifyLoginState();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ParseFirebaseError(ex.Message));
            }
        }

        /// <summary>이메일 로그인. 계정 없거나 비밀번호 틀리면 실패.</summary>
        public async UniTask<(bool success, string error)> SignInAsync(string email, string password)
        {
            if (_auth == null) return (false, "Firebase가 아직 초기화되지 않았습니다.");
            try
            {
                AuthResult result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                _user = result.User;
                NotifyLoginState();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ParseFirebaseError(ex.Message));
            }
        }

        /// <summary>로그아웃. SDK가 로컬 토큰 삭제 → StateChanged 발동 → OnSignedOut 자동 발동.</summary>
        public void SignOut()
        {
            _auth?.SignOut();
        }

        /// <summary>
        /// Firebase 영문 에러 메시지를 사용자 친화적인 한글로 변환.
        /// SDK가 에러 코드를 직접 노출 안 하므로 문자열 매칭으로 처리.
        /// </summary>
        string ParseFirebaseError(string error)
        {
            string lower = error.ToLowerInvariant();
            if (lower.Contains("already in use") || lower.Contains("email-already"))
                return "이미 사용 중인 이메일입니다.";
            if (lower.Contains("at least 6") || lower.Contains("weak") || lower.Contains("password is invalid"))
                return "비밀번호는 6자 이상이어야 합니다.";
            if (lower.Contains("badly formatted") || lower.Contains("invalid-email"))
                return "이메일 형식이 올바르지 않습니다.";
            if (lower.Contains("network"))
                return "네트워크 연결을 확인해주세요.";
            if (lower.Contains("no user record") || lower.Contains("user-not-found"))
                return "존재하지 않는 계정입니다.";
            if (lower.Contains("wrong password") || lower.Contains("invalid-credential"))
                return "비밀번호가 올바르지 않습니다.";
            // 위에 안 걸리는 모든 경우 (보안상 정확한 원인 숨김)
            return "이메일 또는 비밀번호를 확인해주세요.";
        }
    }
}
