using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 임의의 UI 루트에 붙이면 자식의 모든 TextMeshProUGUI를 한국어/영어 원문 사전과
/// 대조해 자동 매핑한 뒤, 언어 변경 시 갱신한다.
/// 매핑 사전은 UILocalizationMap에 중앙 관리 — 새 텍스트는 거기에 한 줄만 추가.
/// </summary>
public class AutoLocalizePanel : MonoBehaviour
{
    [Tooltip("String Table 이름 (기본: Items)")]
    public string tableName = "Items";

    [Tooltip(
        "숫자/수치가 포함된 텍스트는 매핑 사전과 정확히 일치하지 않으면 무시됨. 디버깅 시 활성화하면 매칭 실패 텍스트를 콘솔에 출력"
    )]
    public bool debugLogMisses = false;

    private readonly List<(TextMeshProUGUI text, string key)> _bound = new();
    private bool _initialized;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        StartCoroutine(InitAndRefresh());
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    IEnumerator InitAndRefresh()
    {
        yield return LocalizationSettings.InitializationOperation;
        if (!_initialized)
        {
            Bind();
            _initialized = true;
        }
        Refresh();
    }

    void OnLocaleChanged(Locale _) => StartCoroutine(InitAndRefresh());

    void Bind()
    {
        _bound.Clear();
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            if (t == null)
                continue;
            string current = t.text?.Trim();
            if (string.IsNullOrEmpty(current))
                continue;
            if (UILocalizationMap.TextToKey.TryGetValue(current, out string key))
                _bound.Add((t, key));
            else if (debugLogMisses)
                Debug.Log($"[AutoLocalizePanel] 매칭 실패: \"{current}\" ({t.transform.name})");
        }
    }

    void Refresh()
    {
        var table = LocalizationSettings.StringDatabase?.GetTable(tableName);
        if (table == null)
            return;

        foreach (var (text, key) in _bound)
        {
            if (text == null)
                continue;
            var entry = table.GetEntry(key);
            if (entry != null)
                text.text = entry.GetLocalizedString();
        }
    }
}
