using UnityEngine;

namespace Game.Firebase
{
    [CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Firebase/Firebase Config")]
    public class FirebaseConfig : ScriptableObject
    {
        public string apiKey;
        public string appId;
        public string projectId;
        public string databaseUrl;
        public string storageBucket;

        public bool IsValid =>
            !string.IsNullOrEmpty(apiKey) &&
            !string.IsNullOrEmpty(appId) &&
            !string.IsNullOrEmpty(projectId);
    }
}
