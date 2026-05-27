using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SetupDashAnimation
{
    private const string DashSpritePath = "Assets/Imported/Player/PlayerSprites/PlayerDash.png";
    private const string DashAnimPath = "Assets/Animations/Player/Player_Dash.anim";

    [MenuItem("Tools/Setup Dash Animation")]
    public static void Setup()
    {
        FixSpriteImportSettings();
        // Defer animation rebuild to next editor frame so reimported sprites are fully loaded
        EditorApplication.delayCall += RebuildAndSave;
    }

    static void RebuildAndSave()
    {
        EditorApplication.delayCall -= RebuildAndSave;
        RebuildDashAnimation();
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupDashAnimation] Done.");
    }

    static void FixSpriteImportSettings()
    {
        var importer = AssetImporter.GetAtPath(DashSpritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError(
                "[SetupDashAnimation] Could not load TextureImporter for " + DashSpritePath
            );
            return;
        }

        // Reference sprites use 100 PPU on a 1024 sheet — keep same logical size by doubling PPU
        bool changed = false;

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 200f))
        {
            importer.spritePixelsPerUnit = 200f;
            changed = true;
        }

        // Slice into a 4×4 grid (16 cells, 15 frames used) if no slices exist
        var sheetMeta = importer.spritesheet;
        if (sheetMeta == null || sheetMeta.Length == 0)
        {
            SliceGrid(importer);
            changed = true;
        }
        else if (sheetMeta.Length < 15)
        {
            Debug.LogWarning(
                $"[SetupDashAnimation] Only {sheetMeta.Length} sprite slices found — re-slicing."
            );
            SliceGrid(importer);
            changed = true;
        }

        // Fix pivot on each slice to match standard (bottom-center = 0.5, 0)
        var sprites = importer.spritesheet;
        bool pivotChanged = false;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i].alignment != (int)SpriteAlignment.BottomCenter)
            {
                sprites[i].alignment = (int)SpriteAlignment.BottomCenter;
                sprites[i].pivot = new Vector2(0.5f, 0f);
                pivotChanged = true;
            }
        }
        if (pivotChanged)
        {
            importer.spritesheet = sprites;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log(
                "[SetupDashAnimation] PlayerDash.png reimported: PPU=200, sprites="
                    + importer.spritesheet.Length
            );
        }
        else
        {
            Debug.Log("[SetupDashAnimation] PlayerDash.png already correct.");
        }
    }

    static void SliceGrid(TextureImporter importer)
    {
        // 2048×2048 → 4 cols × 4 rows = 512×512 per cell, 15 frames (last cell unused)
        const int cols = 4,
            rows = 4,
            w = 512,
            h = 512;
        var slices = new List<SpriteMetaData>();
        int idx = 0;
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                if (idx >= 15)
                    break;
                var smd = new SpriteMetaData
                {
                    name = $"PlayerDash_{idx}",
                    rect = new Rect(col * w, row * h, w, h),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                };
                slices.Add(smd);
                idx++;
            }
        }
        importer.spritesheet = slices.ToArray();
    }

    static void RebuildDashAnimation()
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(DashSpritePath);
        Sprite[] sprites = allAssets.OfType<Sprite>().OrderBy(s => s.name).ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError(
                "[SetupDashAnimation] No sprites found — import may need a manual refresh first."
            );
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DashAnimPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Player_Dash" };
            AssetDatabase.CreateAsset(clip, DashAnimPath);
        }

        clip.ClearCurves();

        var frameRate = 12f;
        clip.frameRate = frameRate;

        var editorCurveBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "Visuals/Body",
            propertyName = "m_Sprite",
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, editorCurveBinding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        Debug.Log(
            $"[SetupDashAnimation] Player_Dash.anim rebuilt with {sprites.Length} frames at {frameRate}fps."
        );
    }
}
