using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Firebase;

/// <summary>
/// 세이브 데이터 관리자. 로컬 파일 + Firebase Realtime DB 클라우드 동기화를 함께 처리.
///
/// 동작 흐름:
///  1) Awake: 로컬 save.dat 즉시 로드 (오프라인에서도 동작)
///  2) Firebase 로그인 성공 → SyncFromCloudAsync (클라우드 vs 로컬 중 최신본 사용)
///  3) Save() 호출 → 로컬 저장 + 백그라운드로 클라우드 업로드
///  4) Firebase 로그아웃 → 로컬 데이터 초기화 (계정별 데이터 격리)
///
/// 충돌 해결:
///  - SaveData.updatedAt (Unix millis 타임스탬프) 비교
///  - 더 큰 쪽(=더 최근)이 이김
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    /// <summary>세이브 포맷 버전. 새 필드 추가 시 올리고 Migrate()에서 업그레이드 처리.</summary>
    public const int CurrentVersion = 1;

    /// <summary>현재 로드된 세이브 데이터. 게임 코드는 이걸 읽고 쓰면 됨.</summary>
    public SaveData Data { get; private set; }

    // ── AES-256 암호화 키 ──
    // 32바이트 = AES-256. 세이브 파일 직접 편집(치트) 방지용.
    // 실배포 시엔 코드에 평문으로 두지 말고 난독화 또는 외부 키 관리 권장.
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("MiniGame_SaveKey_2024!__32Bytes!");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("MG_InitVec_16B!!");

    // 구버전 호환: IV가 15바이트였던 시절의 세이브 복호화용
    private static readonly byte[] LegacyIV = Encoding.UTF8.GetBytes("MG_InitVec_16B!");

    // Application.persistentDataPath: 플랫폼별 사용자 데이터 폴더 (Windows %APPDATA%, Mac ~/Library 등)
    private string FilePath => Path.Combine(Application.persistentDataPath, "save.dat");

    void Awake()
    {
        // 싱글톤 + 씬 전환에도 살아남기
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 로컬 즉시 로드 (Firebase 로그인 전에도 게임 진입은 가능해야 함)
        Load();
    }

    void Start()
    {
        // FirebaseAuthManager가 Awake에서 Instance를 등록하므로 Start에서 안전하게 구독 가능
        // (씬 배치 순서와 무관하게 모든 Awake 완료 후 Start 호출됨)
        SubscribeAuthAsync().Forget();
    }

    async UniTaskVoid SubscribeAuthAsync()
    {
        // FirebaseInitializer 준비까지 대기 — 자동 로그인 이벤트 놓치지 않도록
        if (FirebaseInitializer.Instance != null)
            await FirebaseInitializer.Instance.WaitUntilReadyAsync();

        if (FirebaseAuthManager.Instance == null) return;

        FirebaseAuthManager.Instance.OnSignedIn += _ => SyncFromCloudAsync().Forget();
        FirebaseAuthManager.Instance.OnSignedOut += OnSignedOutReset;

        // 이미 자동 로그인된 상태라면 OnSignedIn 이벤트가 우리 구독 전에 발동됐을 수 있음 → 한 번 강제 sync
        if (FirebaseAuthManager.Instance.IsLoggedIn)
            SyncFromCloudAsync().Forget();
    }

    /// <summary>
    /// 로그아웃 시 호출. 계정 데이터를 모두 지운다.
    /// 단, 디스플레이/오디오/언어/키 같은 기기 종속 설정은 DeleteSave()가 보존.
    /// 이유: 다른 사람이 같은 PC에서 로그인하면 이전 사용자의 골드/인벤토리가 보이면 안 됨.
    /// </summary>
    void OnSignedOutReset()
    {
        DeleteSave();
        Debug.Log("[SaveManager] 로그아웃: 로컬 데이터 초기화");
    }

    /// <summary>
    /// 현재 Data를 저장. 로컬 파일 즉시 쓰기 + 클라우드는 백그라운드(fire-and-forget) 업로드.
    /// 게임 코드에서 골드/아이템 변경 후 이 메서드만 호출하면 됨.
    /// </summary>
    public void Save()
    {
        // 클라우드 충돌 해결용 타임스탬프 갱신
        Data.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 1) 직렬화 + AES 암호화 + 로컬 파일 쓰기 (동기, 빠름)
        string json = JsonUtility.ToJson(Data, false);
        byte[] encrypted = Encrypt(json);
        File.WriteAllBytes(FilePath, encrypted);

        // 2) 클라우드 업로드는 await 하지 않고 백그라운드로 던짐 (게임이 멈추지 않게)
        UploadToCloudAsync(json).Forget();
    }

    /// <summary>
    /// 클라우드(Firebase Realtime DB)에 세이브 업로드.
    /// 비로그인/DB 미초기화 시 조용히 스킵 (오프라인이어도 게임은 정상 동작).
    /// </summary>
    async UniTaskVoid UploadToCloudAsync(string json)
    {
        var auth = FirebaseAuthManager.Instance;
        var db = FirebaseDBManager.Instance;
        // 모든 조건이 갖춰져야 업로드 시도
        if (auth == null || !auth.IsLoggedIn || db == null || !db.IsReady) return;

        try
        {
            // 경로: users/{본인UID}/save  (보안 규칙으로 본인만 read/write 가능)
            await db.SetJsonAsync($"users/{auth.UserId}/save", json);
        }
        catch (Exception e)
        {
            // 실패해도 로컬 저장은 이미 완료 → 게임 흐름엔 영향 없음
            Debug.LogWarning($"[SaveManager] 클라우드 업로드 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 로그인 직후 호출. 클라우드와 로컬 중 최신본을 채택.
    ///
    /// 시나리오:
    ///  - 클라우드 비어있음 = 신규 계정 → 로컬 초기화 (이전 사용자 데이터 누출 방지)
    ///  - 클라우드가 더 최신 (다른 기기에서 플레이) → 클라우드로 로컬 덮어쓰기
    ///  - 로컬이 더 최신 (오프라인 플레이 후 첫 로그인) → 클라우드 업로드
    /// </summary>
    async UniTaskVoid SyncFromCloudAsync()
    {
        var auth = FirebaseAuthManager.Instance;
        var db = FirebaseDBManager.Instance;
        if (auth == null || !auth.IsLoggedIn || db == null) return;

        // DBManager 초기화 대기 (Awake 순서 보장 안 되는 케이스 대응)
        await UniTask.WaitUntil(() => db.IsReady);

        try
        {
            string cloudJson = await db.GetJsonAsync($"users/{auth.UserId}/save");
            if (string.IsNullOrEmpty(cloudJson))
            {
                // 클라우드에 없음 = 이 계정으로 첫 플레이 → 로컬을 리셋
                // (이전에 다른 계정으로 플레이했던 데이터가 로컬에 남아있을 수 있으므로 보호)
                DeleteSave();
                Debug.Log("[SaveManager] 신규 계정: 빈 세이브로 시작");
                return;
            }

            var cloud = JsonUtility.FromJson<SaveData>(cloudJson);
            if (cloud.updatedAt > Data.updatedAt)
            {
                // 클라우드가 더 최신 → 채택. 단, 기기 설정(해상도 등)은 로컬 유지.
                PreserveDeviceSettings(cloud, Data);
                Data = cloud;
                string json = JsonUtility.ToJson(Data, false);
                byte[] encrypted = Encrypt(json);
                File.WriteAllBytes(FilePath, encrypted);
                Debug.Log("[SaveManager] 클라우드 세이브로 동기화됨");
            }
            else if (Data.updatedAt > cloud.updatedAt)
            {
                // 로컬이 더 최신 (오프라인 플레이 후 로그인) → 클라우드에 반영
                UploadToCloudAsync(JsonUtility.ToJson(Data, false)).Forget();
            }
            // 같으면 아무것도 안 함
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 클라우드 동기화 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 클라우드에서 받은 데이터로 덮어쓸 때 기기 종속 설정은 로컬 값 유지.
    /// 이유: A기기 1080p ↔ B기기 720p 환경에서 클라우드 동기화하면 해상도가 깨질 수 있음.
    /// </summary>
    void PreserveDeviceSettings(SaveData target, SaveData local)
    {
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
