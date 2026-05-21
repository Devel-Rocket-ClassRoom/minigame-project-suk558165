using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged,
}

[CreateAssetMenu(menuName = "Game/WeaponData", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite sprite;
    public WeaponType weaponType;

    [Header("Stats")]
    public float damage = 20f;
    public float attackCooldown = 0.5f;

    [Header("Swing (Melee)")]
    public float swingAngle = 120f;
    public float swingDuration = 0.5f;

    [Header("Ranged")]
    public float projectileSpeed = 12f;

    [Header("Animation")]
    public AnimationClip attackClip;
    public AnimationClip idleClip;
}
