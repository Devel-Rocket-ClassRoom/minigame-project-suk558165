using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class PauseMenuSetup
{
    // ── 색상 팔레트 ───────────────────────────────────────────────────
    static readonly Color ColOverlay = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color ColPanel = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    static readonly Color ColBtn = new Color(0.18f, 0.20f, 0.30f, 1f);
    static readonly Color ColBtnHover = new Color(0.28f, 0.32f, 0.48f, 1f);
    static readonly Color ColBtnPress = new Color(0.10f, 0.12f, 0.20f, 1f);
    static readonly Color ColSliderBG = new Color(0.15f, 0.15f, 0.22f, 1f);
    static readonly Color ColSliderFill = new Color(0.35f, 0.65f, 1.00f, 1f);
    static readonly Color ColText = Color.white;

    [MenuItem("Tools/Create Pause Menu Prefab")]
    public static void Run()
    {
        // 저장 폴더 확인
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        var root = Build();

        const string path = "Assets/Prefabs/UI/PauseMenuCanvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "완료",
            $"프리팹 생성 완료!\n{path}\n\nUICanvas 프리팹 안에 추가하거나 씬에 배치하세요.",
            "확인"
        );
        Debug.Log("[PauseMenuSetup] " + path);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  계층 구조 생성
    // ═══════════════════════════════════════════════════════════════════

    static GameObject Build()
    {
        // ── Canvas 루트 ────────────────────────────────────────────────
        var root = new GameObject("PauseMenuCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // ── 풀스크린 컨테이너 ──────────────────────────────────────────
        var container = FullscreenRect("PauseMenuRoot", root.transform);
        var pauseMenu = container.AddComponent<PauseMenu>();

        // ── 어두운 오버레이 ────────────────────────────────────────────
        var overlay = FullscreenRect("Overlay", container.transform);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = ColOverlay;
        overlayImg.raycastTarget = true;

        // ── Pause Panel ────────────────────────────────────────────────
        var pausePanelGO = MakePanel("PausePanel", container.transform, new Vector2(420, 460));
        var pauseCG = pausePanelGO.AddComponent<CanvasGroup>();

        MakeTitle(pausePanelGO.transform, "일시정지", 36f);

        var btnGroup = VGroup(
            "Buttons",
            pausePanelGO.transform,
            new Vector2(340, 0),
            new Vector2(0, -30f),
            14
        );
        var resumeBtn = MakeButton("계속하기", btnGroup.transform);
        var optionsBtn = MakeButton("옵션", btnGroup.transform);
        var quitBtn = MakeButton("종료", btnGroup.transform);

        // ── Options Panel ──────────────────────────────────────────────
        var optsPanelGO = MakePanel("OptionsPanel", container.transform, new Vector2(480, 520));
        var optsCG = optsPanelGO.AddComponent<CanvasGroup>();
        var optsMenu = optsPanelGO.AddComponent<OptionsMenu>();

        MakeTitle(optsPanelGO.transform, "옵션", 32f);

        var sliderGroup = VGroup(
            "Sliders",
            optsPanelGO.transform,
            new Vector2(420, 0),
            new Vector2(0, -20f),
            18
        );
        var masterSlider = MakeSliderRow("마스터 음량", sliderGroup.transform);
        var bgmSlider = MakeSliderRow("BGM 음량", sliderGroup.transform);
        var sfxSlider = MakeSliderRow("효과음", sliderGroup.transform);

        // 여백
        Spacer(sliderGroup.transform, 10f);
        var backBtn = MakeButton("뒤로", sliderGroup.transform);

        // ── 레퍼런스 연결 ──────────────────────────────────────────────
        pauseMenu.pausePanel = pauseCG;
        pauseMenu.optionsPanel = optsCG;

        optsMenu.masterVolumeSlider = masterSlider;
        optsMenu.bgmVolumeSlider = bgmSlider;
        optsMenu.sfxVolumeSlider = sfxSlider;

        // ── 버튼 이벤트 연결 ───────────────────────────────────────────
        Bind(resumeBtn, pauseMenu.OnResumeButton);
        Bind(optionsBtn, pauseMenu.OnOptionsButton);
        Bind(quitBtn, pauseMenu.OnQuitButton);
        Bind(backBtn, optsMenu.OnBackButton);

        BindSlider(masterSlider, optsMenu.OnMasterVolumeChanged);
        BindSlider(bgmSlider, optsMenu.OnBGMVolumeChanged);
        BindSlider(sfxSlider, optsMenu.OnSFXVolumeChanged);

        return root;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UI 빌더 헬퍼
    // ═══════════════════════════════════════════════════════════════════

    static GameObject FullscreenRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject MakePanel(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = ColPanel;
        img.raycastTarget = true;
        return go;
    }

    static void MakeTitle(Transform parent, string text, float fontSize)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0, -80f);
        rt.offsetMax = new Vector2(0, -16f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = ColText;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // 수직 레이아웃 그룹 컨테이너
    static GameObject VGroup(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 offset,
        float spacing
    )
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    static Button MakeButton(string label, Transform parent)
    {
        var go = new GameObject(label + "_Btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 58);

        var img = go.AddComponent<Image>();
        img.color = ColBtn;

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = ColBtn;
        c.highlightedColor = ColBtnHover;
        c.pressedColor = ColBtnPress;
        c.selectedColor = ColBtn;
        c.fadeDuration = 0.08f;
        btn.colors = c;

        go.AddComponent<ButtonScaleEffect>();

        // 텍스트
        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var tmp = tgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.color = ColText;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static Slider MakeSliderRow(string label, Transform parent)
    {
        // 행 컨테이너
        var row = new GameObject(label + "_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 48);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(8, 8, 0, 0);

        // 레이블
        var lgo = new GameObject("Label", typeof(RectTransform));
        lgo.transform.SetParent(row.transform, false);
        var lle = lgo.AddComponent<LayoutElement>();
        lle.preferredWidth = 110;

        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f;
        tmp.color = ColText;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        // 슬라이더 오브젝트
        var sgo = new GameObject("Slider", typeof(RectTransform));
        sgo.transform.SetParent(row.transform, false);
        var sle = sgo.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1;

        var slider = sgo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(sgo.transform, false);
        SetStretch(bg.GetComponent<RectTransform>(), new Vector2(0, 0.3f), new Vector2(1, 0.7f));
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = ColSliderBG;

        // Fill Area
        var fa = new GameObject("Fill Area", typeof(RectTransform));
        fa.transform.SetParent(sgo.transform, false);
        var faRT = fa.GetComponent<RectTransform>();
        SetStretch(faRT, new Vector2(0, 0.3f), new Vector2(1, 0.7f));
        faRT.offsetMin = new Vector2(5, 0);
        faRT.offsetMax = new Vector2(-15, 0);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fa.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1); // Slider가 제어
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = ColSliderFill;
        slider.fillRect = fillRT;

        // Handle Slide Area
        var ha = new GameObject("Handle Slide Area", typeof(RectTransform));
        ha.transform.SetParent(sgo.transform, false);
        var haRT = ha.GetComponent<RectTransform>();
        SetStretch(haRT, Vector2.zero, Vector2.one);
        haRT.offsetMin = new Vector2(10, 0);
        haRT.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(ha.transform, false);
        var hRT = handle.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 0.5f);
        hRT.anchorMax = new Vector2(0, 0.5f);
        hRT.pivot = new Vector2(0.5f, 0.5f);
        hRT.sizeDelta = new Vector2(22, 22);
        var hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;

        slider.targetGraphic = hImg;
        slider.handleRect = hRT;

        return slider;
    }

    static void Spacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    static void SetStretch(RectTransform rt, Vector2 ancMin, Vector2 ancMax)
    {
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Bind(Button btn, UnityAction action) =>
        UnityEventTools.AddPersistentListener(btn.onClick, action);

    static void BindSlider(Slider slider, UnityAction<float> action) =>
        UnityEventTools.AddPersistentListener(slider.onValueChanged, action);
}
