using UnityEngine;

[RequireComponent(typeof(EnemyMovementHandler))]
[RequireComponent(typeof(EnemyAttackHandler))]
[RequireComponent(typeof(EnemyHealthHandler))]
public class EnemyController : MonoBehaviour, ILethalOnTouch
{
    private EnemyMovementHandler _movementHandler;
    private EnemyAttackHandler _attackHandler;
    private EnemyHealthHandler _healthHandler;

    private void Awake()
    {
        // Cache components in Awake — they are guaranteed by [RequireComponent]
        _movementHandler = GetComponent<EnemyMovementHandler>();
        _attackHandler = GetComponent<EnemyAttackHandler>();
        _healthHandler = GetComponent<EnemyHealthHandler>();
    }

    /// <summary>Called by EnemySpawner on each spawn (including pool re-use).</summary>
    public void Initialize(Transform playerTransform, EnemyData data)
    {
        _movementHandler.Initialize(playerTransform, data);
        _attackHandler.Initialize(playerTransform, data);
        _healthHandler.Initialize(data);
    }

    /// <summary>Warps the enemy to a NavMesh-safe position. Call after Initialize.</summary>
    public void PlaceAt(Vector3 position) => _movementHandler.WarpTo(position);
}
