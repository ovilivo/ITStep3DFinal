using UnityEngine;

public class EnemyHealthHandler : BaseHealthHandler
{
    // Called by EnemyController on each spawn (including pool re-use)
    public void Initialize(EnemyData data)
    {
        InitializeHealth(data.maxHealth);
    }

    public override void Die()
    {
        EventBus.Publish(new EnemyDiedEvent { EnemyObject = gameObject });
        // Actual deactivation (pool release) is handled by EnemySpawner via EnemyDiedEvent
    }
}
