using UnityEngine;

/// <summary>
/// Owns the run goal: collect <see cref="_crystalsRequired"/> crystals before the run timer expires.
/// Decides the result (win or lose by timeout) and broadcasts the crystal count for the UI.
/// GameManager accepts only the first result, so a run can never be both won and lost.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    [Header("Goal")]
    [SerializeField] private int _crystalsRequired = 5;

    private int _collected;

    public int Collected => _collected;
    public int Required => _crystalsRequired;

    private void OnEnable()
    {
        EventBus.Subscribe<CrystalCollectedEvent>(OnCrystalCollected);
        EventBus.Subscribe<RunTimerExpiredEvent>(OnRunTimerExpired);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CrystalCollectedEvent>(OnCrystalCollected);
        EventBus.Unsubscribe<RunTimerExpiredEvent>(OnRunTimerExpired);
    }

    private void Start() => PublishCount();

    private void OnCrystalCollected(CrystalCollectedEvent e)
    {
        if (RunEnded) return;

        _collected++;
        PublishCount();

        if (_collected >= _crystalsRequired)
            GameManager.Instance.TriggerVictory();
    }

    /// <summary>Time is up: the run is lost unless the goal was already reached (which would have ended the run).</summary>
    private void OnRunTimerExpired(RunTimerExpiredEvent e)
    {
        if (RunEnded) return;

        if (_collected >= _crystalsRequired)
            GameManager.Instance.TriggerVictory();
        else
            GameManager.Instance.TriggerGameOver();
    }

    private bool RunEnded => GameManager.Instance == null || GameManager.Instance.IsRunEnded;

    private void PublishCount()
    {
        EventBus.Publish(new CrystalCountChangedEvent { Collected = _collected, Required = _crystalsRequired });
    }
}
