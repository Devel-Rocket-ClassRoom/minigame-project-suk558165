using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        InitAndRefresh().Forget();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    async UniTaskVoid InitAndRefresh()
    {
        await LocalizationSettings.InitializationOperation;
        if (!_initialized)
        {
            Bind();
            _initialized = true;
        }
        // SelectedLocaleChanged 콜백 안에서 InitializationOperation이 이미 완료된 경우
        // continuation이 동기 실행되어 ResourceManager.Update에 재진입하게 된다.
        // 한 프레임 양보해서 콜백 밖으로 빠진 뒤 비동기 테이블 로드로 갱신.
        await UniTask.Yield();
        await RefreshAsync();
    }

    void OnLocaleChanged(Locale _) => InitAndRefresh().Forget();

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

    async UniTask RefreshAsync()
    {
        var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        var table = await tableOp;
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
