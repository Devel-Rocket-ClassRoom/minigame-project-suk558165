#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Tools > 버튼 클릭음 자동 연결
// 모든 프리팹에서 ButtonScaleEffect를 찾아 button.mp3를 clickSound에 연결합니다.
public static class ButtonSoundSetup
{
    const string SoundGuid = "fcdca50249c5f43498993a3ce819366e"; // button.mp3

    [MenuItem("Tools/버튼 클릭음 자동 연결")]
    static void Run()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            AssetDatabase.GUIDToAssetPath(SoundGuid)
        );
        if (clip == null)
        {
            Debug.LogError("[ButtonSoundSetup] button.mp3를 찾을 수 없습니다.");
            return;
        }

        int total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;

            foreach (var effect in root.GetComponentsInChildren<ButtonScaleEffect>(true))
            {
                var so = new SerializedObject(effect);
                var prop = so.FindProperty("clickSound");
                if (prop == null)
                    continue;

                if (prop.objectReferenceValue == clip)
                    continue;

                prop.objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                total++;
                Debug.Log($"[ButtonSoundSetup] 연결: {path} → {effect.gameObject.name}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ButtonSoundSetup] 완료 — {total}개 ButtonScaleEffect에 클릭음 연결.");
    }
}
#endif
