using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Firebase;

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

        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance.OnSignedIn += _ => SyncFromCloudAsync().Forget();
    }

    // ── 저장 ──
    public void Save()
    {
        Data.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string json = JsonUtility.ToJson(Data, false);
        byte[] encrypted = Encrypt(json);
        File.WriteAllBytes(FilePath, encrypted);

        UploadToCloudAsync(json).Forget();
    }

    // ── 클라우드 업로드 ──
    async UniTaskVoid UploadToCloudAsync(string json)
    {
        var auth = FirebaseAuthManager.Instance;
        var db = FirebaseDBManager.Instance;
        if (auth == null || !auth.IsLoggedIn || db == null || !db.IsReady) return;

        try
        {
            await db.SetJsonAsync($"users/{auth.UserId}/save", json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 클라우드 업로드 실패: {e.Message}");
        }
    }

    // ── 로그인 시 클라우드에서 가져오기 (최신본 사용) ──
    async UniTaskVoid SyncFromCloudAsync()
    {
        var auth = FirebaseAuthManager.Instance;
        var db = FirebaseDBManager.Instance;
        if (auth == null || !auth.IsLoggedIn || db == null) return;

        await UniTask.WaitUntil(() => db.IsReady);

        try
        {
            string cloudJson = await db.GetJsonAsync($"users/{auth.UserId}/save");
            if (string.IsNullOrEmpty(cloudJson))
            {
                UploadToCloudAsync(JsonUtility.ToJson(Data, false)).Forget();
                return;
            }

            var cloud = JsonUtility.FromJson<SaveData>(cloudJson);
            if (cloud.updatedAt > Data.updatedAt)
            {
                PreserveDeviceSettings(cloud, Data);
                Data = cloud;
                string json = JsonUtility.ToJson(Data, false);
                byte[] encrypted = Encrypt(json);
                File.WriteAllBytes(FilePath, encrypted);
                Debug.Log("[SaveManager] 클라우드 세이브로 동기화됨");
            }
            else if (Data.updatedAt > cloud.updatedAt)
            {
                UploadToCloudAsync(JsonUtility.ToJson(Data, false)).Forget();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 클라우드 동기화 실패: {e.Message}");
        }
    }

    void PreserveDeviceSettings(SaveData target, SaveData local)
    {
        // 디스플레이는 기기마다 다르므로 로컬 값 유지
        target.resolutionWidth = local.resolutionWidth;
        target.resolutionHeight = local.resolutionHeight;
        target.refreshRate = local.refreshRate;
        target.fullscreenMode = local.fullscreenMode;
    }

    // ── 불러오기 ──
    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Data = new SaveData();
            ApplyPlayerPrefsVolume();
            return;
        }

        byte[] encrypted = File.ReadAllBytes(FilePath);

        // 현재 IV로 복호화 시도
        if (TryDecrypt(encrypted, IV, out string json))
        {
            Data = JsonUtility.FromJson<SaveData>(json);
            Migrate();
            return;
        }

        // 구버전(15바이트 IV) 세이브 호환 복호화
        if (TryDecrypt(encrypted, LegacyIV, out json))
        {
            Data = JsonUtility.FromJson<SaveData>(json);
            Save(); // 새 IV로 덮어쓰기
            Migrate();
            return;
        }

        Data = new SaveData();
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
