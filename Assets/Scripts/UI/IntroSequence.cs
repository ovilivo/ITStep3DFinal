using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroSequence : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject nameInputContainer;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    [Header("Captions")]
    [SerializeField] private string _firstCaption = "On this day, you died.";
    [SerializeField] private string _secondCaption = "Unknown: \"Hey! You still have a job to do.\"";
    [SerializeField] private string _namePrompt = "What is your name?";
    [SerializeField] private float _firstCaptionSeconds = 3f;
    [SerializeField] private float _secondCaptionSeconds = 4f;

    [Header("Scene")]
    [SerializeField] private string _gameSceneName = "SampleScene";
    [Tooltip("Short pause after pressing Start so the click sound is heard before the scene changes.")]
    [SerializeField] private float _sceneLoadDelay = 0.15f;

    private bool _starting;

    private void Start()
    {
        nameInputContainer.SetActive(false);
        captionText.text = "";

        // Remove white box from input field
        nameInputField.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        nameInputField.onValueChanged.AddListener(SanitizeInput);
        nameInputField.onSubmit.AddListener(OnNameSubmitted);
        if (_startButton != null)
            _startButton.onClick.AddListener(StartGame);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(QuitGame);

        StartCoroutine(PlayIntro());
    }

    private void OnDestroy()
    {
        nameInputField.onValueChanged.RemoveListener(SanitizeInput);
        nameInputField.onSubmit.RemoveListener(OnNameSubmitted);
        if (_startButton != null)
            _startButton.onClick.RemoveListener(StartGame);
        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(QuitGame);
    }

    private void SanitizeInput(string value)
    {
        string clean = Regex.Replace(value, @"[^a-zA-Z]", "");
        if (clean != value)
            nameInputField.SetTextWithoutNotify(clean);
    }

    private IEnumerator PlayIntro()
    {
        captionText.text = _firstCaption;
        yield return new WaitForSeconds(_firstCaptionSeconds);

        captionText.text = _secondCaption;
        yield return new WaitForSeconds(_secondCaptionSeconds);

        captionText.text = _namePrompt;
        nameInputContainer.SetActive(true);
        nameInputField.ActivateInputField();
    }

    private void OnNameSubmitted(string value) => StartGame();

    /// <summary>Stores the typed name (an empty name falls back to the default) and loads the game scene.</summary>
    private void StartGame()
    {
        if (_starting) return;
        _starting = true;

        GameData.PlayerName = nameInputField.text;
        AudioManager.Play(SfxId.UiClick);
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    /// <summary>Quit button: in a build this closes the game, in the Editor it stops Play Mode.</summary>
    private void QuitGame()
    {
        if (_starting) return;
        _starting = true; // blocks Start/Quit from being used twice

        AudioManager.Play(SfxId.UiClick);
        StartCoroutine(QuitAfterDelay());
    }

    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_sceneLoadDelay);
        AppQuit.Quit();
    }

    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_sceneLoadDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(_gameSceneName);
    }
}
