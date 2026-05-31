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

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── 저장 ──
    public void Save()
    {
        string json = JsonUtility.ToJson(Data, false);
        byte[] encrypted = Encrypt(json);
        File.WriteAllBytes(FilePath, encrypted);
    }

    // ── 불러오기 ──
    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Data = new SaveData();
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
            Debug.Log("[SaveManager] 구버전 세이브 감지 → 새 형식으로 재저장");
            Data = JsonUtility.FromJson<SaveData>(json);
            Save(); // 새 IV로 덮어쓰기
            Migrate();
            return;
        }

        Debug.LogWarning("[SaveManager] 세이브 복호화 실패, 새 데이터 생성");
        Data = new SaveData();
    }

    // ── 세이브 초기화 ──
    public void DeleteSave()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        Data = new SaveData();
    }

    // ── 마이그레이션 ──
    void Migrate()
    {
        if (Data.version >= CurrentVersion)
            return;

        // 예시: version 1 → 2 마이그레이션이 필요할 때
        // if (Data.version < 2)
        // {
        //     Data.newField = defaultValue;
        //     Data.version = 2;
        // }

        Data.version = CurrentVersion;
        Save();
        Debug.Log($"[SaveManager] 세이브 마이그레이션 완료 → v{CurrentVersion}");
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
