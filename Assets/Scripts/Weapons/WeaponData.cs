using UnityEngine;
using UnityEngine.Localization;

public enum WeaponType
{
    Melee,
    Ranged,
    Magic,
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

    [Header("Audio")]
    public AudioClip attackSound;

    [Header("Visuals")]
    public float spriteScale = 1f;
    public Vector2 spriteOffset = Vector2.zero;
    public float spriteRotation = 0f;

    [Header("Swing (Melee)")]
    public float swingAngle = 120f;
    public float swingDuration = 0.5f;

    [Tooltip("공격 중에도 방향 전환 허용 (건틀릿 등 연타 무기)")]
    public bool flipDuringAttack = false;

    [Header("Ranged")]
    public float projectileSpeed = 12f;
}
