using UnityEngine;
using TMPro;

/// <summary>
/// Owns the run timeline. Counts run time upward, shows the remaining time on screen,
/// and triggers the boss phase at a fixed run time.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Run Timeline")]
    [Tooltip("Length of the run in seconds. The on-screen countdown goes from this value to 0.")]
    [SerializeField] private float _runDuration = 60f;
    [Tooltip("Run time in seconds at which the boss spawns (measured from when the run timer starts).")]
    [SerializeField] private float _bossSpawnTime = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [Tooltip("The countdown turns red when this many seconds (or fewer) remain.")]
    [SerializeField] private float _warningSeconds = 10f;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = Color.red;

    private float _elapsed;
    private bool _bossSpawned;
    private bool _stopped;

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

    private void Start()
    {
        _elapsed = 0f;
        _timerText.gameObject.SetActive(true);
        UpdateCountdownUI();

        EventBus.Publish(new RunStartedEvent());
    }

    private void Update()
    {
        if (_stopped) return;

        _elapsed = Mathf.Min(_elapsed + Time.deltaTime, _runDuration);
        UpdateCountdownUI();
        EvaluateTimeline();
    }

    /// <summary>Boss and end-of-run checks against the run clock. Also used right after a time penalty.</summary>
    private void EvaluateTimeline()
    {
        if (!_bossSpawned && _elapsed >= _bossSpawnTime)
            TriggerBossPhase();

        if (_elapsed >= _runDuration)
        {
            _stopped = true;
            EventBus.Publish(new RunTimerExpiredEvent());
        }
    }

    /// <summary>
    /// The run clock moves forward by the penalty, i.e. the countdown drops by that many seconds. If that crosses the boss time,
    /// the boss spawns immediately; if it crosses the end of the run, the run ends.
    /// </summary>
    private void OnPlayerFellOffMap(PlayerFellOffMapEvent e)
    {
        if (_stopped) return;

        _elapsed = Mathf.Min(_elapsed + e.TimePenaltySeconds, _runDuration);
        UpdateCountdownUI();
        EvaluateTimeline();
    }

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        if (e.State.IsRunEnded())
            _stopped = true;
    }

    private void UpdateCountdownUI()
    {
        float timeRemaining = Mathf.Max(_runDuration - _elapsed, 0f);
        _timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
        _timerText.color = timeRemaining <= _warningSeconds ? _warningColor : _normalColor;
    }

    private void TriggerBossPhase()
    {
        _bossSpawned = true;

        GameManager.Instance?.StartBossPhase();
        EventBus.Publish(new BossPhaseStartedEvent());
    }
}
