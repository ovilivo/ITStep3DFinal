using UnityEngine;
using UnityEngine.AI;

public class WaveBossAI : MonoBehaviour, ILethalOnTouch
{
    [Header("Chase Settings")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _stoppingDistance = 1.5f;

    [Header("Spawn Settings")]
    [SerializeField] private float _spawnRadius = 20f;
    [SerializeField] private int _maxSpawnAttempts = 16;
    [SerializeField] private float _maxHeightDifference = 3f;

    private NavMeshAgent _agent;
    private bool _isActive;
    private Transform _player;
    private Camera _camera;
    private Vector3 _lastDestination;
    private float _stoppingDistanceSq;

    private const float DestinationUpdateThreshold = 0.5f;
    private const float DestinationUpdateThresholdSq = DestinationUpdateThreshold * DestinationUpdateThreshold;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        _agent.stoppingDistance = _stoppingDistance;
        _agent.autoBraking = false;
        _stoppingDistanceSq = _stoppingDistance * _stoppingDistance;
        _camera = Camera.main;

        EventBus.Subscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Subscribe<BossPhaseStartedEvent>(OnBossPhaseStarted);
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerRegisteredEvent>(OnPlayerRegistered);
        EventBus.Unsubscribe<BossPhaseStartedEvent>(OnBossPhaseStarted);
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnPlayerRegistered(PlayerRegisteredEvent e)
    {
        _player = e.PlayerTransform;
        _camera = Camera.main;
    }

    private void OnBossPhaseStarted(BossPhaseStartedEvent e) => Activate();

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (e.State == GameState.GameOver)
            gameObject.SetActive(false);
    }

    private void Activate()
    {
        Debug.Log("Activate called!");
        Vector3 spawnPosition = FindSpawnPosition();
        Debug.Log($"Final spawn position: {spawnPosition}");
        

        gameObject.SetActive(true);
        _isActive = true;
        _lastDestination = Vector3.positiveInfinity;

        _agent.enabled = true;
        _agent.Warp(spawnPosition);
        _agent.speed = _moveSpeed;
        _agent.isStopped = false;
    }

    private Vector3 FindSpawnPosition()
    {
        if (_player == null) return transform.position;

        for (int i = 0; i < _maxSpawnAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 candidate = _player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Debug.Log($"Attempt {i}: NavMesh sample FAILED at {candidate}");
                continue;
            }

            Debug.Log($"Attempt {i}: NavMesh hit Y: {hit.position.y} | Player Y: {_player.position.y}");

            if (Mathf.Abs(hit.position.y - _player.position.y) > _maxHeightDifference)
                continue;

            if (IsVisibleToCamera(hit.position))
                continue;

            return hit.position;
        }

        // Fallback
        if (NavMesh.SamplePosition(_player.position, out NavMeshHit fallback, 10f, NavMesh.AllAreas))
        {
            Debug.Log($"Using FALLBACK position Y: {fallback.position.y} | Player Y: {_player.position.y}");
            return fallback.position;
        }

        Debug.Log("All failed — using transform.position");
        return transform.position;
    }

    private bool IsVisibleToCamera(Vector3 worldPos)
    {
        if (_camera == null) return false;

        Vector3 viewportPos = _camera.WorldToViewportPoint(worldPos);
        return viewportPos.z > 0f
            && viewportPos.x > 0f && viewportPos.x < 1f
            && viewportPos.y > 0f && viewportPos.y < 1f;
    }

    private void Update()
    {
        if (!_isActive || _player == null) return;

        if ((_player.position - transform.position).sqrMagnitude <= _stoppingDistanceSq) return;

        if (_agent.isOnNavMesh)
        {
            if ((_player.position - _lastDestination).sqrMagnitude > DestinationUpdateThresholdSq)
            {
                _lastDestination = _player.position;
                _agent.SetDestination(_lastDestination);
                _agent.isStopped = false;
            }
        }
        else
        {
            Vector3 direction = (_player.position - transform.position).normalized;
            transform.position += direction * _moveSpeed * Time.deltaTime;
            transform.LookAt(_player.position);
        }
    }
}