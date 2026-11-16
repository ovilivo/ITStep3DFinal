using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A "Volume: 70%" display with a minus and a plus button. Changes the AudioManager's master volume
/// (sound effects and music) in steps; the choice is saved by the AudioManager. Used in the pause menu and on the intro screen.
/// </summary>
public class VolumeControl : MonoBehaviour
{
    [SerializeField] private Button _downButton;
    [SerializeField] private Button _upButton;
    [SerializeField] private TextMeshProUGUI _valueText;
    [Tooltip("How much one click changes the volume (0.1 = 10 percentage points).")]
    [SerializeField] private float _step = 0.1f;
    [Tooltip("{0} is replaced with the volume in percent.")]
    [SerializeField] private string _format = "Volume: {0}%";

    private void OnEnable()
    {
        if (_downButton != null) _downButton.onClick.AddListener(OnDownClicked);
        if (_upButton != null) _upButton.onClick.AddListener(OnUpClicked);
        Refresh();
    }

    private void OnDisable()
    {
        if (_downButton != null) _downButton.onClick.RemoveListener(OnDownClicked);
        if (_upButton != null) _upButton.onClick.RemoveListener(OnUpClicked);
    }

    // The AudioManager may not have loaded yet when this is enabled; show the right value once everything has started.
    private void Start() => Refresh();

    private void OnDownClicked() => Change(-_step);
    private void OnUpClicked() => Change(_step);

    private void Change(float delta)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;

        float step = Mathf.Max(_step, 0.01f);
        audio.MasterVolume = Mathf.Round((audio.MasterVolume + delta) / step) * step; // rounded: no float drift after many clicks
        Refresh();
        AudioManager.Play(SfxId.UiClick); // heard at the new volume
    }

    private void Refresh()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;

        float volume = audio.MasterVolume;
        if (_valueText != null)
            _valueText.text = string.Format(_format, Mathf.RoundToInt(volume * 100f));
        if (_downButton != null) _downButton.interactable = volume > 0.001f;
        if (_upButton != null) _upButton.interactable = volume < 0.999f;
    }
}
