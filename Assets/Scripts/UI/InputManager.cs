using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public KeyCode Dash { get; private set; } = KeyCode.Z;
    public KeyCode Attack { get; private set; } = KeyCode.X;
    public KeyCode Inventory { get; private set; } = KeyCode.Tab;
    public KeyCode Interact { get; private set; } = KeyCode.A;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Load();
    }

    public void Load()
    {
        if (SaveManager.Instance == null)
            return;

        var kb = SaveManager.Instance.Data.keyBindings;
        Dash = ParseKey(kb.dashKey, KeyCode.Z);
        Attack = ParseKey(kb.attackKey, KeyCode.X);
        Inventory = ParseKey(kb.inventoryKey, KeyCode.Tab);
        Interact = ParseKey(kb.interactKey, KeyCode.A);
    }

    public void SetKey(string action, KeyCode key)
    {
        if (SaveManager.Instance == null)
            return;

        var kb = SaveManager.Instance.Data.keyBindings;
        switch (action)
        {
            case "Dash":
                Dash = key;
                kb.dashKey = key.ToString();
                break;
            case "Attack":
                Attack = key;
                kb.attackKey = key.ToString();
                break;
            case "Inventory":
                Inventory = key;
                kb.inventoryKey = key.ToString();
                break;
            case "Interact":
                Interact = key;
                kb.interactKey = key.ToString();
                break;
        }
        SaveManager.Instance.Save();
    }

    static KeyCode ParseKey(string name, KeyCode fallback)
    {
        try
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), name, true);
        }
        catch
        {
            return fallback;
        }
    }
}
