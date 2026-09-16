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
    [SerializeField] private RectTransform latchKnob;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Quality Buttons (Rustic Theme)")]
    [SerializeField] private Button lowQualityBtn;
    [SerializeField] private Button medQualityBtn;
    [SerializeField] private Button highQualityBtn;
    [SerializeField] private Button ultraQualityBtn;
    [SerializeField] private RectTransform qualityHighlightRing;

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

        if (lowQualityBtn != null) lowQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(0));
        if (medQualityBtn != null) medQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(1));
        if (highQualityBtn != null) highQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(2));
        if (ultraQualityBtn != null) ultraQualityBtn.onClick.AddListener(() => SetQualityLevelDirect(3));
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
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            if (latchKnob != null) latchKnob.anchoredPosition = new Vector2(Screen.fullScreen ? 24f : -24f, 0f);
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
        if (latchKnob != null) latchKnob.anchoredPosition = new Vector2(isFullscreen ? 24f : -24f, 0f);
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