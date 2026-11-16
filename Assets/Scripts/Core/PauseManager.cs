using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ESC (or the gamepad Start button) opens the pause menu. While it is open the game is frozen:
/// Time.timeScale is 0 (so the run timer, enemies, spawners, chests, animations and physics stop) and
/// audio is paused. Resume restores everything. It cannot be opened once the run has ended
/// (You lost / You Win), and it only lives in the main scene.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    /// <summary>True while the pause menu is open. Other systems use this to ignore input.</summary>
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _returnToDesktopButton;
    [Tooltip("Short pause after pressing 'Return to Desktop' so the click sound is heard first.")]
    [SerializeField] private float _quitDelay = 0.15f;

    [Header("Input")]
    [Tooltip("Action in the project's Input System asset (Assets/InputSystem_Actions): Escape and gamepad Start.")]
    [SerializeField] private string _pauseActionPath = "Player/Pause";

    private InputAction _pauseAction;
    private float _timeScaleBeforePause = 1f;
    private bool _quitting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsPaused = false;
    }

    private void Start()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    private void OnEnable()
    {
        _pauseAction = InputSystem.actions != null ? InputSystem.actions.FindAction(_pauseActionPath) : null;
        if (_pauseAction == null)
            Debug.LogError($"[PauseManager] Input action '{_pauseActionPath}' not found in the project's Input System actions.", this);
        else
        {
            _pauseAction.performed += OnPausePerformed;
            _pauseAction.Enable();
        }

        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
        if (_returnToDesktopButton != null) _returnToDesktopButton.onClick.AddListener(OnReturnToDesktopClicked);
    }

    private void OnDisable()
    {
        if (_pauseAction != null)
            _pauseAction.performed -= OnPausePerformed;

        if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (_returnToDesktopButton != null) _returnToDesktopButton.onClick.RemoveListener(OnReturnToDesktopClicked);
    }

    private void OnDestroy()
    {
        // Never leave the game frozen if this object goes away while paused (scene change, quit).
        if (IsPaused)
        {
            SetTimeScale(_timeScaleBeforePause);
            AudioListener.pause = false;
            IsPaused = false;
        }
        if (Instance == this)
            Instance = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext context) => TogglePause();

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || !CanPause) return;

        IsPaused = true;
        _timeScaleBeforePause = Time.timeScale;
        SetTimeScale(0f);
        AudioListener.pause = true;

        if (_pausePanel != null)
            _pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        SetTimeScale(_timeScaleBeforePause);
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>The menu can only open during a run that is still going (not on You lost / You Win).</summary>
    private static bool CanPause => GameManager.Instance != null && !GameManager.Instance.IsRunEnded;

    private void OnResumeClicked()
    {
        AudioManager.Play(SfxId.UiClick);
        Resume();
    }

    private void OnReturnToDesktopClicked()
    {
        if (_quitting) return;
        _quitting = true;

        AudioManager.Play(SfxId.UiClick);
        StartCoroutine(QuitAfterDelay());
    }

    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_quitDelay); // real time: the game is frozen
        AppQuit.Quit();
    }

    private static void SetTimeScale(float scale)
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeScale(scale);
        else
            Time.timeScale = scale;
    }
}
