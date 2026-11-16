using UnityEngine;

public class PlayerHealthHandler : BaseHealthHandler
{
    // Health is initialized from CharacterData by PlayerController
    public void Initialize(CharacterData data)
    {
        InitializeHealth(data.maxHealth);
    }

    public override void Die()
    {
        EventBus.Publish(new PlayerDiedEvent());
        GameManager.Instance?.TriggerGameOver();
    }
}
