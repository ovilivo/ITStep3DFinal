public static class GameData
{
    public const string DefaultPlayerName = "Player";

    private static string _playerName = DefaultPlayerName;

    public static string BossName { get; set; } = "Boss";
    public static int Score { get; set; }

    /// <summary>Name typed on the intro scene. Never empty: falls back to <see cref="DefaultPlayerName"/>.</summary>
    public static string PlayerName
    {
        get => _playerName;
        set => _playerName = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();
    }

    public static void Reset()
    {
        BossName = "Boss";
        Score = 0;
        PlayerName = DefaultPlayerName;
    }
}
