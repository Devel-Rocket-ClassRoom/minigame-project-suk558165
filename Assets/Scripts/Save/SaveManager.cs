using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    public const int CurrentVersion = 1;

    public SaveData Data { get; private set; }

    // ── 암호화 키 (32바이트 = AES-256) ──
    // 실제 배포 시 난독화 또는 키 관리 방식 변경 권장
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("MiniGame_SaveKey_2024!__32Bytes!");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("MG_InitVec_16B!!");

    // 구버전 호환: IV가 15바이트였던 시절의 세이브 복호화용
    private static readonly byte[] LegacyIV = Encoding.UTF8.GetBytes("MG_InitVec_16B!");

    private string FilePath => Path.Combine(Application.persistentDataPath, "save.dat");
    private string TempPath => FilePath + ".tmp";
    private string BackupPath => FilePath + ".bak";

    private bool isDirty;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void OnApplicationQuit() => Flush();

    void OnApplicationPause(bool paused)
    {
        if (paused)
            Flush();
    }

    /// <summary>
    /// 저장 예약. 골드 획득처럼 자주 일어나는 변경은 이걸 쓴다.
    /// 실제 디스크 쓰기는 Flush() 시점(방 전환·씬 이탈·종료)에 1회만 일어난다.
    /// </summary>
    public void MarkDirty() => isDirty = true;

    /// <summary>예약된 변경이 있을 때만 실제로 기록한다.</summary>
    public void Flush()
    {
        if (isDirty)
            Save();
    }

    // ── 저장 ──
    // 원자적 쓰기: 임시 파일에 먼저 쓰고 교체한다.
    // 곧바로 덮어쓰면 쓰는 도중 강제 종료 시 세이브가 통째로 깨지고,
    // 암호화되어 있어 수동 복구도 불가능하다.
    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, false);
            byte[] encrypted = Encrypt(json);

            File.WriteAllBytes(TempPath, encrypted);

            if (File.Exists(FilePath))
                File.Replace(TempPath, FilePath, BackupPath);
            else
                File.Move(TempPath, FilePath);

            isDirty = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    // ── 불러오기 ──
    public void Load()
    {
        // 정상 파일 → 직전 백업 순으로 시도.
        // 쓰기 도중 종료돼 본 파일이 깨졌더라도 한 판 전 상태로는 복구된다.
        if (TryLoadFrom(FilePath) || TryLoadFrom(BackupPath))
            return;

        Data = new SaveData();
        ApplyPlayerPrefsVolume();
    }

    bool TryLoadFrom(string path)
    {
        if (!File.Exists(path))
            return false;

        byte[] encrypted;
        try
        {
            encrypted = File.ReadAllBytes(path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] 읽기 실패 ({path}): {e.Message}");
            return false;
        }

        // 현재 IV → 구버전(15바이트 IV) 순으로 복호화 시도
        if (TryDecrypt(encrypted, IV, out string json) && TryParse(json))
        {
            Migrate();
            return true;
        }

        if (TryDecrypt(encrypted, LegacyIV, out json) && TryParse(json))
        {
            Save(); // 새 IV로 덮어쓰기
            Migrate();
            return true;
        }

        return false;
    }

    /// <summary>복호화는 됐지만 내용이 세이브 형식이 아닐 수 있으므로 파싱 결과를 검증한다.</summary>
    bool TryParse(string json)
    {
        try
        {
            var parsed = JsonUtility.FromJson<SaveData>(json);
            if (parsed == null)
                return false;
            Data = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── 세이브 초기화 ──
    public void DeleteSave()
    {
        // 사용자 설정은 보존
        float master = Data.volumeMaster;
        float bgm = Data.volumeBGM;
        float sfx = Data.volumeSFX;
        string lang = Data.languageCode;
        KeyBindingSaveData keys = Data.keyBindings;
        int resW = Data.resolutionWidth;
        int resH = Data.resolutionHeight;
        int refresh = Data.refreshRate;
        int fullscreen = Data.fullscreenMode;

        if (File.Exists(FilePath))
            File.Delete(FilePath);

        Data = new SaveData();
        Data.volumeMaster = master;
        Data.volumeBGM = bgm;
        Data.volumeSFX = sfx;
        Data.languageCode = lang;
        Data.keyBindings = keys;
        Data.resolutionWidth = resW;
        Data.resolutionHeight = resH;
        Data.refreshRate = refresh;
        Data.fullscreenMode = fullscreen;
    }

    void ApplyPlayerPrefsVolume()
    {
        if (PlayerPrefs.HasKey("Vol_Master"))
            Data.volumeMaster = PlayerPrefs.GetFloat("Vol_Master");
        if (PlayerPrefs.HasKey("Vol_BGM"))
            Data.volumeBGM = PlayerPrefs.GetFloat("Vol_BGM");
        if (PlayerPrefs.HasKey("Vol_SFX"))
            Data.volumeSFX = PlayerPrefs.GetFloat("Vol_SFX");
    }

    // ── 마이그레이션 ──
    void Migrate()
    {
        if (Data.version >= CurrentVersion)
            return;

        Data.version = CurrentVersion;
        Save();
    }

    // ── AES-256 암호화 ──
    bool TryDecrypt(byte[] cipherBytes, byte[] iv, out string result)
    {
        try
        {
            result = DecryptWith(cipherBytes, iv);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            cs.Write(bytes, 0, bytes.Length);
        }
        return ms.ToArray();
    }

    string DecryptWith(byte[] cipherBytes, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
