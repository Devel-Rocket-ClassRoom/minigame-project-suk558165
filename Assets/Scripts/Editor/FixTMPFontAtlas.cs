using TMPro;
using UnityEditor;
using UnityEngine;

public static class FixTMPFontAtlas
{
    [MenuItem("Tools/Fix DungGeunMo TMP Font Atlas")]
    static void Fix()
    {
        string[] guids = AssetDatabase.FindAssets("DungGeunMo TMP t:TMP_FontAsset");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[FixTMPFont] DungGeunMo TMP 에셋을 찾지 못했습니다.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
                continue;

            // atlas 텍스처 배열이 비어 있거나 null 원소가 있으면 교체
            bool needsFix =
                font.atlasTextures == null
                || font.atlasTextures.Length == 0
                || font.atlasTextures[0] == null;

            if (!needsFix)
            {
                Debug.Log($"[FixTMPFont] {path} — 이미 정상입니다.");
                continue;
            }

            var tex = new Texture2D(font.atlasWidth, font.atlasHeight, TextureFormat.Alpha8, false)
            {
                name = font.name + " Atlas",
            };
            tex.SetPixels32(new Color32[font.atlasWidth * font.atlasHeight]);
            tex.Apply(false, false);

            // 서브에셋으로 추가
            AssetDatabase.AddObjectToAsset(tex, path);

            // 내부 배열 교체
            font.atlasTextures = new Texture2D[] { tex };
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[FixTMPFont] {path} — Atlas Texture 생성 완료 ({font.atlasWidth}×{font.atlasHeight})"
            );
        }

        AssetDatabase.Refresh();
    }
}
