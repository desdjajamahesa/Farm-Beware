using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteAllToggle;
    [SerializeField] private RectTransform muteKnob;
    [SerializeField] private Slider volumeSlider; // Backward compatibility
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Display")]
    [SerializeField] private Slider qualitySlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private RectTransform latchKnob;
    [SerializeField] private RectTransform fullscreenKnob;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Quality Buttons (Legacy / Optional)")]
    [SerializeField] private Button lowQualityBtn;
    [SerializeField] private Button medQualityBtn;
    [SerializeField] private Button highQualityBtn;
    [SerializeField] private Button ultraQualityBtn;
    [SerializeField] private RectTransform qualityHighlightRing;

    private Resolution[] availableResolutions;
    private bool isMuted = false;

    private const float KNOB_OFF_X = -18f;
    private const float KNOB_ON_X = 18f;

    void Awake()
    {
        PopulateResolutions();
        PopulateQuality();

        // Music volume
        Slider mainVol = musicVolumeSlider != null ? musicVolumeSlider : volumeSlider;
        if (mainVol != null)
        {
            float savedVol = PlayerPrefs.GetFloat("MusicVolume", AudioListener.volume);
            mainVol.value = savedVol;
            mainVol.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // SFX volume
        if (sfxVolumeSlider != null)
        {
            float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            sfxVolumeSlider.value = savedSfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        // Mute All
        if (muteAllToggle != null)
        {
            isMuted = PlayerPrefs.GetInt("MuteAll", 0) == 1;
            muteAllToggle.isOn = isMuted;
            muteAllToggle.onValueChanged.AddListener(OnMuteAllToggled);
            UpdateToggleKnob(muteKnob, isMuted);
        }

        // Quality slider (0 = Low, 1 = Med, 2 = High)
        if (qualitySlider != null)
        {
            int q = QualitySettings.GetQualityLevel();
            int maxQ = Mathf.Min(2, QualitySettings.names.Length - 1);
            qualitySlider.minValue = 0;
            qualitySlider.maxValue = maxQ;
            qualitySlider.wholeNumbers = true;
            qualitySlider.value = Mathf.Clamp(q, 0, maxQ);
            qualitySlider.onValueChanged.AddListener(OnQualitySliderChanged);
        }

        // Fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
            UpdateToggleKnob(fullscreenKnob ?? latchKnob, Screen.fullScreen);
        }

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (lowQualityBtn != null) lowQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(0));
        if (medQualityBtn != null) medQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(1));
        if (highQualityBtn != null) highQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(2));
        if (ultraQualityBtn != null) ultraQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(3));
    }

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        Slider mainVol = musicVolumeSlider != null ? musicVolumeSlider : volumeSlider;
        if (mainVol != null)
        {
            float savedVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            mainVol.value = savedVol;
            UpdateVolumeLabel(savedVol);
            if (!isMuted) AudioListener.volume = savedVol;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        if (muteAllToggle != null)
        {
            isMuted = PlayerPrefs.GetInt("MuteAll", 0) == 1;
            muteAllToggle.isOn = isMuted;
            UpdateToggleKnob(muteKnob, isMuted);
            if (isMuted) AudioListener.volume = 0f;
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            UpdateToggleKnob(fullscreenKnob ?? latchKnob, Screen.fullScreen);
        }

        if (qualitySlider != null)
        {
            int q = QualitySettings.GetQualityLevel();
            qualitySlider.value = Mathf.Clamp(q, 0, (int)qualitySlider.maxValue);
        }

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

        UpdateQualityButtonsHighlight(QualitySettings.GetQualityLevel());
    }

    private void UpdateToggleKnob(RectTransform knob, bool state)
    {
        if (knob != null)
        {
            knob.anchoredPosition = new Vector2(state ? KNOB_ON_X : KNOB_OFF_X, 0f);
        }
    }

    private void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
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
        var options = new List<string>(QualitySettings.names);
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

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (!isMuted)
        {
            AudioListener.volume = value;
        }
        UpdateVolumeLabel(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void OnMuteAllToggled(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt("MuteAll", muted ? 1 : 0);
        UpdateToggleKnob(muteKnob, muted);

        if (muted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            Slider mainVol = musicVolumeSlider != null ? musicVolumeSlider : volumeSlider;
            AudioListener.volume = mainVol != null ? mainVol.value : 1f;
        }
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
            volumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnQualitySliderChanged(float value)
    {
        int level = Mathf.RoundToInt(value);
        SetQualityLevelDirect(level);
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        UpdateToggleKnob(fullscreenKnob ?? latchKnob, isFullscreen);
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
        SetQualityLevelDirect(index);
    }

    public void SetQualityLevelDirect(int level)
    {
        int max = QualitySettings.names.Length - 1;
        int clamped = Mathf.Clamp(level, 0, max);
        QualitySettings.SetQualityLevel(clamped, true);
        if (qualityDropdown != null)
        {
            qualityDropdown.value = clamped;
            qualityDropdown.RefreshShownValue();
        }
        if (qualitySlider != null && Mathf.RoundToInt(qualitySlider.value) != clamped)
        {
            qualitySlider.value = Mathf.Clamp(clamped, 0, (int)qualitySlider.maxValue);
        }
        UpdateQualityButtonsHighlight(clamped);
    }

    private void UpdateQualityButtonsHighlight(int level)
    {
        Button targetBtn = level switch
        {
            0 => lowQualityBtn,
            1 => medQualityBtn,
            2 => highQualityBtn,
            _ => ultraQualityBtn != null ? ultraQualityBtn : highQualityBtn
        };

        if (qualityHighlightRing != null && targetBtn != null)
        {
            qualityHighlightRing.gameObject.SetActive(true);
            qualityHighlightRing.position = targetBtn.transform.position;
        }
    }
}