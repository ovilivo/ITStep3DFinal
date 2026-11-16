using UnityEngine;

[RequireComponent(typeof(PlayerMovementHandler))]
[RequireComponent(typeof(PlayerAttackHandler))]
[RequireComponent(typeof(PlayerHealthHandler))]
public class PlayerController : MonoBehaviour, ILookInputProvider
{
    [SerializeField] private CharacterData _characterData;

    private PlayerMovementHandler _movementHandler;
    private PlayerAttackHandler _attackHandler;
    private PlayerHealthHandler _healthHandler;
    private bool _initialized;

    public Vector2 LookInput => _movementHandler.LookInput;

    private void Awake()
    {
        _movementHandler = GetComponent<PlayerMovementHandler>();
        _attackHandler = GetComponent<PlayerAttackHandler>();
        _healthHandler = GetComponent<PlayerHealthHandler>();

        if (_characterData == null)
        {
            Debug.LogError("[PlayerController] CharacterData is not assigned!", this);
            return;
        }

        Camera mainCamera = Camera.main;

        _movementHandler.Initialize(mainCamera.transform, _characterData);
        _attackHandler.Initialize(_characterData);
        _healthHandler.Initialize(_characterData);

        CameraFollowHandler cameraHandler = mainCamera.GetComponent<CameraFollowHandler>();
        cameraHandler.Initialize(transform, this);

        LockCursor();
        _initialized = true;
    }

    private void Start()
    {
        if (!_initialized) return;

        // Published in Start so all OnEnable subscriptions (e.g. EnemySpawner)
        // are guaranteed to be registered before this event fires.
        EventBus.Publish(new PlayerRegisteredEvent { PlayerTransform = transform });
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
