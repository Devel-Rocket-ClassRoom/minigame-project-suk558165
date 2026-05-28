using UnityEngine;

public class RunStats : MonoBehaviour
{
    public static RunStats Instance { get; private set; }

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
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    }

    public void StopTimer() => running = false;

    public void AddKill() => Kills++;

    public void AddDeath() => Deaths++;

    public void AddGold(int amount) => GoldEarned += Mathf.Max(0, amount);

    public void AddDamageDealt(float d) => DamageDealt += Mathf.Max(0f, d);

    public void AddDamageTaken(float d) => DamageTaken += Mathf.Max(0f, d);

    public void AddItem() => ItemsGained++;
}
