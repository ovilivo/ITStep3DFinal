using UnityEngine;

/// <summary>
/// Abstract base for all health components. Handles HP tracking and damage reception.
/// Subclasses implement Die() with entity-specific death behavior.
/// </summary>
public abstract class BaseHealthHandler : MonoBehaviour, IDamageable, IKillable
{
    protected int _maxHealth;
    protected int _currentHealth;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    protected void InitializeHealth(int maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
    }

    public virtual void TakeDamage(int amount)
    {
        if (_currentHealth <= 0) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);

        if (_currentHealth <= 0)
            Die();
    }

    public abstract void Die();
}
