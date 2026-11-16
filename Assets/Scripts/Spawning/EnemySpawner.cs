using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

/// <summary>
/// Spawns enemies from a weighted pool. Maintains one ObjectPool per enemy prefab
/// so enemies are reused rather than instantiated/destroyed every spawn cycle.
/// Add entries to _enemyTypes in the Inspector — each entry pairs a prefab with
/// its EnemyData ScriptableObject. Spawn rate ramps from _initialSpawnInterval
/// down to _minSpawnInterval over _rampDuration seconds.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public struct SpawnableEnemy
    {
        public GameObject prefab;
        public EnemyData data;
    }

    private const float MinTime = 0.01f;

    [SerializeField] private List<SpawnableEnemy> _enemyTypes;

    [Header("Spawn Rate")]
    [Tooltip("Seconds between spawns at the start of the run (start rate = 1 / this).")]
    [SerializeField] private float _initialSpawnInterval = 3f;
    [Tooltip("Safety cap: spawns never come faster than one per this many seconds.")]
    [SerializeField] private float _minSpawnInterval = 0.5f;
    [Tooltip("Every this many seconds the spawn rate goes up by 'Spawn Rate Increase Per Step'.")]
    [SerializeField] private float _stepSeconds = 10f;
    [Tooltip("Spawns per second added at every step.")]
    [SerializeField] private float _spawnRateIncreasePerStep = 0.1f;

    [Header("Spawn Position")]
    [SerializeField] private float _spawnRadius = 20f;
    [SerializeField] private LayerMask _occlusionLayers;
    [SerializeField] private int _maxSpawnAttempts = 10;

    [Header("Pooling")]
    [SerializeField] private int _poolDefaultCapacity = 20;
    [SerializeField] private int _poolMaxSize = 50;
    [SerializeField] private int _prewarmCount = 10;

    private Transform _player;
    private Camera _camera;
    private Transform _poolContainer;
    private float _totalSpawnWeight;
    private float _spawnElapsed;
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _enemyPoolMap = new();
    private readonly Dictionary<GameObject, EnemyController> _controllerCache = new();

    private void Awake()
    {
        // Disabled container defers NavMeshAgent.Awake until we've had a chance to disable the agent,
        // preventing "not close enough to NavMesh" warnings during pool pre-warming.
        var containerGO = new GameObject("EnemyPool_Container");
        containerGO.SetActive(false);
        _poolContainer = containerGO.transform;

        foreach (var entry in _enemyTypes)
        {
            var prefab = entry.prefab;
            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Instantiate(prefab, _poolContainer);
                    _controllerCache[go] = go.GetComponent<EnemyController>();
                    go.GetComponent<NavMeshAgent>().enabled = false;
                    go.SetActive(false);
                    go.transform.SetParent(null, true);
                    return go;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: Destroy,
                collectionCheck: false,
                defaultCapacity: _poolDefaultCapacity,
                maxSize: _poolMaxSize
            );
            _pools[prefab] = pool;
            _totalSpawnWeight += entry.data.spawnWeight;
            PrewarmPool(pool, _prewarmCount);
        }
    }

    private void PrewarmPool(ObjectPool<GameObject> pool, int count)
    {
        var prewarmBuffer = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
            prewarmBuffer.Add(pool.Get());
        foreach (var obj in prewarmBuffer)
            pool.Release(obj);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (!e.State.IsRunEnded()) return;

        StopAllCoroutines();

        // On a loss the enemies are removed; on a win they stay where they are (time is frozen).
        if (e.State == GameState.GameOver)
            ReleaseAllActiveEnemies();
    }

    private void ReleaseAllActiveEnemies()
    {
        foreach (var kvp in _enemyPoolMap)
            kvp.Value.Release(kvp.Key);
        _enemyPoolMap.Clear();
    }

    private void OnPlayerRegistered(PlayerRegisteredEvent e)
    {
        _player = e.PlayerTransform;
        _camera = Camera.main;
        _spawnElapsed = 0f;
        StartCoroutine(SpawnLoop());
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        GameData.Score++;
        if (_enemyPoolMap.TryGetValue(e.EnemyObject, out var pool))
        {
            pool.Release(e.EnemyObject);
            _enemyPoolMap.Remove(e.EnemyObject);
        }
    }

    // ── Spawn loop ────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulates "spawn credit" at the current rate and spawns whenever a full spawn is owed,
    /// so a rate increase takes effect immediately instead of after the next scheduled spawn.
    /// The first spawn comes after one initial interval.
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        float spawnCredit = 0f;

        while (true)
        {
            _spawnElapsed += Time.deltaTime;
            spawnCredit += GetCurrentSpawnRate() * Time.deltaTime;

            if (spawnCredit >= 1f)
            {
                spawnCredit -= 1f;
                TrySpawnEnemy();
            }

            yield return null;
        }
    }

    /// <summary>Spawns per second: the start rate plus one increase for every completed step.</summary>
    private float GetCurrentSpawnRate()
    {
        float startRate = 1f / Mathf.Max(_initialSpawnInterval, MinTime);
        int completedSteps = Mathf.FloorToInt(_spawnElapsed / Mathf.Max(_stepSeconds, MinTime));
        float rate = startRate + completedSteps * _spawnRateIncreasePerStep;
        return Mathf.Min(rate, 1f / Mathf.Max(_minSpawnInterval, MinTime));
    }

    // ── Spawn position ────────────────────────────────────────────────────────

    private void TrySpawnEnemy()
    {
        if (_player == null || _enemyTypes.Count == 0) return;

        if (!TryGetSpawnPosition(out Vector3 spawnPos))
            return;

        SpawnableEnemy entry = GetWeightedRandom();
        ObjectPool<GameObject> pool = _pools[entry.prefab];

        GameObject enemyObj = pool.Get();
        EnemyController controller = _controllerCache[enemyObj];
        controller.Initialize(_player, entry.data);
        controller.PlaceAt(spawnPos);
        _enemyPoolMap[enemyObj] = pool;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector3 offset = UnityEngine.Random.insideUnitSphere * _spawnRadius;
            offset.y = 0f;
            Vector3 desiredPos = _player.position + offset;

            if (!NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                continue;

            // Reject underground NavMesh surfaces — only spawn at roughly player height
            if (Mathf.Abs(hit.position.y - _player.position.y) > 3f)
                continue;

            if (IsVisibleToCamera(hit.position))
                continue;

            spawnPos = hit.position;
            return true;
        }

        spawnPos = Vector3.zero;
        return false;
    }

    // Returns true if the position is inside the camera frustum with an unobstructed
    // line of sight — meaning the player could actually see an enemy appear there.
    // Positions in the frustum but behind geometry return false (valid surprise spawns).
    private bool IsVisibleToCamera(Vector3 worldPos)
    {
        if (_camera == null) return false;

        Vector3 viewportPos = _camera.WorldToViewportPoint(worldPos);
        bool inFrustum = viewportPos.z > 0f
                      && viewportPos.x > 0f && viewportPos.x < 1f
                      && viewportPos.y > 0f && viewportPos.y < 1f;

        if (!inFrustum) return false;

        // In frustum but occluded by world geometry — still a valid (surprise) spawn
        if (Physics.Linecast(_camera.transform.position, worldPos + Vector3.up, _occlusionLayers))
            return false;

        return true;
    }

    // ── Weighted random ───────────────────────────────────────────────────────

    private SpawnableEnemy GetWeightedRandom()
    {
        float roll = UnityEngine.Random.value * _totalSpawnWeight;
        float cumulative = 0f;

        foreach (var entry in _enemyTypes)
        {
            cumulative += entry.data.spawnWeight;
            if (roll <= cumulative) return entry;
        }

        return _enemyTypes[_enemyTypes.Count - 1];
    }
}
