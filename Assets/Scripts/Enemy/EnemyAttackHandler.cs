using UnityEngine;

public class EnemyAttackHandler : MonoBehaviour
{
    [Tooltip("Off by default: touching an enemy ends the run instantly (see PlayerTouchHandler), so contact damage is not used.")]
    [SerializeField] private bool _dealContactDamage;

    private int _damage;
    private float _attackCooldown;
    private float _attackRangeSq;
    private float _lastAttackTime;

    private Transform _player;
    private IDamageable _playerDamageable;

    public void Initialize(Transform playerTransform, EnemyData data)
    {
        _player = playerTransform;
        _playerDamageable = playerTransform.GetComponent<IDamageable>();
        _damage = data.attackDamage;
        _attackCooldown = data.attackCooldown;
        _attackRangeSq = data.attackRange * data.attackRange;
    }

    private void Update()
    {
        if (!_dealContactDamage || _player == null) return;

        if ((_player.position - transform.position).sqrMagnitude <= _attackRangeSq)
            TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < _lastAttackTime + _attackCooldown) return;

        _playerDamageable?.TakeDamage(_damage);
        _lastAttackTime = Time.time;
    }
}
