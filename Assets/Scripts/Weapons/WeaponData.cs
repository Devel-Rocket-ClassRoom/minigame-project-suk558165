using UnityEngine;

[CreateAssetMenu(menuName = "Game/WeaponData", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite sprite;

    [Header("Stats")]
    public float damage = 20f;
    public float attackCooldown = 0.5f;

    [Header("Swing")]
    public float swingAngle = 120f;
    public float swingDuration = 0.5f;

    [Header("Animation")]
    public AnimationClip attackClip;
}
