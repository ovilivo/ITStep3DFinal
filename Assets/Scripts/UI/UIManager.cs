using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Game Over")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private string _loseMessage = "You lost";
    [SerializeField] private string _winMessage = "You Win";
    [Tooltip("Panel background on a loss. Captured from the scene's panel colour at start if left unchanged.")]
    [SerializeField] private Color _losePanelColor = new Color(0.8f, 0.1f, 0.1f, 0.6f);
    [SerializeField] private Color _winPanelColor = new Color(0.1f, 0.6f, 0.2f, 0.6f);

    [Header("On-screen Messages")]
    [Tooltip("How long each message stays on screen, in real seconds.")]
    [SerializeField] private float _messageSeconds = 5f;

    [Header("Game Start Message (shown when the run timer starts)")]
    [FormerlySerializedAs("_speechText")]
    [SerializeField] private TextMeshProUGUI _gameStartText;
    [SerializeField] private string _gameStartMessage = "No, Not you, Leave me ALONE";

    [Header("Boss Spawn Message (shown when the boss spawns)")]
    [Tooltip("A separate text object from the game start message, so the two messages can never overwrite each other.")]
    [SerializeField] private TextMeshProUGUI _bossSpawnText;
    [Tooltip("{0} is replaced with the player's name: first letter capitalized, the rest lowercase.")]
    [SerializeField] private string _bossSpawnFormat = "{0} spawned";

    [Header("Fall Message (shown when the player falls out of the map)")]
    [Tooltip("Its own text object, so it can show together with the other two messages.")]
    [SerializeField] private TextMeshProUGUI _fallText;
    [SerializeField] private string _fallMessage = "Escaping this ain't that easy";

    [Header("Crystal Counter")]
    [SerializeField] private TextMeshProUGUI _crystalCounterText;
    [Tooltip("{0} = crystals collected, {1} = crystals required.")]
    [SerializeField] private string _crystalCounterFormat = "Crystals: {0}/{1}";

    [Header("Restart")]
    [SerializeField] private string _introSceneName = "IntroScene";
    [Tooltip("Short pause after pressing Restart so the click sound is heard before the scene changes.")]
    [SerializeField] private float _sceneLoadDelay = 0.15f;

    private bool _restarting;
    private Coroutine _gameStartRoutine;
    private Coroutine _bossRoutine;
    private Coroutine _fallRoutine;
    private Image _panelImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_gameOverPanel == null)
        {
            Debug.LogError("[UIManager] Game Over Panel is not assigned!", this);
            return;
        }

        _panelImage = _gameOverPanel.GetComponent<Image>();
        if (_panelImage != null)
            _losePanelColor = _panelImage.color; // keep the existing look for a loss

        _gameOverPanel.SetActive(false);

        if (_gameStartText != null && _gameStartRoutine == null)
            _gameStartText.gameObject.SetActive(false);

        if (_bossSpawnText != null && _bossRoutine == null)
            _bossSpawnText.gameObject.SetActive(false);

        if (_fallText != null && _fallRoutine == null)
            _fallText.gameObject.SetActive(false);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(Restart);
    }

    private void OnDestroy()
    {
        if (_restartButton != null)
            _restartButton.onClick.RemoveListener(Restart);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Subscribe<BossPhaseStartedEvent>(OnBossPhaseStarted);
        EventBus.Subscribe<PlayerFellOffMapEvent>(OnPlayerFellOffMap);
        EventBus.Subscribe<CrystalCountChangedEvent>(OnCrystalCountChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Unsubscribe<BossPhaseStartedEvent>(OnBossPhaseStarted);
        EventBus.Unsubscribe<PlayerFellOffMapEvent>(OnPlayerFellOffMap);
        EventBus.Unsubscribe<CrystalCountChangedEvent>(OnCrystalCountChanged);
    }

    private void OnPlayerFellOffMap(PlayerFellOffMapEvent e)
    {
        if (_fallText == null) return;

        if (_fallRoutine != null)
            StopCoroutine(_fallRoutine);
        _fallRoutine = StartCoroutine(ShowTimedMessage(_fallText, _fallMessage, _messageSeconds));
    }

    private void OnCrystalCountChanged(CrystalCountChangedEvent e)
    {
        if (_crystalCounterText != null)
            _crystalCounterText.text = string.Format(_crystalCounterFormat, e.Collected, e.Required);
    }

    private void OnRunStarted(RunStartedEvent e)
    {
        if (_gameStartText == null) return;

        if (_gameStartRoutine != null)
            StopCoroutine(_gameStartRoutine);
        _gameStartRoutine = StartCoroutine(ShowTimedMessage(_gameStartText, _gameStartMessage, _messageSeconds));
    }

    /// <summary>Published by WaveManager in the same frame the boss is activated (WaveBossAI listens to the same event).</summary>
    private void OnBossPhaseStarted(BossPhaseStartedEvent e)
    {
        if (_bossSpawnText == null) return;

        if (_bossRoutine != null)
            StopCoroutine(_bossRoutine);
        string message = string.Format(_bossSpawnFormat, FormatPlayerName(GameData.PlayerName));
        _bossRoutine = StartCoroutine(ShowTimedMessage(_bossSpawnText, message, _messageSeconds));
    }

    /// <summary>"nika", "NIKA" and "nIkA" all become "Nika". An empty name falls back to the default name.</summary>
    private static string FormatPlayerName(string rawName)
    {
        string name = string.IsNullOrWhiteSpace(rawName) ? GameData.DefaultPlayerName : rawName.Trim();
        return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
    }

    /// <summary>
    /// Shows <paramref name="message"/> on its own text object for <paramref name="seconds"/>, then hides it.
    /// Uses game time (not real time), so a message stops counting down while the pause menu is open.
    /// </summary>
    private static IEnumerator ShowTimedMessage(TextMeshProUGUI target, string message, float seconds)
    {
        target.text = message;
        target.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        target.gameObject.SetActive(false);
    }

    private void HideMessages()
    {
        if (_gameStartRoutine != null)
        {
            StopCoroutine(_gameStartRoutine);
            _gameStartRoutine = null;
        }
        if (_gameStartText != null)
            _gameStartText.gameObject.SetActive(false);

        if (_bossRoutine != null)
        {
            StopCoroutine(_bossRoutine);
            _bossRoutine = null;
        }
        if (_bossSpawnText != null)
            _bossSpawnText.gameObject.SetActive(false);

        if (_fallRoutine != null)
        {
            StopCoroutine(_fallRoutine);
            _fallRoutine = null;
        }
        if (_fallText != null)
            _fallText.gameObject.SetActive(false);
    }

    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        switch (e.State)
        {
            case GameState.GameOver:
                ShowResult(_loseMessage, _losePanelColor);
                break;
            case GameState.Victory:
                ShowResult(_winMessage, _winPanelColor);
                break;
        }
    }

    private void ShowResult(string message, Color panelColor)
    {
        HideMessages();

        if (_panelImage != null)
            _panelImage.color = panelColor;

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);

        if (_resultText != null)
            _resultText.text = message;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Returns to the intro scene with a fresh run (time scale and run data reset).</summary>
    private void Restart()
    {
        if (_restarting) return;
        _restarting = true;

        AudioManager.Play(SfxId.UiClick);
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        // Real time: the run has ended with the time scale at 0.
        yield return new WaitForSecondsRealtime(_sceneLoadDelay);

        if (TimeManager.Instance != null)
            TimeManager.Instance.ResetTimeScale();
        else
            Time.timeScale = 1f;

        GameData.Reset();
        SceneManager.LoadScene(_introSceneName);
    }
}
