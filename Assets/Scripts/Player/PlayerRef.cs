using UnityEngine;

/// <summary>
/// 플레이어 컴포넌트들을 정적으로 캐싱한다.
/// PlayerHealth.Awake에서 Register, OnDestroy에서 Clear 호출.
/// 매번 GameObject.FindGameObjectWithTag("Player")를 호출하던 코드를 대체한다.
/// </summary>
public static class PlayerRef
{
    public static PlayerHealth Health { get; private set; }
    public static Transform Transform { get; private set; }
    public static GameObject GameObject { get; private set; }
    public static IDamageable Damageable { get; private set; }
    public static PlayerController Controller { get; private set; }
    public static PlayerMovement Movement { get; private set; }
    public static Inventory Inventory { get; private set; }

    public static bool Exists => Transform != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Health = null;
        Transform = null;
        GameObject = null;
        Damageable = null;
        Controller = null;
        Movement = null;
        Inventory = null;
    }

    public static void Register(PlayerHealth health)
    {
        if (health == null)
            return;
        Health = health;
        GameObject = health.gameObject;
        Transform = health.transform;
        Damageable = health;
        Controller = health.GetComponent<PlayerController>();
        Movement = health.GetComponent<PlayerMovement>();
        Inventory = health.GetComponent<Inventory>();
    }

    public static void Clear(PlayerHealth health)
    {
        if (Health != health)
            return;
        Health = null;
        GameObject = null;
        Transform = null;
        Damageable = null;
        Controller = null;
        Movement = null;
        Inventory = null;
    }
}
