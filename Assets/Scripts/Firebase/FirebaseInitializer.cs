using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

namespace Game.Firebase
{
    public class FirebaseInitializer : MonoBehaviour
    {
        public static FirebaseInitializer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        public enum InitState { Pending, Ready, Failed }

        public InitState State { get; private set; } = InitState.Pending;
        public bool IsReady => State == InitState.Ready;

        [Tooltip("비워두면 Resources/FirebaseConfig 를 자동 로드.")]
        [SerializeField] FirebaseConfig config;

        FirebaseApp _app;
        public FirebaseApp App => _app;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                config = Resources.Load<FirebaseConfig>("FirebaseConfig");

            if (config == null || !config.IsValid)
            {
                Debug.LogError("[Firebase] FirebaseConfig 누락 또는 필수값 비어있음");
                State = InitState.Failed;
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _app = FirebaseApp.DefaultInstance;
                    var opts = _app.Options;
                    opts.ApiKey = config.apiKey;
                    opts.AppId = config.appId;
                    opts.ProjectId = config.projectId;
                    if (!string.IsNullOrEmpty(config.databaseUrl))
                        opts.DatabaseUrl = new Uri(config.databaseUrl);
                    if (!string.IsNullOrEmpty(config.storageBucket))
                        opts.StorageBucket = config.storageBucket;

                    State = InitState.Ready;
                    Debug.Log("[Firebase] 초기화 성공");
                }
                else
                {
                    State = InitState.Failed;
                    Debug.LogError($"[Firebase] 초기화 실패: {task.Result}");
                }
            });
        }

        public async UniTask<bool> WaitUntilReadyAsync()
        {
            await UniTask.WaitUntil(() => State != InitState.Pending);
            return State == InitState.Ready;
        }
    }
}
