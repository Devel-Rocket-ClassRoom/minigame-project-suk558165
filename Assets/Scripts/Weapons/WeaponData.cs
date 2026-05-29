using UnityEngine;
using UnityEngine.Localization;

public enum WeaponType
{
    Melee,
    Ranged,
}

[CreateAssetMenu(menuName = "Game/WeaponData", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    [Tooltip("세이브/로드에 사용되는 고유 식별자. 절대 변경 금지.")]
    public string id;

    public LocalizedString weaponName;
    public Sprite sprite;
    public WeaponType weaponType;

    public LocalizedString description;
    public int price;

    [Header("Stats")]
    public float damage = 20f;
    public float attackCooldown = 0.5f;

    [Header("Visuals")]
    public float spriteScale = 1f;

    [Header("Swing (Melee)")]
    public float swingAngle = 120f;
    public float swingDuration = 0.5f;

    [Header("Ranged")]
    public float projectileSpeed = 12f;
}
