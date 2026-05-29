using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class ItemImporter
{
    const string WEAPON_NUM_CSV = "Assets/Data/CSV/ItemNumericData_Weapon.csv";
    const string ACC_NUM_CSV = "Assets/Data/CSV/ItemNumericData_Accessory.csv";
    const string WEAPON_STR_CSV = "Assets/Data/CSV/ItemStringData_Weapon.csv";
    const string ACC_STR_CSV = "Assets/Data/CSV/ItemStringData_Accessory.csv";
    const string WEAPON_DIR = "Assets/Data/Weapons";
    const string ACC_DIR = "Assets/Data/Accessories";
    const string DB_PATH = "Assets/Data/ItemDatabase.asset";
    const string TABLE_NAME = "Items";

    [MenuItem("Game/아이템 CSV에서 가져오기 _F5")]
    static void ImportAll()
    {
        EnsureDir("Assets/Data");
        EnsureDir(WEAPON_DIR);
        EnsureDir(ACC_DIR);

        var weaponNumRows = ReadCsv(WEAPON_NUM_CSV);
        var accNumRows = ReadCsv(ACC_NUM_CSV);
        var weaponStrRows = ReadCsv(WEAPON_STR_CSV);
        var accStrRows = ReadCsv(ACC_STR_CSV);

        if (weaponNumRows.Count == 0 || accNumRows.Count == 0)
        {
            Debug.LogError(
                "[ItemImporter] CSV 파일을 찾을 수 없습니다. build_items_excel.py 를 먼저 실행하세요."
            );
            return;
        }

        var strTable = EnsureStringTableCollection();
        PopulateStringTable(strTable, weaponStrRows);
        PopulateStringTable(strTable, accStrRows);

        ImportWeapons(weaponNumRows, BuildIdKeyMap(weaponStrRows));
        ImportAccessories(accNumRows, BuildIdKeyMap(accStrRows));
        UpdateDatabase();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemImporter] ✅ 아이템 가져오기 완료!");
    }

    // ── String Table ───────────────────────────────────────────────────────────

    static StringTableCollection EnsureStringTableCollection()
    {
        var all = LocalizationEditorSettings.GetStringTableCollections();
        var col = all.FirstOrDefault(c => c.TableCollectionName == TABLE_NAME);
        if (col != null)
            return col;

        EnsureDir("Assets/Localization");
        var locales = LocalizationEditorSettings.GetLocales();
        col = LocalizationEditorSettings.CreateStringTableCollection(
            TABLE_NAME,
            "Assets/Localization",
            locales
        );
        AssetDatabase.SaveAssets();
        Debug.Log($"[ItemImporter] String Table Collection '{TABLE_NAME}' 생성됨");
        return col;
    }

    // rows[0] = header: [id, Key, 한국어, English]
    static void PopulateStringTable(StringTableCollection col, List<string[]> rows)
    {
        if (col == null || rows.Count < 2)
            return;

        var header = rows[0];
        var localeCols = new Dictionary<int, string>();
        for (int c = 2; c < header.Length; c++)
        {
            string h = header[c].Trim();
            if (h == "한국어" || h == "Korean")
                localeCols[c] = "ko";
            else if (h == "English")
                localeCols[c] = "en";
        }

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Length < 2)
                continue;
            string key = row[1].Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            foreach (var kv in localeCols)
            {
                if (kv.Key >= row.Length)
                    continue;
                SetEntry(col, kv.Value, key, row[kv.Key]);
            }
        }
        EditorUtility.SetDirty(col);
    }

    static void SetEntry(StringTableCollection col, string localeCode, string key, string value)
    {
        foreach (var table in col.StringTables)
        {
            if (table.LocaleIdentifier.Code != localeCode)
                continue;
            var entry = table.GetEntry(key);
            if (entry == null)
                table.AddEntry(key, value);
            else
                entry.Value = value;
            EditorUtility.SetDirty(table);
        }
    }

    // id → (nameKey, descKey)
    static Dictionary<string, (string name, string desc)> BuildIdKeyMap(List<string[]> rows)
    {
        var map = new Dictionary<string, (string name, string desc)>();
        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Length < 2)
                continue;
            string id = row[0].Trim(),
                key = row[1].Trim();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key))
                continue;
            if (!map.ContainsKey(id))
                map[id] = (key, "");
            else
                map[id] = (map[id].name, key);
        }
        return map;
    }

    static LocalizedString Loc(string key) => new LocalizedString(TABLE_NAME, key);

    // ── Weapon Import ──────────────────────────────────────────────────────────

    static void ImportWeapons(
        List<string[]> rows,
        Dictionary<string, (string name, string desc)> idKeys
    )
    {
        if (rows.Count < 2)
            return;
        var header = rows[0];
        int C(string n) => System.Array.IndexOf(header, n);

        int iId = C("id"),
            iType = C("weaponType"),
            iDmg = C("damage"),
            iCool = C("attackCooldown"),
            iAngle = C("swingAngle"),
            iSwing = C("swingDuration"),
            iProj = C("projectileSpeed"),
            iPrice = C("price");

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string id = row[iId].Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            string path = $"{WEAPON_DIR}/{id}.asset";
            var data =
                AssetDatabase.LoadAssetAtPath<WeaponData>(path) ?? CreateAsset<WeaponData>(path);

            data.id = id;
            data.weaponType = row[iType].Trim() == "Ranged" ? WeaponType.Ranged : WeaponType.Melee;
            data.damage = F(row[iDmg]);
            data.attackCooldown = F(row[iCool]);
            data.swingAngle = F(row[iAngle]);
            data.swingDuration = F(row[iSwing]);
            data.projectileSpeed = F(row[iProj]);
            data.price = (int)F(row[iPrice]);

            if (idKeys.TryGetValue(id, out var keys))
            {
                if (!string.IsNullOrEmpty(keys.name))
                    data.weaponName = Loc(keys.name);
                if (!string.IsNullOrEmpty(keys.desc))
                    data.description = Loc(keys.desc);
            }
            EditorUtility.SetDirty(data);
        }
        Debug.Log($"[ItemImporter] 무기 {rows.Count - 1}개 처리 완료");
    }

    // ── Accessory Import ───────────────────────────────────────────────────────

    static void ImportAccessories(
        List<string[]> rows,
        Dictionary<string, (string name, string desc)> idKeys
    )
    {
        if (rows.Count < 2)
            return;
        var header = rows[0];
        int C(string n) => System.Array.IndexOf(header, n);

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string id = row[C("id")].Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            string path = $"{ACC_DIR}/{id}.asset";
            var data =
                AssetDatabase.LoadAssetAtPath<AccessoryData>(path)
                ?? CreateAsset<AccessoryData>(path);

            data.id = id;
            data.price = (int)F(row[C("price")]);
            data.maxHpBonus = F(row[C("maxHpBonus")]);
            data.damageBonus = F(row[C("damageBonus")]);
            data.speedBonus = F(row[C("speedBonus")]);
            data.jumpBonus = F(row[C("jumpBonus")]);
            data.criticalChance = F(row[C("criticalChance")]);
            data.criticalDamage = F(row[C("criticalDamage")]);
            data.attackSpeedBonus = F(row[C("attackSpeedBonus")]);
            data.damageReduction = F(row[C("damageReduction")]);
            data.damageReceivedMult = F(row[C("damageReceivedMult")]);
            data.damageDealtMult = F(row[C("damageDealtMult")]);
            data.dashCountBonus = (int)F(row[C("dashCountBonus")]);
            data.dashRangeBonus = F(row[C("dashRangeBonus")]);
            data.evasionRate = F(row[C("evasionRate")]);
            data.goldDropBonus = F(row[C("goldDropBonus")]);
            data.arrowCount = (int)F(row[C("arrowCount")]);
            data.arrowDamageMult = F(row[C("arrowDamageMult")]);
            data.penetrationCount = (int)F(row[C("penetrationCount")]);

            if (idKeys.TryGetValue(id, out var keys))
            {
                if (!string.IsNullOrEmpty(keys.name))
                    data.accessoryName = Loc(keys.name);
                if (!string.IsNullOrEmpty(keys.desc))
                    data.description = Loc(keys.desc);
            }
            EditorUtility.SetDirty(data);
        }
        Debug.Log($"[ItemImporter] 악세서리 {rows.Count - 1}개 처리 완료");
    }

    // ── ItemDatabase ───────────────────────────────────────────────────────────

    static void UpdateDatabase()
    {
        var db =
            AssetDatabase.LoadAssetAtPath<ItemDatabase>(DB_PATH)
            ?? CreateAsset<ItemDatabase>(DB_PATH);

        db.weapons.Clear();
        db.accessories.Clear();

        foreach (var guid in AssetDatabase.FindAssets("t:WeaponData", new[] { WEAPON_DIR }))
            db.weapons.Add(
                AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid))
            );

        foreach (var guid in AssetDatabase.FindAssets("t:AccessoryData", new[] { ACC_DIR }))
            db.accessories.Add(
                AssetDatabase.LoadAssetAtPath<AccessoryData>(AssetDatabase.GUIDToAssetPath(guid))
            );

        EditorUtility.SetDirty(db);
        Debug.Log(
            $"[ItemImporter] ItemDatabase: 무기 {db.weapons.Count}개, 악세서리 {db.accessories.Count}개"
        );
    }

    // ── 유틸 ───────────────────────────────────────────────────────────────────

    static float F(string s) =>
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? v
            : 0f;

    static T CreateAsset<T>(string path)
        where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureDir(string unityPath)
    {
        if (AssetDatabase.IsValidFolder(unityPath))
            return;
        string parent = Path.GetDirectoryName(unityPath)?.Replace('\\', '/') ?? "Assets";
        string folder = Path.GetFileName(unityPath);
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    // ── CSV 파서 ───────────────────────────────────────────────────────────────

    static List<string[]> ReadCsv(string assetPath)
    {
        var rows = new List<string[]>();
        string full = Path.GetFullPath(assetPath);
        if (!File.Exists(full))
        {
            Debug.LogError($"[ItemImporter] 파일 없음: {assetPath}");
            return rows;
        }
        foreach (var line in File.ReadAllLines(full, new UTF8Encoding(true)))
            if (!string.IsNullOrWhiteSpace(line))
                rows.Add(ParseLine(line));
        return rows;
    }

    static string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQ = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                    inQ = !inQ;
            }
            else if (c == ',' && !inQ)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
                sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields.ToArray();
    }
}
