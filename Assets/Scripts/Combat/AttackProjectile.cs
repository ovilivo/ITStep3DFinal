using System;
using UnityEngine;

public class AttackProjectile : MonoBehaviour
{
    private const float HitDistanceSq = 0.3f * 0.3f;
    private const float Speed = 20f;

    private IDamageable _target;
    private int _damage;
    private Collider _targetCollider;
    private MonoBehaviour _targetMB;
    private Action<GameObject> _onRelease;

    public void Initialize(IDamageable targetEnemy, int damageAmount, Action<GameObject> releaseCallback)
    {
        _target = targetEnemy;
        _damage = damageAmount;
        // Cast to MonoBehaviour so we can check gameObject.activeInHierarchy in Update —
        // that's how we detect when the target has been returned to the enemy pool.
        _targetMB = _target as MonoBehaviour;
        _targetCollider = _targetMB != null ? _targetMB.GetComponent<Collider>() : null;
        _onRelease = releaseCallback;
    }

    private void Update()
    {
        if (_target == null || _targetMB == null)
        {
            Release();
            return;
        }

        // Target was pooled (deactivated) — stop tracking
        if (!_targetMB.gameObject.activeInHierarchy)
        {
            Release();
            return;
        }

        Vector3 targetPos = _targetCollider != null
            ? _targetCollider.bounds.center
            : _targetMB.transform.position;

        transform.position += (targetPos - transform.position).normalized * Speed * Time.deltaTime;

        if ((transform.position - targetPos).sqrMagnitude < HitDistanceSq)
        {
            _target.TakeDamage(_damage);
            AudioManager.Play(SfxId.Hit);
            Release();
        }
    }

    private void Release()
    {
        _target = null;
        _targetMB = null;
        _targetCollider = null;
        _onRelease?.Invoke(gameObject);
    }
}
