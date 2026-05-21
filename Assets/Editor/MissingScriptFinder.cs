using UnityEditor;
using UnityEngine;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find and Remove Missing Scripts")]
    static void FindAndRemove()
    {
        var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int removed = 0;
        foreach (var go in all)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                Debug.Log($"[MissingScript] '{go.name}'에서 {count}개 제거");
                removed += count;
            }
        }
        Debug.Log($"[MissingScript] 총 {removed}개 제거 완료");
    }
}
