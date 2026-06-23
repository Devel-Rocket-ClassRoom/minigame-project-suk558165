using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// TextMeshProUGUI에 붙이면 지정한 키의 번역된 문자열로 자동 갱신.
/// 언어 변경 시 자동으로 다시 가져온다.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedLabel : MonoBehaviour
{
    [Tooltip("String Table 이름 (기본: Items — UI 전용 테이블이 있으면 그 이름)")]
    public string tableName = "Items";

    [Tooltip("String Table 안의 키")]
    public string entryKey;

    private TextMeshProUGUI _label;

    void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale _) => Refresh();

    void Refresh()
    {
        if (_label == null || string.IsNullOrEmpty(entryKey))
            return;

        // SelectedLocaleChanged 콜백은 ResourceManager.Update 안에서 호출되므로
        // 동기 GetTable() (내부 WaitForCompletion) 호출 시 재진입 예외가 발생한다.
        // GetLocalizedStringAsync + Completed 콜백으로 비동기 처리.
        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, entryKey);
        op.Completed += handle =>
        {
            if (_label != null && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                _label.text = handle.Result;
        };
    }

    /// <summary>코드에서 키를 바꾸고 즉시 갱신.</summary>
    public void SetKey(string key)
    {
        entryKey = key;
        Refresh();
    }
}
