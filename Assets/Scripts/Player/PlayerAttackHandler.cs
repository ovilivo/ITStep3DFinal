using System;
using UnityEngine;
using UnityEngine.Pool;

public class PlayerAttackHandler : MonoBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private GameObject _attackProjectilePrefab;

    private float _attackRadius;
    private int _damage;
    private float _attackCooldown;
    private float _lastAttackTime;

    private readonly Collider[] _hitBuffer = new Collider[32];
    private ObjectPool<GameObject> _projectilePool;

    private void Awake()
    {
        _projectilePool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(_attackProjectilePrefab);
                go.SetActive(false);
                return go;
            },
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 30
        );
    }

    public void Initialize(CharacterData data)
    {
        _attackRadius = data.attackRadius;
        _damage = data.attackDamage;
        _attackCooldown = data.attackCooldown;
    }

    private void Update()
    {
        if (Time.time >= _lastAttackTime + _attackCooldown)
        {
            Attack();
            _lastAttackTime = Time.time;
        }
    }

    private void Attack()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _attackRadius, _hitBuffer, _enemyLayer);
        if (count == 0) return;

        IDamageable closestTarget = null;
        float closestDistSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            IDamageable damageable = _hitBuffer[i].GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            float distSq = (transform.position - _hitBuffer[i].transform.position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestTarget = damageable;
            }
        }

        if (closestTarget == null) return;

        // Damage is dealt by the projectile on hit — do NOT call TakeDamage here
        GameObject proj = _projectilePool.Get();
        proj.transform.SetPositionAndRotation(transform.position + Vector3.up, Quaternion.identity);
        proj.GetComponent<AttackProjectile>().Initialize(closestTarget, _damage, _projectilePool.Release);
        AudioManager.Play(SfxId.ProjectileFire);
    }
}
