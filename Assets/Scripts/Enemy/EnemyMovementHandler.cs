using UnityEngine;
using UnityEngine.AI;

public class EnemyMovementHandler : MonoBehaviour
{
    private const float DestinationUpdateThreshold = 0.5f;
    private const float DestinationUpdateThresholdSq = DestinationUpdateThreshold * DestinationUpdateThreshold;

    private Transform _player;
    private NavMeshAgent _agent;
    private Vector3 _lastDestination;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void Initialize(Transform playerTransform, EnemyData data)
    {
        _player = playerTransform;
        _lastDestination = Vector3.positiveInfinity;

        _agent.speed = data.moveSpeed;
        _agent.stoppingDistance = data.attackRange;
        _agent.autoBraking = false;
    }

    public void WarpTo(Vector3 position)
    {
        _lastDestination = Vector3.positiveInfinity;
        transform.position = position; // Move transform first so agent enables at the correct location
        if (!_agent.enabled) _agent.enabled = true;
        _agent.Warp(position); // Snap precisely to NavMesh surface
    }

    private void Update()
    {
        if (_player == null || !_agent.isOnNavMesh) return;

        if ((_player.position - _lastDestination).sqrMagnitude > DestinationUpdateThresholdSq)
        {
            _lastDestination = _player.position;
            _agent.SetDestination(_lastDestination);
        }
    }
}
