using UnityEngine;

// ── Game State ────────────────────────────────────────────────────────────────

public struct GameStateChangedEvent
{
    public GameState State;
}

// ── Player ────────────────────────────────────────────────────────────────────

/// <summary>Published by PlayerController in Start (after all OnEnable subscriptions) so all listeners are guaranteed to receive it.</summary>
public struct PlayerRegisteredEvent
{
    public Transform PlayerTransform;
}

public struct PlayerDiedEvent { }

/// <summary>
/// Published by PlayerFallRespawn right after the player fell out of the map and was put back at the spawn point.
/// WaveManager takes the time penalty, UIManager shows the message, the camera snaps to the player.
/// </summary>
public struct PlayerFellOffMapEvent
{
    public float TimePenaltySeconds;
}

/// <summary>Published by PlayerTouchHandler the first time an <see cref="ILethalOnTouch"/> collider touches the player.</summary>
public struct PlayerTouchedEnemyEvent { }

// ── Enemies ───────────────────────────────────────────────────────────────────

/// <summary>Published by EnemyHealthHandler on death. Carries the GameObject for pool release.</summary>
public struct EnemyDiedEvent
{
    public GameObject EnemyObject;
}

// ── Loot ──────────────────────────────────────────────────────────────────────

/// <summary>Published by GemCollect when the player picks up a crystal.</summary>
public struct CrystalCollectedEvent { }

/// <summary>Published by ObjectiveManager whenever the collected crystal count changes (and once at the start).</summary>
public struct CrystalCountChangedEvent
{
    public int Collected;
    public int Required;
}

// ── Wave ──────────────────────────────────────────────────────────────────────

public struct BossPhaseStartedEvent { }

/// <summary>Published by WaveManager when the run timer starts.</summary>
public struct RunStartedEvent { }

/// <summary>Published by WaveManager once, when the run timer reaches the end of the run.</summary>
public struct RunTimerExpiredEvent { }
