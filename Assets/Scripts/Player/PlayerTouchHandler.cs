using UnityEngine;

/// <summary>
/// Detects enemies (anything implementing <see cref="ILethalOnTouch"/>) overlapping the player's
/// capsule and publishes <see cref="PlayerTouchedEnemyEvent"/> once. The run ends on the first touch.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerTouchHandler : MonoBehaviour
{
    private const int HitBufferSize = 16;

    [SerializeField] private LayerMask _touchLayers;
    [Tooltip("Extra radius added to the player's capsule when checking for contact.")]
    [SerializeField] private float _touchPadding = 0.05f;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];
    private CharacterController _controller;
    private bool _touched;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_touched) return;

        float radius = _controller.radius + _touchPadding;
        float halfSegment = Mathf.Max(0f, _controller.height * 0.5f - _controller.radius);
        Vector3 center = transform.TransformPoint(_controller.center);
        Vector3 bottom = center + Vector3.down * halfSegment;
        Vector3 top = center + Vector3.up * halfSegment;

        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _hitBuffer, _touchLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].GetComponentInParent<ILethalOnTouch>() == null) continue;

            _touched = true;
            EventBus.Publish(new PlayerTouchedEnemyEvent());
            return;
        }
    }
}
