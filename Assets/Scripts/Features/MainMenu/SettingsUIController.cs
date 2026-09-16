using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    private Resolution[] availableResolutions;

    void Awake()
    {
        PopulateResolutions();
        PopulateQuality();

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    void OnEnable()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            UpdateVolumeLabel(AudioListener.volume);
        }

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        if (resolutionDropdown != null)
        {
            int current = GetCurrentResolutionIndex();
            resolutionDropdown.value = current;
            resolutionDropdown.RefreshShownValue();
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }
    }

    private void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution r = availableResolutions[i];
            string option = r.width + " x " + r.height;
            options.Add(option);

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void PopulateQuality()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    private int GetCurrentResolutionIndex()
    {
        if (availableResolutions == null) return 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        UpdateVolumeLabel(value);
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
            volumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index < 0 || index >= availableResolutions.Length)
            return;

        Resolution r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
    }
}