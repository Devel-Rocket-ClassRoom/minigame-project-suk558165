using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Fix SwordEnemy Attack PPU
///
/// SwordAttck.png 의 PPU 를 SwordEnemy_Walk.png 기준으로 재계산합니다.
/// Walk 프레임 = 256px, Attack 캐릭터 실측 = 140px
/// attackPPU = walkPPU * (140f / 256f)
/// </summary>
public class FixSwordEnemyAttackPPU
{
    const string WalkPath = "Assets/Imported/EnemySprites/SwordEnemy/SwordEnemy_Walk.png";
    const string AttackPath = "Assets/Imported/EnemySprites/SwordEnemy/SwordAttck.png";
    const string AnimPath = "Assets/Animations/Enemy/SwordEnemy/SwordEnemy_Attack.anim";

    // 실측값 (Python PIL 분석 결과)
    const float WalkFrameH = 256f; // Walk 프레임 높이(px)
    const float WalkCharH = 256f; // Walk 캐릭터 실제 높이(px) — 프레임 전체 사용
    const float AttackFrameH = 146f; // Attack 프레임 높이(px)   (584/4)
    const float AttackFrameW = 106f; // Attack 프레임 너비(px)   (424/4, 마지막 3px 무시)
    const float AttackCharH = 140f; // Attack 캐릭터 실제 높이(px)

    [MenuItem("Tools/Fix SwordEnemy Attack PPU")]
    public static void Fix()
    {
        // ── 1. Walk PPU 읽기 ───────────────────────────────────────────────
        var walkImporter = AssetImporter.GetAtPath(WalkPath) as TextureImporter;
        if (walkImporter == null)
        {
            Debug.LogError($"[Fix] Walk importer 없음: {WalkPath}");
            return;
        }

        // spritesheet 의 PPU 는 TextureImporter.spritePixelsPerUnit
        float walkPPU = walkImporter.spritePixelsPerUnit;
        Debug.Log($"[Fix] Walk PPU = {walkPPU}");

        // ── 2. Attack PPU 계산 ─────────────────────────────────────────────
        // 캐릭터 실제 높이 기준으로 월드 높이 일치
        float attackPPU = walkPPU * (AttackCharH / WalkCharH);
        Debug.Log($"[Fix] Attack PPU 계산값 = {attackPPU:F2}");

        // ── 3. SwordAttck.png 재슬라이싱 + 피벗 + PPU 적용 ─────────────────
        var atkImporter = AssetImporter.GetAtPath(AttackPath) as TextureImporter;
        if (atkImporter == null)
        {
            Debug.LogError($"[Fix] Attack importer 없음: {AttackPath}");
            return;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AttackPath);
        int cols = 4,
            rows = 4;
        int fw = Mathf.RoundToInt(AttackFrameW); // 106
        int fh = Mathf.RoundToInt(AttackFrameH); // 146

        var metas = new SpriteMetaData[cols * rows];
        string baseName = "SwordAttck";

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                int unityRow = (rows - 1) - row; // Unity Y는 아래→위
                metas[idx] = new SpriteMetaData
                {
                    name = $"{baseName}_{idx}",
                    rect = new Rect(col * fw, unityRow * fh, fw, fh),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f), // Bottom Center
                };
            }
        }

        atkImporter.spriteImportMode = SpriteImportMode.Multiple;
        atkImporter.spritePixelsPerUnit = attackPPU;
        atkImporter.spritesheet = metas;
        atkImporter.SaveAndReimport();
        Debug.Log($"[Fix] SwordAttck.png 슬라이싱 완료 — PPU={attackPPU:F2}, 4×4=16 프레임");

        // ── 4. SwordEnemy_Attack.anim 재연결 ──────────────────────────────
        var sprites = AssetDatabase
            .LoadAllAssetsAtPath(AttackPath)
            .OfType<Sprite>()
            .OrderBy(s => ExtractIndex(s.name))
            .ThenBy(s => s.name)
            .ToList();

        if (sprites.Count == 0)
        {
            Debug.LogError("[Fix] 슬라이싱 후 스프라이트를 찾을 수 없음");
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "SwordEnemy_Attack" };
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AnimPath));
            AssetDatabase.CreateAsset(clip, AnimPath);
        }

        var existingEvents = AnimationUtility.GetAnimationEvents(clip);
        clip.ClearCurves();

        float sampleRate = clip.frameRate > 0 ? clip.frameRate : 8f;
        float dt = 1f / sampleRate;

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite",
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i * dt, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.stopTime = sprites.Count * dt;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (existingEvents?.Length > 0)
            AnimationUtility.SetAnimationEvents(clip, existingEvents);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[Fix] SwordEnemy_Attack.anim 재연결 완료 — {sprites.Count}프레임, walkPPU={walkPPU}, attackPPU={attackPPU:F2}"
        );
    }

    static int ExtractIndex(string name)
    {
        int under = name.LastIndexOf('_');
        if (under >= 0 && int.TryParse(name.Substring(under + 1), out int v))
            return v;
        return 0;
    }
}
