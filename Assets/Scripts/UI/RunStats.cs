using UnityEngine;

public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    public float PlayTime { get; private set; }
    public int Deaths { get; private set; }
    public int Kills { get; private set; }
    public int GoldEarned { get; private set; }
    public float DamageDealt { get; private set; }
    public float DamageTaken { get; private set; }
    public int ItemsGained { get; private set; }

    private bool running;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
    }

    void Update()
    {
        if (running)
            PlayTime += Time.deltaTime;
    }

    public void StartRun()
    {
        PlayTime = 0f;
        Deaths = 0;
        Kills = 0;
        GoldEarned = 0;
        DamageDealt = 0f;
        DamageTaken = 0f;
        ItemsGained = 0;
        running = true;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.totelData.totalRuns++;
            SaveManager.Instance.Save();
        }
    }

    public void StopTimer()
    {
        running = false;
        if (SaveManager.Instance != null)
        {
            var t = SaveManager.Instance.Data.totelData;
            t.totalPlaytime += PlayTime;
            t.UpdatePlaytimeFormatted();
        }
        SaveBestRun();
    }

    void SaveBestRun()
    {
        if (SaveManager.Instance == null)
            return;

        var best = SaveManager.Instance.Data.bestRun;
        bool changed = false;

        if (Kills > best.bestKills)
        {
            best.bestKills = Kills;
            changed = true;
        }
        if (GoldEarned > best.bestGoldEarned)
        {
            best.bestGoldEarned = GoldEarned;
            changed = true;
        }
        if (PlayTime > best.bestPlayTime)
        {
            best.bestPlayTime = PlayTime;
            changed = true;
        }
        if (DamageDealt > best.bestDamageDealt)
        {
            best.bestDamageDealt = DamageDealt;
            changed = true;
        }

        // 누적 통계는 StopTimer 시점에 무조건 저장
        SaveManager.Instance.Save();

        // 도전과제 평가
        AchievementManager.Instance?.Evaluate();
    }

    public void AddKill()
    {
        Kills++;
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.totelData.totalKills++;
    }

    public void AddBossKill()
    {
        AddKill();
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.totelData.totalbossKliis++;
    }

    public void AddDeath()
    {
        Deaths++;
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.totelData.totalDeaths++;
    }

    public void AddGold(int amount)
    {
        int a = Mathf.Max(0, amount);
        GoldEarned += a;
        if (SaveManager.Instance != null)
            SaveManager.Instance.Data.totelData.totalGoldGet += a;
    }

    public void AddDamageDealt(float d) => DamageDealt += Mathf.Max(0f, d);

    public void AddDamageTaken(float d) => DamageTaken += Mathf.Max(0f, d);

    public void AddItem() => ItemsGained++;
}
