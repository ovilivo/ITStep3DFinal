using UnityEngine;

/// <summary>
/// Falling off the map is allowed, but not forever: below <see cref="_fallLimitY"/> the player is put back
/// at the spawn point and the run timer takes a penalty (see <see cref="PlayerFellOffMapEvent"/>).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFallRespawn : MonoBehaviour
{
    [Tooltip("When the player's Y goes below this, the player is respawned.")]
    [SerializeField] private float _fallLimitY = -200f;
    [Tooltip("Seconds taken off the countdown each time the player falls out of the map.")]
    [SerializeField] private float _timePenaltySeconds = 5f;

    private CharacterController _controller;
    private PlayerMovementHandler _movement;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovementHandler>();
    }

    private void Start()
    {
        // Where the player starts is where the player comes back.
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (transform.position.y >= _fallLimitY) return;
        if (GameManager.Instance != null && GameManager.Instance.IsRunEnded) return;

        Respawn();
    }

    private void Respawn()
    {
        // A CharacterController must be off while it is teleported.
        _controller.enabled = false;
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        _controller.enabled = true;
        Physics.SyncTransforms();

        if (_movement != null)
            _movement.ResetMotion();

        EventBus.Publish(new PlayerFellOffMapEvent { TimePenaltySeconds = _timePenaltySeconds });
    }
}
