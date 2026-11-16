using UnityEngine;

public enum GameState
{
    Playing,
    BossPhase,
    GameOver,   // the run was lost
    Victory     // the run was won
}

public static class GameStateExtensions
{
    /// <summary>True for the two final states (lost or won). Once reached, a run cannot change result.</summary>
    public static bool IsRunEnded(this GameState state) => state == GameState.GameOver || state == GameState.Victory;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    /// <summary>True once the run has ended. The result can only be decided once.</summary>
    public bool IsRunEnded => CurrentState.IsRunEnded();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable() => EventBus.Subscribe<PlayerTouchedEnemyEvent>(OnPlayerTouchedEnemy);
    private void OnDisable() => EventBus.Unsubscribe<PlayerTouchedEnemyEvent>(OnPlayerTouchedEnemy);

    private void Start()
    {
        TransitionTo(GameState.Playing);
    }

    public void StartBossPhase()
    {
        if (IsRunEnded) return;
        TransitionTo(GameState.BossPhase);
    }

    public void TriggerGameOver() => EndRun(GameState.GameOver);

    public void TriggerVictory() => EndRun(GameState.Victory);

    private void OnPlayerTouchedEnemy(PlayerTouchedEnemyEvent e) => TriggerGameOver();

    /// <summary>Single exit point for a run: ignores any call after the result has been decided.</summary>
    private void EndRun(GameState result)
    {
        if (IsRunEnded) return;

        TransitionTo(result);
        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeScale(0f);
    }

    private void TransitionTo(GameState newState)
    {
        CurrentState = newState;
        EventBus.Publish(new GameStateChangedEvent { State = newState });
    }
}
