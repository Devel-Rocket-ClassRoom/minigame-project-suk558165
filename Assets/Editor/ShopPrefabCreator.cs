using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ShopPrefabCreator
{
    // ── 메뉴 1 : ShopPanel UI 프리팹 생성 ───────────────────────────────────
    [MenuItem("Game/상점 패널 생성 (ShopPanel)")]
    static void CreateAll()
    {
        EnsureDir("Assets/Prefabs/UI");
        CreateShopPanel();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ShopPrefabCreator] ShopPanel.prefab 생성 완료");
    }

    // ── 메뉴 2 : Project 창에서 선택한 NPC 프리팹에 Shop 추가 ────────────────
    [MenuItem("Game/선택한 NPC에 상점 추가")]
    static void AddShopToSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("[Shop] Project 창에서 NPC 프리팹을 먼저 선택하세요.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
        {
            Debug.LogError("[Shop] 선택한 오브젝트가 프리팹이 아닙니다.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var npc = scope.prefabContentsRoot;
            AddShopComponents(npc);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Shop] '{selected.name}' 에 상점 컴포넌트 추가 완료 → {path}");
    }

    [MenuItem("Game/선택한 NPC에 상점 추가", true)]
    static bool ValidateAddShop() => Selection.activeGameObject != null;

    static void AddShopComponents(GameObject npc)
    {
        // CircleCollider2D (Trigger) — 없으면 추가
        if (npc.GetComponent<CircleCollider2D>() == null)
        {
            var col = npc.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.5f;
        }
        else
        {
            npc.GetComponent<CircleCollider2D>().isTrigger = true;
        }

        // InteractPrompt — 이미 있으면 건너뜀
        var existingPrompt = npc.transform.Find("InteractPrompt");
        GameObject prompt;
        if (existingPrompt == null)
        {
            prompt = new GameObject("InteractPrompt");
            prompt.transform.SetParent(npc.transform, false);
            prompt.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            var canvas = prompt.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            prompt.AddComponent<CanvasScaler>();

            var rt = prompt.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 60f);
            rt.localScale = Vector3.one * 0.01f;

            var label = new GameObject("Label");
            label.transform.SetParent(prompt.transform, false);
            var lRT = label.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var tmp = label.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "[A] 상점";
            tmp.fontSize = 28;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.yellow;

            prompt.SetActive(false);
        }
        else
        {
            prompt = existingPrompt.gameObject;
        }

        // Shop 컴포넌트 — 없으면 추가
        var shop = npc.GetComponent<Shop>() ?? npc.AddComponent<Shop>();
        var so = new SerializedObject(shop);
        so.FindProperty("interactPrompt").objectReferenceValue = prompt;
        so.ApplyModifiedProperties();
    }

    // ── ShopPanel ────────────────────────────────────────────────────────────

    static void CreateShopPanel()
    {
        var root = new GameObject("ShopPanel");
        root.AddComponent<RectTransform>();
        var shopUI = root.AddComponent<ShopUI>();

        // Frame
        var frame = MakePanel(root, "Frame", new Color(0.08f, 0.08f, 0.12f, 0.97f));
        var fRT = frame.GetComponent<RectTransform>();
        fRT.anchorMin = fRT.anchorMax = new Vector2(0.5f, 0.5f);
        fRT.sizeDelta = new Vector2(700, 480);
        fRT.anchoredPosition = Vector2.zero;

        // 타이틀
        var title = MakeText(frame, "TitleText", "상점", 30, Color.white);
        Anchor(title, 0, 1, 1, 1, 10, -60, -10, 0);

        // 골드
        var gold = MakeText(frame, "GoldText", "골드: 0", 22, new Color(1f, 0.85f, 0.2f));
        Anchor(gold, 0, 1, 1, 1, 10, -100, -10, -60);

        // 구분선
        var sep = new GameObject("Separator");
        sep.transform.SetParent(frame.transform, false);
        var sepRT = sep.AddComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0, 1);
        sepRT.anchorMax = new Vector2(1, 1);
        sepRT.anchoredPosition = new Vector2(0, -102);
        sepRT.sizeDelta = new Vector2(-20, 2);
        sep.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);

        // 슬롯 그리드
        var grid = new GameObject("SlotsGrid");
        grid.transform.SetParent(frame.transform, false);
        var gRT = grid.AddComponent<RectTransform>();
        gRT.anchorMin = Vector2.zero;
        gRT.anchorMax = Vector2.one;
        gRT.offsetMin = new Vector2(15, 65);
        gRT.offsetMax = new Vector2(-15, -108);
        var hlg = grid.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        var slotList = new List<ShopSlotUI>();
        for (int i = 0; i < 4; i++)
            slotList.Add(MakeSlot(grid, i));

        // 닫기 버튼
        var close = MakeButton(frame, "CloseButton", "닫기  [A]", new Color(0.55f, 0.18f, 0.18f));
        var cRT = close.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0);
        cRT.anchoredPosition = new Vector2(0, 32);
        cRT.sizeDelta = new Vector2(160, 46);

        frame.SetActive(false);

        // 필드 연결
        var so = new SerializedObject(shopUI);
        so.FindProperty("frame").objectReferenceValue = frame;
        so.FindProperty("goldText").objectReferenceValue = gold.GetComponent<TextMeshProUGUI>();
        so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<Button>();
        var arr = so.FindProperty("slots");
        arr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = slotList[i];
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/ShopPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("ShopPanel.prefab 저장됨");
    }

    // ── ShopNPC ──────────────────────────────────────────────────────────────

    static void CreateShopNPC()
    {
        var root = new GameObject("ShopNPC");

        // SpriteRenderer
        root.AddComponent<SpriteRenderer>();

        // Trigger Collider
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 2.5f;

        // Interact Prompt (WorldSpace Canvas)
        var promptCanvas = new GameObject("InteractPrompt");
        promptCanvas.transform.SetParent(root.transform, false);
        promptCanvas.transform.localPosition = new Vector3(0, 1.8f, 0);
        var canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var cRT = promptCanvas.GetComponent<RectTransform>();
        cRT.sizeDelta = new Vector2(2.5f, 0.6f);
        cRT.localScale = Vector3.one * 0.01f;
        promptCanvas.AddComponent<CanvasScaler>();

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(promptCanvas.transform, false);
        var tRT = textGo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "[A] 상점";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        promptCanvas.SetActive(false);

        // Shop 컴포넌트
        var shop = root.AddComponent<Shop>();
        var so = new SerializedObject(shop);
        so.FindProperty("interactPrompt").objectReferenceValue = promptCanvas;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/NPC/ShopNPC.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("ShopNPC.prefab 저장됨");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MakeText(GameObject parent, string name, string text, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return go;
    }

    // offsetMin = (left, bottom), offsetMax = (right, top) — top/bottom relative to anchorMax
    static void Anchor(
        GameObject go,
        float ax,
        float ay,
        float bx,
        float by,
        float left,
        float bottom,
        float right,
        float top
    )
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();
        var t = MakeText(go, "Text", label, 18, Color.white);
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static ShopSlotUI MakeSlot(GameObject parent, int index)
    {
        var slot = new GameObject("Slot_" + index);
        slot.transform.SetParent(parent.transform, false);
        slot.AddComponent<RectTransform>();
        slot.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);
        var slotUI = slot.AddComponent<ShopSlotUI>();

        // 아이콘
        var icon = new GameObject("Icon");
        icon.transform.SetParent(slot.transform, false);
        var iRT = icon.AddComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.1f, 0.45f);
        iRT.anchorMax = new Vector2(0.9f, 0.95f);
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.preserveAspect = true;

        // 이름
        var nameGo = MakeText(slot, "NameText", "", 13, Color.white);
        var nRT = nameGo.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0.32f);
        nRT.anchorMax = new Vector2(1, 0.46f);
        nRT.offsetMin = new Vector2(4, 0);
        nRT.offsetMax = new Vector2(-4, 0);

        // 가격
        var priceGo = MakeText(slot, "PriceText", "", 13, new Color(1f, 0.85f, 0.2f));
        var pRT = priceGo.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0, 0.19f);
        pRT.anchorMax = new Vector2(1, 0.33f);
        pRT.offsetMin = new Vector2(4, 0);
        pRT.offsetMax = new Vector2(-4, 0);

        // 구매 버튼
        var btn = MakeButton(slot, "BuyButton", "구매", new Color(0.15f, 0.45f, 0.75f));
        var bRT = btn.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.08f, 0.03f);
        bRT.anchorMax = new Vector2(0.92f, 0.18f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        // 매진 오버레이
        var sold = new GameObject("SoldOutOverlay");
        sold.transform.SetParent(slot.transform, false);
        var sRT = sold.AddComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        sold.AddComponent<Image>().color = new Color(0, 0, 0, 0.72f);
        var soldTxt = MakeText(sold, "Text", "매진", 26, new Color(1f, 0.3f, 0.3f));
        var stRT = soldTxt.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.offsetMin = stRT.offsetMax = Vector2.zero;
        sold.SetActive(false);

        var so = new SerializedObject(slotUI);
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.FindProperty("nameText").objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("priceText").objectReferenceValue = priceGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("buyButton").objectReferenceValue = btn.GetComponent<Button>();
        so.FindProperty("soldOutOverlay").objectReferenceValue = sold;
        so.ApplyModifiedProperties();

        slot.SetActive(false);
        return slotUI;
    }

    static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
