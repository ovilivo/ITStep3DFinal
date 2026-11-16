using UnityEngine;

/// <summary>A crystal pickup. Collecting it counts toward the run goal (see CrystalCollectedEvent).</summary>
public class GemCollect : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private bool _collected;

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag(PlayerTag)) return;

        _collected = true;
        EventBus.Publish(new CrystalCollectedEvent());
        Destroy(gameObject);
    }
}
