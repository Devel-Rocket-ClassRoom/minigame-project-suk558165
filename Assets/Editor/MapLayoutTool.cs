using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ASCII 레이아웃을 방 프리팹의 타일맵에 찍는 도구.
///
/// 손으로 타일을 칠하는 대신 텍스트로 지형을 정의한다.
/// 난이도를 수치로 검증할 수 있고(점프 사거리 대비 구덩이 폭), 되돌리기·재현이 쉽다.
///
/// 사용법: MapLayouts 에 레이아웃을 정의하고 Apply(프리팹경로, 레이아웃) 호출.
/// </summary>
public static class MapLayoutTool
{
    // 지형 타일 — 기존 맵에서 쓰던 것과 동일 (Falete main_lev_build)
    const string SurfaceTile = "main_lev_build_1159"; // 위가 뚫린 지면 = 윗면
    const string FillTile = "main_lev_build_1182"; // 사방이 막힌 지면 = 내부
    const string PlatformTile = "main_lev_build_993"; // 통과 가능 발판

    public struct Anchor
    {
        public char Key;
        public Vector2Int Cell;
    }

    /// <summary>
    /// 레이아웃을 적용한다.
    /// rows[0] 이 가장 위쪽 줄이며, originCell 은 rows 의 좌하단이 놓일 셀 좌표.
    /// </summary>
    public static Dictionary<char, Vector2Int> Apply(
        GameObject root,
        string[] rows,
        Vector2Int originCell,
        bool clearFirst = true
    )
    {
        var floor = FindTilemap(root, "Floor");
        var platform = FindTilemap(root, "PlatForm");
        if (floor == null)
        {
            Debug.LogError($"[MapLayoutTool] {root.name}: Floor 타일맵을 찾을 수 없습니다.");
            return null;
        }

        var surface = LoadTile(SurfaceTile);
        var fill = LoadTile(FillTile);
        var plat = LoadTile(PlatformTile);

        int h = rows.Length;
        var solid = new HashSet<Vector2Int>();
        var oneWay = new HashSet<Vector2Int>();
        var anchors = new Dictionary<char, Vector2Int>();

        for (int r = 0; r < h; r++)
        {
            string line = rows[r];
            for (int c = 0; c < line.Length; c++)
            {
                var cell = new Vector2Int(originCell.x + c, originCell.y + (h - 1 - r));
                char ch = line[c];
                if (ch == '#')
                    solid.Add(cell);
                else if (ch == '=')
                    oneWay.Add(cell);
                else if (ch != ' ' && ch != '.')
                    anchors[ch] = cell;
            }
        }

        if (clearFirst)
        {
            floor.ClearAllTiles();
            if (platform != null)
                platform.ClearAllTiles();
        }

        // 위가 뚫려 있으면 윗면 타일, 아니면 내부 타일 — 2타일 오토타일링
        foreach (var cell in solid)
        {
            bool covered = solid.Contains(cell + Vector2Int.up);
            floor.SetTile(new Vector3Int(cell.x, cell.y, 0), covered ? fill : surface);
        }

        if (platform != null)
            foreach (var cell in oneWay)
                platform.SetTile(new Vector3Int(cell.x, cell.y, 0), plat);

        floor.CompressBounds();
        if (platform != null)
            platform.CompressBounds();

        EditorUtility.SetDirty(root);
        return anchors;
    }

    static Tilemap FindTilemap(GameObject root, string name)
    {
        foreach (var tm in root.GetComponentsInChildren<Tilemap>(true))
            if (tm.gameObject.name == name)
                return tm;
        return null;
    }

    static TileBase LoadTile(string tileName)
    {
        var guids = AssetDatabase.FindAssets($"{tileName} t:Tile", new[] { "Assets/Imported" });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != tileName)
                continue;
            return AssetDatabase.LoadAssetAtPath<TileBase>(path);
        }
        Debug.LogError($"[MapLayoutTool] 타일 '{tileName}' 을 찾을 수 없습니다.");
        return null;
    }

    // ── 난이도 검증 ────────────────────────────────────────
    // 플레이어 실측치 (Player.prefab): walkSpeed 6, jumpForce 18, gravityScale 4,
    // fallGravityMultiplier 2.5, 2단 점프, 대쉬 3배속 0.3초
    public const float JumpHeight = 4.13f; // 1단 점프 최고 높이
    public const float JumpRange = 4.49f; // 1단 점프 수평 도달
    public const float DoubleJumpRange = 7.5f; // 2단 점프 수평 도달
    public const float DashRange = 5.4f; // 대쉬 수평 이동(중력 0)
    public const float MaxRange = 9.5f; // 점프 + 대쉬

    /// <summary>레이아웃의 구덩이 폭과 단차를 검사해 넘을 수 없는 곳을 찾는다.</summary>
    public static List<string> Validate(string[] rows)
    {
        int h = rows.Length;
        int w = 0;
        foreach (var r in rows)
            w = Mathf.Max(w, r.Length);

        var issues = new List<string>();
        // 각 열의 가장 높은 발판 높이(없으면 -1)
        var top = new int[w];
        for (int c = 0; c < w; c++)
        {
            top[c] = -1;
            for (int r = 0; r < h; r++)
            {
                if (c >= rows[r].Length)
                    continue;
                char ch = rows[r][c];
                if (ch == '#' || ch == '=')
                {
                    top[c] = h - 1 - r;
                    break;
                }
            }
        }

        // 연속된 빈 열 = 구덩이
        int gapStart = -1;
        for (int c = 0; c <= w; c++)
        {
            bool empty = c < w && top[c] < 0;
            if (empty && gapStart < 0)
                gapStart = c;
            if (!empty && gapStart >= 0)
            {
                int width = c - gapStart;
                if (width > MaxRange)
                    issues.Add($"x={gapStart}~{c - 1}: 구덩이 폭 {width} — 점프+대쉬({MaxRange})로도 못 넘음");
                gapStart = -1;
            }
        }
        return issues;
    }
}
