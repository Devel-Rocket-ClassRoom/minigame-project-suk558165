using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;

namespace Game.Firebase
{
    public class FirebaseAuthManager : MonoBehaviour
    {
        public static FirebaseAuthManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        FirebaseAuth _auth;
        FirebaseUser _user;
        bool _lastNotifiedSignedIn;

        public bool IsLoggedIn => _user != null;
        public string UserId => _user?.UserId ?? string.Empty;
        public string Email => _user?.Email;

        public event Action<FirebaseUser> OnSignedIn;
        public event Action OnSignedOut;

        void Awake()
        {
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
            if (FirebaseInitializer.Instance == null)
            {
                Debug.LogError("[Auth] FirebaseInitializer 누락");
                return;
            }
            bool ok = await FirebaseInitializer.Instance.WaitUntilReadyAsync();
            if (!ok)
            {
                Debug.LogError("[Auth] Firebase 초기화 실패로 Auth 사용 불가");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _auth.StateChanged += OnAuthStateChanged;

            _user = _auth.CurrentUser;
            Debug.Log(_user != null ? $"[Auth] 이미 로그인됨: {_user.UserId}" : "[Auth] 로그인 필요");
            NotifyLoginState();
        }

        void OnDestroy()
        {
            if (_auth != null) _auth.StateChanged -= OnAuthStateChanged;
        }

        void OnAuthStateChanged(object sender, EventArgs e)
        {
            _user = _auth.CurrentUser;
            NotifyLoginState();
        }

        void NotifyLoginState()
        {
            bool signedIn = IsLoggedIn;
            if (signedIn == _lastNotifiedSignedIn) return;
            _lastNotifiedSignedIn = signedIn;

            if (signedIn) OnSignedIn?.Invoke(_user);
            else OnSignedOut?.Invoke();
        }

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

        public void SignOut()
        {
            _auth?.SignOut();
        }

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
            return "이메일 또는 비밀번호를 확인해주세요.";
        }
    }
}
