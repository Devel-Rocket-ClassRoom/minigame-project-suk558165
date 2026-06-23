using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Firebase.Database;

namespace Game.Firebase
{
    public class FirebaseDBManager : MonoBehaviour
    {
        public static FirebaseDBManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        DatabaseReference _root;
        public bool IsReady => _root != null;

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
                Debug.LogError("[DB] FirebaseInitializer 누락");
                return;
            }
            bool ok = await FirebaseInitializer.Instance.WaitUntilReadyAsync();
            if (!ok) { Debug.LogError("[DB] Firebase 초기화 실패"); return; }
            _root = FirebaseDatabase.DefaultInstance.RootReference;
        }

        public async UniTask SetJsonAsync(string path, string json)
            => await _root.Child(path).SetRawJsonValueAsync(json);

        public UniTask SetAsync<T>(string path, T data)
            => SetJsonAsync(path, JsonUtility.ToJson(data));

        public async UniTask<string> GetJsonAsync(string path)
        {
            DataSnapshot snap = await _root.Child(path).GetValueAsync();
            return snap.Exists ? snap.GetRawJsonValue() : null;
        }

        public async UniTask<T> GetAsync<T>(string path) where T : class
        {
            string json = await GetJsonAsync(path);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<T>(json);
        }

        public async UniTask SetValueAsync(string path, object value)
            => await _root.Child(path).SetValueAsync(value);

        public async UniTask<object> GetValueAsync(string path)
        {
            DataSnapshot snap = await _root.Child(path).GetValueAsync();
            return snap.Exists ? snap.Value : null;
        }

        public async UniTask RemoveAsync(string path)
            => await _root.Child(path).RemoveValueAsync();

        public DatabaseReference Listen(string path, EventHandler<ValueChangedEventArgs> handler)
        {
            var refNode = _root.Child(path);
            refNode.ValueChanged += handler;
            return refNode;
        }

        public void StopListening(DatabaseReference refNode, EventHandler<ValueChangedEventArgs> handler)
        {
            if (refNode != null) refNode.ValueChanged -= handler;
        }

        public Query OrderByChildDesc(string path, string childKey, int limit)
            => _root.Child(path).OrderByChild(childKey).LimitToLast(limit);
    }
}
