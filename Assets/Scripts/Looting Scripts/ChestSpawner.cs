using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// Spawns chests around the player on a schedule: some at the start of the run, more as time passes.
/// Positions come from a ground raycast (the original approach) and must also be on the NavMesh, so the
/// player can walk to every chest. Which chest type appears is picked by rarity weight.
/// </summary>
public class ChestSpawner : MonoBehaviour
{
    [Serializable]
    public class ChestEntry
    {
        public GameObject prefab;
        [Range(0, 100)] public float spawnChance;
    }

    [Serializable]
    public struct ChestWave
    {
        [Tooltip("Seconds after the player spawns.")]
        public float time;
        [Tooltip("How many chests to spawn at that time.")]
        public int count;
    }

    private const int AttemptsPerChest = 20;
    private const float PlayerSnapMultiplier = 2f;

    [Header("Chest Prefabs & Rarity")]
    public List<ChestEntry> chests;

    [Header("Schedule")]
    [SerializeField] private List<ChestWave> _waves = new List<ChestWave>
    {
        new ChestWave { time = 0f, count = 7 },
        new ChestWave { time = 20f, count = 2 },
        new ChestWave { time = 40f, count = 2 },
    };

    [Header("Placement (around the player)")]
    [FormerlySerializedAs("spawnRadius")]
    [SerializeField] private float _spawnRadius = 60f;
    [Tooltip("Chests never spawn closer to the player than this.")]
    [SerializeField] private float _minDistanceFromPlayer = 8f;
    [Tooltip("Minimum distance between two chests.")]
    [SerializeField] private float _minChestSpacing = 6f;
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;
    [FormerlySerializedAs("groundCheckHeight")]
    [SerializeField] private float _groundCheckHeight = 20f;
    [Tooltip("A spawn point must be within this distance of the NavMesh, so the player can reach it.")]
    [SerializeField] private float _navMeshSnapDistance = 2f;
    [Tooltip("Lift above the ground hit point (chest prefabs have their pivot at the base).")]
    [SerializeField] private float _groundOffset = 0.02f;

    private readonly List<Vector3> _placed = new List<Vector3>();
    private NavMeshPath _path;
    private Transform _player;

    private void Awake()
    {
        // NavMeshPath cannot be created in a field initializer.
        _path = new NavMeshPath();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnPlayerRegistered(PlayerRegisteredEvent e)
    {
        if (chests == null || chests.Count == 0)
        {
            Debug.LogWarning("[ChestSpawner] No chest prefabs assigned.", this);
            return;
        }
        if (_groundLayer.value == 0)
            Debug.LogWarning("[ChestSpawner] Ground Layer is not set; no chest can be placed.", this);

        _player = e.PlayerTransform;
        StartCoroutine(RunSchedule());
    }

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (e.State.IsRunEnded())
            StopAllCoroutines();
    }

    private IEnumerator RunSchedule()
    {
        float elapsed = 0f;

        foreach (ChestWave wave in _waves.OrderBy(w => w.time))
        {
            if (wave.time > elapsed)
                yield return new WaitForSeconds(wave.time - elapsed);

            elapsed = Mathf.Max(elapsed, wave.time);
            SpawnChests(wave.count);
        }
    }

    private void SpawnChests(int count)
    {
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * AttemptsPerChest;

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            if (!TryGetSpawnPoint(out Vector3 point)) continue;

            GameObject prefab = PickChestByRarity();
            if (prefab == null) continue;

            Instantiate(prefab, point + Vector3.up * _groundOffset, Quaternion.identity);
            _placed.Add(point);
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[ChestSpawner] Only placed {spawned}/{count} chests; check radius, ground layer and NavMesh.", this);
    }

    private bool TryGetSpawnPoint(out Vector3 point)
    {
        point = default;

        Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(_minDistanceFromPlayer, _spawnRadius);
        Vector3 origin = _player.position + new Vector3(ring.x, _groundCheckHeight, ring.y);

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundCheckHeight * 2f, _groundLayer, QueryTriggerInteraction.Ignore))
            return false;

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, _navMeshSnapDistance, NavMesh.AllAreas))
            return false;

        // Near the NavMesh is not enough: it may be a disconnected island (courtyard, rooftop). Require a full walkable path.
        if (!HasCompletePath(navHit.position))
            return false;

        float spacingSq = _minChestSpacing * _minChestSpacing;
        foreach (Vector3 other in _placed)
            if ((other - hit.point).sqrMagnitude < spacingSq)
                return false;

        point = hit.point;
        return true;
    }

    private bool HasCompletePath(Vector3 target)
    {
        if (!NavMesh.SamplePosition(_player.position, out NavMeshHit from, _navMeshSnapDistance * PlayerSnapMultiplier, NavMesh.AllAreas))
            return true; // the player itself is not on the NavMesh: nothing meaningful to compare against

        return NavMesh.CalculatePath(from.position, target, NavMesh.AllAreas, _path)
               && _path.status == NavMeshPathStatus.PathComplete;
    }

    private GameObject PickChestByRarity()
    {
        float total = 0f;
        foreach (var c in chests) total += c.spawnChance;
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var c in chests)
        {
            cumulative += c.spawnChance;
            if (roll < cumulative) return c.prefab;
        }

        return chests[chests.Count - 1].prefab;
    }
}
