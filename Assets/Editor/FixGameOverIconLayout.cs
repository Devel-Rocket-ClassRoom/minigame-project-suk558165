using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools/Fix GameOver Icon Layout
/// AddGameOverIcons 실행 후 깨진 레이아웃을 수정합니다.
/// - HLG childControlWidth/Height = true
/// - Icon  : flexibleWidth = 0, fixed 36×36
/// - Text  : flexibleWidth = 1 (나머지 공간 채움)
/// </summary>
public class FixGameOverIconLayout
{
    static readonly string[] RowNames =
    {
        "PlayTimeText_Row",
        "DeathCountText_Row",
        "KillCountText_Row",
        "GoldEarnedText_Row",
        "DamageDealtText_Row",
        "DamageTakenText_Row",
        "ItemsGainedText_Row",
    };

    [MenuItem("Tools/Fix GameOver Icon Layout")]
    public static void Fix()
    {
        const string prefabPath = "Assets/Prefabs/UI/GameOverPanel.prefab";

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        foreach (var rowName in RowNames)
        {
            var rowTr = FindDeep(root.transform, rowName);
            if (rowTr == null)
            {
                Debug.LogWarning($"[FixGameOverIconLayout] {rowName} 을 찾을 수 없음");
                continue;
            }

            // ── HorizontalLayoutGroup 수정 ──────────────────────────────────
            var hlg = rowTr.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
                hlg = rowTr.gameObject.AddComponent<HorizontalLayoutGroup>();

            hlg.childControlWidth = true; // HLG 가 자식 너비 관리
            hlg.childControlHeight = true; // HLG 가 자식 높이 관리
            hlg.childForceExpandWidth = false; // flexibleWidth 로만 확장
            hlg.childForceExpandHeight = true; // 세로는 Row 높이에 맞춤
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // ── Icon LayoutElement: 36×36 고정, 늘어나지 않음 ───────────────
            var iconTr = rowTr.Find("Icon");
            if (iconTr != null)
            {
                var le = iconTr.GetComponent<LayoutElement>();
                if (le == null)
                    le = iconTr.gameObject.AddComponent<LayoutElement>();

                le.minWidth = 36f;
                le.preferredWidth = 36f;
                le.flexibleWidth = 0f; // 고정 크기

                le.minHeight = 36f;
                le.preferredHeight = 36f;
                le.flexibleHeight = 0f;

                // Image: Preserve Aspect 활성
                var img = iconTr.GetComponent<Image>();
                if (img != null)
                    img.preserveAspect = true;
            }

            // ── Text LayoutElement: 나머지 공간 모두 채움 ───────────────────
            // 두 번째 자식이 텍스트
            if (rowTr.childCount >= 2)
            {
                var textTr = rowTr.GetChild(1);

                var le = textTr.GetComponent<LayoutElement>();
                if (le == null)
                    le = textTr.gameObject.AddComponent<LayoutElement>();

                le.flexibleWidth = 1f; // 남은 너비 전부 사용
                le.flexibleHeight = 1f;
                le.minHeight = 40f;
                le.preferredHeight = 44f;
            }

            EditorUtility.SetDirty(rowTr.gameObject);
            Debug.Log($"[FixGameOverIconLayout] {rowName} 레이아웃 수정 완료");
        }

        Debug.Log("[FixGameOverIconLayout] 전체 완료");
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            var found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
