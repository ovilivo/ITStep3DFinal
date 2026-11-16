using UnityEngine;

/// <summary>
/// ScriptableObject that defines all stats for a playable character.
/// Create via: Assets > Create > DayOff > Character Data
/// One asset per character — swap the reference on PlayerController to change characters.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "DayOff/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Character";

    [Header("Movement")]
    public float moveSpeed = 12f;
    public float acceleration = 20f;
    public float airControl = 5f;
    public float rotationSpeed = 15f;
    public float jumpForce = 10f;
    public float gravity = -30f;

    [Header("Health")]
    public int maxHealth = 100;

    [Header("Attack")]
    public float attackRadius = 2f;
    public int attackDamage = 10;
    public float attackCooldown = 1f;
}
