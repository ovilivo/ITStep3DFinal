using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementHandler : MonoBehaviour
{
    private float _moveSpeed;
    private float _acceleration;
    private float _airControl;
    private float _rotationSpeed;
    private float _jumpForce;
    private float _gravity;

    public Vector2 LookInput { get; private set; }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private CharacterController _controller;
    private Animator _animator;
    private Transform _cam;

    private Vector2 _moveInput;
    private Vector3 _velocity;
    private Vector3 _moveVelocity;

    public void Initialize(Transform cameraTransform, CharacterData data)
    {
        _cam = cameraTransform;
        _moveSpeed = data.moveSpeed;
        _acceleration = data.acceleration;
        _airControl = data.airControl;
        _rotationSpeed = data.rotationSpeed;
        _jumpForce = data.jumpForce;
        _gravity = data.gravity;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // The visual model is a child; this script owns position, so the Animator must never move the root.
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (_cam == null) return;

        HandleMovement();
        ApplyGravity();

        _controller.Move((_moveVelocity + _velocity) * Time.deltaTime);

        UpdateAnimator();
    }

    /// <summary>Clears all speed (including the fall speed), e.g. after the player is put back at the spawn point.</summary>
    public void ResetMotion()
    {
        _velocity = Vector3.zero;
        _moveVelocity = Vector3.zero;
    }

    // ── Input callbacks ───────────────────────────────────────────────────────

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => LookInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed && _controller.isGrounded && !PauseManager.IsPaused)
        {
            _velocity.y = _jumpForce;
            AudioManager.Play(SfxId.Jump);
            if (_animator != null)
                _animator.SetTrigger(JumpHash);
        }
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        _animator.SetFloat(SpeedHash, _moveVelocity.magnitude);
        _animator.SetBool(IsGroundedHash, _controller.isGrounded);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        Vector3 input = new Vector3(_moveInput.x, 0f, _moveInput.y);

        Vector3 camForward = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized;

        Vector3 targetDir = camForward * input.z + camRight * input.x;

        float control = _controller.isGrounded ? _acceleration : _airControl;
        _moveVelocity = Vector3.Lerp(_moveVelocity, targetDir * _moveSpeed, control * Time.deltaTime);

        if (_moveVelocity.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_moveVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += _gravity * Time.deltaTime;
    }
}
