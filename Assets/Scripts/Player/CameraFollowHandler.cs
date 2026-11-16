using UnityEngine;

public class CameraFollowHandler : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float _distance = 6f;
    [SerializeField] private float _height = 3f;
    [SerializeField] private float _followSpeed = 20f;

    [Header("Orbit")]
    [SerializeField] private float _sensitivity = 0.5f;
    [SerializeField] private Vector2 _pitchLimits = new Vector2(-20f, 60f);

    [Header("Collision")]
    [SerializeField] private LayerMask _collisionLayers;
    [SerializeField] private float _wallOffset = 0.2f;

    private const float FocusPointHeight = 1.2f;
    private const float SnapThreshold = 0.5f;

    private Transform _target;
    private ILookInputProvider _inputProvider;
    private float _yaw;
    private float _pitch;
    private bool _frozen;
    private bool _snapNextFrame;

    public void Initialize(Transform targetTransform, ILookInputProvider inputProvider)
    {
        _target = targetTransform;
        _inputProvider = inputProvider;

        Vector3 angles = transform.eulerAngles;
        _pitch = angles.x;
        _yaw = angles.y;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<PlayerFellOffMapEvent>(OnPlayerFellOffMap);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<PlayerFellOffMapEvent>(OnPlayerFellOffMap);
    }

    // The player was teleported back to the spawn point: jump the camera there instead of flying 200 m through the map.
    private void OnPlayerFellOffMap(PlayerFellOffMapEvent e) => _snapNextFrame = true;

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (e.State.IsRunEnded())
            _frozen = true;
    }

    private void LateUpdate()
    {
        // Paused: mouse movement must not turn the camera while the menu is open.
        if (_frozen || PauseManager.IsPaused || _target == null || _inputProvider == null) return;

        Vector2 input = _inputProvider.LookInput;

        _yaw += input.x * _sensitivity;
        _pitch -= input.y * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, _pitchLimits.x, _pitchLimits.y);

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 idealPosition = _target.position + rot * new Vector3(0f, _height, -_distance);
        Vector3 focusPoint = _target.position + Vector3.up * FocusPointHeight;

        Vector3 finalPosition = idealPosition;
        if (Physics.Linecast(focusPoint, idealPosition, out RaycastHit hit, _collisionLayers))
            finalPosition = hit.point + hit.normal * _wallOffset;

        bool follow = !_snapNextFrame && Vector3.Distance(transform.position, finalPosition) > SnapThreshold;
        transform.position = follow
            ? Vector3.Lerp(transform.position, finalPosition, _followSpeed * Time.deltaTime)
            : finalPosition;
        _snapNextFrame = false;

        transform.LookAt(focusPoint);
    }
}
