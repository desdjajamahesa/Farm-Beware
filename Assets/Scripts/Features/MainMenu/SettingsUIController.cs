using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider soundVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider volumeSlider; // Backward compatibility
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Audio Buttons")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button sfxButton;

    [Header("Display & Navigation")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button backButton;

    [Header("Legacy References")]
    [SerializeField] private Toggle muteAllToggle;
    [SerializeField] private RectTransform muteKnob;
    [SerializeField] private Slider qualitySlider;
    [SerializeField] private RectTransform latchKnob;
    [SerializeField] private RectTransform fullscreenKnob;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Button lowQualityBtn;
    [SerializeField] private Button medQualityBtn;
    [SerializeField] private Button highQualityBtn;
    [SerializeField] private Button ultraQualityBtn;
    [SerializeField] private RectTransform qualityHighlightRing;

    private bool isSoundMuted = false;
    private bool isMusicMuted = false;
    private bool isSfxMuted = false;
    private Resolution[] availableResolutions;

    private SettingsButtonAnimator musicBtnAnim;
    private SettingsButtonAnimator soundBtnAnim;
    private SettingsButtonAnimator sfxBtnAnim;

    private void Awake()
    {
        AutoResolveReferences();
        PopulateResolutions();
        PopulateQuality();

        // 1. Sound (Master) volume
        Slider mainSound = soundVolumeSlider != null ? soundVolumeSlider : volumeSlider;
        if (mainSound != null)
        {
            float savedSound = PlayerPrefs.GetFloat("SoundVolume", 1.0f);
            mainSound.value = savedSound;
            mainSound.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        // 2. Music volume
        if (musicVolumeSlider != null)
        {
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            musicVolumeSlider.value = savedMusic;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // 3. SFX volume
        if (sfxVolumeSlider != null)
        {
            float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            sfxVolumeSlider.value = savedSfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        // Setup Button Animators & Listeners
        SetupAudioButtons();

        // 4. Fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        // 5. Back Button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Legacy Mute All
        if (muteAllToggle != null)
        {
            isSoundMuted = PlayerPrefs.GetInt("MuteAll", 0) == 1;
            muteAllToggle.isOn = isSoundMuted;
            muteAllToggle.onValueChanged.AddListener(OnMuteAllToggled);
        }

        // Legacy Quality slider
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

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void AutoResolveReferences()
    {
        if (musicVolumeSlider == null)
            musicVolumeSlider = FindChildComponent<Slider>("MusicSlider", "MusicVolumeSlider", "MusicRow/Slider");
        if (soundVolumeSlider == null)
            soundVolumeSlider = FindChildComponent<Slider>("SoundSlider", "SoundVolumeSlider", "SoundRow/Slider");
        if (sfxVolumeSlider == null)
            sfxVolumeSlider = FindChildComponent<Slider>("SFXSlider", "SFXVolumeSlider", "SFXRow/Slider");

        if (musicButton == null)
            musicButton = FindChildComponent<Button>("MusicButton", "Btn_Music", "MusicRow/Button");
        if (soundButton == null)
            soundButton = FindChildComponent<Button>("SoundButton", "Btn_Sound", "SoundRow/Button");
        if (sfxButton == null)
            sfxButton = FindChildComponent<Button>("SFXButton", "Btn_SFX", "SFXRow/Button");

        if (fullscreenToggle == null)
            fullscreenToggle = FindChildComponent<Toggle>("FullscreenToggle", "FullscreenRow/Toggle");
        if (backButton == null)
            backButton = FindChildComponent<Button>("BackButton");
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        foreach (string n in names)
        {
            Transform t = transform.Find(n);
            if (t != null)
            {
                T comp = t.GetComponent<T>();
                if (comp != null) return comp;
            }
        }
        return null;
    }

    private void SetupAudioButtons()
    {
        isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        isSoundMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
        isSfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        if (musicButton != null)
        {
            musicBtnAnim = musicButton.GetComponent<SettingsButtonAnimator>() ?? musicButton.gameObject.AddComponent<SettingsButtonAnimator>();
            musicBtnAnim.SetAudioFeedbackType(SettingsButtonAnimator.AudioFeedbackType.MusicChime);
            musicBtnAnim.SetMutedState(isMusicMuted);
            musicButton.onClick.AddListener(OnMusicButtonClicked);
        }

        if (soundButton != null)
        {
            soundBtnAnim = soundButton.GetComponent<SettingsButtonAnimator>() ?? soundButton.gameObject.AddComponent<SettingsButtonAnimator>();
            soundBtnAnim.SetAudioFeedbackType(SettingsButtonAnimator.AudioFeedbackType.SoundTone);
            soundBtnAnim.SetMutedState(isSoundMuted);
            soundButton.onClick.AddListener(OnSoundButtonClicked);
        }

        if (sfxButton != null)
        {
            sfxBtnAnim = sfxButton.GetComponent<SettingsButtonAnimator>() ?? sfxButton.gameObject.AddComponent<SettingsButtonAnimator>();
            sfxBtnAnim.SetAudioFeedbackType(SettingsButtonAnimator.AudioFeedbackType.SFXPop);
            sfxBtnAnim.SetMutedState(isSfxMuted);
            sfxButton.onClick.AddListener(OnSfxButtonClicked);
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        // Sound
        Slider mainSound = soundVolumeSlider != null ? soundVolumeSlider : volumeSlider;
        if (mainSound != null)
        {
            float savedSound = PlayerPrefs.GetFloat("SoundVolume", 1.0f);
            mainSound.value = savedSound;
            if (!isSoundMuted) AudioListener.volume = savedSound;
            else AudioListener.volume = 0f;
            UpdateVolumeLabel(savedSound);
        }

        // Music
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        }

        // SFX
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        }

        // Fullscreen
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            var anim = fullscreenToggle.GetComponent<FullscreenToggleAnimator>();
            if (anim != null) anim.ApplyInstantState(Screen.fullScreen);
        }

        // Mute state visuals
        if (musicBtnAnim != null) musicBtnAnim.SetMutedState(isMusicMuted);
        if (soundBtnAnim != null) soundBtnAnim.SetMutedState(isSoundMuted);
        if (sfxBtnAnim != null) sfxBtnAnim.SetMutedState(isSfxMuted);
    }

    private void OnSoundVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SoundVolume", value);
        if (!isSoundMuted)
        {
            AudioListener.volume = value;
        }
        UpdateVolumeLabel(value);

        // If volume was dragged up while muted, automatically unmute
        if (isSoundMuted && value > 0.05f)
        {
            isSoundMuted = false;
            PlayerPrefs.SetInt("SoundMuted", 0);
            if (soundBtnAnim != null) soundBtnAnim.SetMutedState(false);
            AudioListener.volume = value;
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (isMusicMuted && value > 0.05f)
        {
            isMusicMuted = false;
            PlayerPrefs.SetInt("MusicMuted", 0);
            if (musicBtnAnim != null) musicBtnAnim.SetMutedState(false);
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (isSfxMuted && value > 0.05f)
        {
            isSfxMuted = false;
            PlayerPrefs.SetInt("SFXMuted", 0);
            if (sfxBtnAnim != null) sfxBtnAnim.SetMutedState(false);
        }
    }

    private void OnMusicButtonClicked()
    {
        isMusicMuted = !isMusicMuted;
        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
        if (musicBtnAnim != null) musicBtnAnim.SetMutedState(isMusicMuted);

        if (isMusicMuted)
        {
            // Mute music
        }
        else
        {
            if (musicVolumeSlider != null && musicVolumeSlider.value <= 0.05f)
                musicVolumeSlider.value = 0.75f;
        }
    }

    private void OnSoundButtonClicked()
    {
        isSoundMuted = !isSoundMuted;
        PlayerPrefs.SetInt("SoundMuted", isSoundMuted ? 1 : 0);
        if (soundBtnAnim != null) soundBtnAnim.SetMutedState(isSoundMuted);

        if (isSoundMuted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            Slider mainSound = soundVolumeSlider != null ? soundVolumeSlider : volumeSlider;
            if (mainSound != null)
            {
                if (mainSound.value <= 0.05f) mainSound.value = 0.8f;
                AudioListener.volume = mainSound.value;
            }
            else
            {
                AudioListener.volume = 1f;
            }
        }
    }

    private void OnSfxButtonClicked()
    {
        isSfxMuted = !isSfxMuted;
        PlayerPrefs.SetInt("SFXMuted", isSfxMuted ? 1 : 0);
        if (sfxBtnAnim != null) sfxBtnAnim.SetMutedState(isSfxMuted);

        if (!isSfxMuted && sfxVolumeSlider != null && sfxVolumeSlider.value <= 0.05f)
        {
            sfxVolumeSlider.value = 0.8f;
        }
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void OnBackButtonClicked()
    {
        // Try finding MainMenuController to perform clean animated close
        var mainMenu = GetComponentInParent<MainMenuController>();
        if (mainMenu == null) mainMenu = FindObjectOfType<MainMenuController>();

        if (mainMenu != null)
        {
            // MainMenuController has OnSettingsBackClicked
            var backField = typeof(MainMenuController).GetMethod("OnSettingsBackClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (backField != null)
            {
                backField.Invoke(mainMenu, null);
                return;
            }
        }

        gameObject.SetActive(false);
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
            volumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnMuteAllToggled(bool muted)
    {
        isSoundMuted = muted;
        PlayerPrefs.SetInt("MuteAll", muted ? 1 : 0);
        AudioListener.volume = muted ? 0f : (soundVolumeSlider != null ? soundVolumeSlider.value : 1f);
        if (soundBtnAnim != null) soundBtnAnim.SetMutedState(muted);
    }

    private void OnQualitySliderChanged(float value)
    {
        int level = Mathf.RoundToInt(value);
        SetQualityLevelDirect(level);
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
            options.Add(r.width + " x " + r.height);
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                currentIndex = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void PopulateQuality()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }
}