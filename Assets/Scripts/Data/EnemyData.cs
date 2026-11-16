using UnityEngine;

/// <summary>
/// ScriptableObject that defines all stats for an enemy type.
/// Create via: Assets > Create > DayOff > Enemy Data
/// One asset per enemy type. Assign to SpawnableEnemy entries on EnemySpawner.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "DayOff/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Health")]
    public int maxHealth = 20;

    [Header("Attack")]
    public int attackDamage = 5;
    public float attackCooldown = 1f;
    public float attackRange = 1.5f;

    [Header("Spawning")]
    [Tooltip("Relative weight in the spawn pool. Higher = spawns more often.")]
    public float spawnWeight = 1f;
}
