using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A chest starts fully closed. The first time the player touches it, the opening animation plays and,
/// when it finishes, one crystal pops out. An opened chest never drops a second crystal.
/// </summary>
public class ChestInteraction : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const float MinSeconds = 0.01f;

    [Header("Chest Animation")]
    [FormerlySerializedAs("chestAnimation")]
    [Tooltip("Legacy Animation component. Its 'Play Automatically' must stay OFF so the chest starts closed.")]
    [SerializeField] private Animation _chestAnimation;
    [FormerlySerializedAs("openClipName")]
    [SerializeField] private string _openClipName = "Open";
    [Tooltip("Playback speed of the open clip. The crystal drops when the clip ends.")]
    [SerializeField] private float _openSpeed = 2f;

    [Header("Crystal Drop")]
    [FormerlySerializedAs("gemPrefabs")]
    [SerializeField] private GameObject[] _crystalPrefabs;
    [FormerlySerializedAs("dropHeight")]
    [Tooltip("Height above the chest where the crystal appears.")]
    [SerializeField] private float _dropHeight = 0.8f;
    [Tooltip("Peak of the arc the crystal follows while popping out.")]
    [SerializeField] private float _popHeight = 1.2f;
    [Tooltip("Distance from the chest where the crystal lands.")]
    [SerializeField] private float _scatterRadius = 1.5f;
    [SerializeField] private float _popSeconds = 0.6f;
    [Tooltip("Height above the ground where the crystal rests so the player can pick it up.")]
    [SerializeField] private float _restHeight = 0.3f;
    [SerializeField] private float _groundProbeHeight = 3f;
    [SerializeField] private LayerMask _groundLayers;

    private bool _opened;

    private void OnTriggerEnter(Collider other)
    {
        if (_opened) return;
        if (!other.CompareTag(PlayerTag)) return;

        _opened = true;
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        AudioManager.Play(SfxId.ChestOpen);

        float wait = 0f;

        if (_chestAnimation != null)
        {
            AnimationState state = _chestAnimation[_openClipName];
            if (state != null)
            {
                state.speed = _openSpeed;
                _chestAnimation.Play(_openClipName);
                wait = state.length / Mathf.Max(_openSpeed, MinSeconds);
            }
        }

        yield return new WaitForSeconds(wait);
        DropCrystal();
    }

    private void DropCrystal()
    {
        if (_crystalPrefabs == null || _crystalPrefabs.Length == 0) return;

        GameObject prefab = _crystalPrefabs[Random.Range(0, _crystalPrefabs.Length)];
        if (prefab == null) return;

        GameObject crystal = Instantiate(prefab, transform.position + Vector3.up * _dropHeight, Quaternion.identity);

        // The crystal moves along a scripted arc and rests on the ground; physics would let its trigger collider fall through the floor.
        if (crystal.TryGetComponent(out Rigidbody body))
            body.isKinematic = true;

        StartCoroutine(PopCrystal(crystal.transform, FindLandingPoint()));
    }

    private Vector3 FindLandingPoint()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * _scatterRadius;
        Vector3 flat = transform.position + new Vector3(offset.x, 0f, offset.y);

        if (Physics.Raycast(flat + Vector3.up * _groundProbeHeight, Vector3.down, out RaycastHit hit,
                _groundProbeHeight * 2f, _groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * _restHeight;

        return flat + Vector3.up * _restHeight;
    }

    private IEnumerator PopCrystal(Transform crystal, Vector3 landing)
    {
        Vector3 start = crystal.position;
        float duration = Mathf.Max(_popSeconds, MinSeconds);

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            if (crystal == null) yield break; // collected mid-air

            Vector3 position = Vector3.Lerp(start, landing, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _popHeight;
            crystal.position = position;
            yield return null;
        }

        if (crystal != null)
            crystal.position = landing;
    }
}
