using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsMenu : MonoBehaviour
{
    public static bool IsSettingsMenuOpen { get; private set; }

    private VisualElement _settingsMenuContainer;

    // ========== Labels ==========
    private Label _graphicsLabel;
    private Label _qualityLabel;
    private Label _audioLabel;
    private Label _audioMasterLabel;
    private Label _audioAmbientLabel;
    private Label _audioSFXLabel;
    private Label _languageLabel;

    // ========== Graphics ==========
    private SliderInt _qualitySlider;
    private Label _qualityValueLabel;
    private Toggle _vSyncToggle;
    
    // ========== Audio ==========
    private SliderInt _audioMasterSlider;
    private Label _audioMasterValueLabel;
    private SliderInt _audioAmbientSlider;
    private Label _audioAmbientValueLabel;
    private SliderInt _audioSFXSlider;
    private Label _audioSFXValueLabel;

    // ========== Language ==========
    private RadioButtonGroup _languageRadioGroup;

    void OnEnable()
    {
        IsSettingsMenuOpen = false;
        _settingsMenuContainer = GetComponent<UIDocument>().rootVisualElement;

        AssignElements();
        InitUIValues();
        RegisterCallbacks();
        HideMenu();

        UpdateLabelsLocalization();
        SettingsManager.OnLanguageChanged += UpdateLabelsLocalization;
    }

    void OnDisable()
    {
        SettingsManager.OnLanguageChanged -= UpdateLabelsLocalization;
    }

    private void AssignElements()
    {
        // ========== LocalizationElements ==========
        _graphicsLabel = _settingsMenuContainer.Q<Label>("graphics-label");
        _qualityLabel = _settingsMenuContainer.Q<Label>("quality-label");

        _audioLabel = _settingsMenuContainer.Q<Label>("audio-label");
        _audioMasterLabel = _settingsMenuContainer.Q<Label>("audio-master-label");
        _audioAmbientLabel = _settingsMenuContainer.Q<Label>("audio-ambient-label");
        _audioSFXLabel = _settingsMenuContainer.Q<Label>("audio-sfx-label");

        _languageLabel = _settingsMenuContainer.Q<Label>("language-label");

        // ========== ChangeElements ==========
        _qualitySlider = _settingsMenuContainer.Q<SliderInt>("quality-slider");
        _qualityValueLabel = _settingsMenuContainer.Q<Label>("quality-value-label");
        _vSyncToggle = _settingsMenuContainer.Q<Toggle>("vsync-toggle");

        _audioMasterSlider = _settingsMenuContainer.Q<SliderInt>("audio-master-slider");
        _audioMasterValueLabel = _settingsMenuContainer.Q<Label>("audio-master-value-label");
        _audioAmbientSlider = _settingsMenuContainer.Q<SliderInt>("audio-ambient-slider");
        _audioAmbientValueLabel = _settingsMenuContainer.Q<Label>("audio-ambient-value-label");
        _audioSFXSlider = _settingsMenuContainer.Q<SliderInt>("audio-sfx-slider");
        _audioSFXValueLabel = _settingsMenuContainer.Q<Label>("audio-sfx-value-label");

        _languageRadioGroup = _settingsMenuContainer.Q<RadioButtonGroup>("language-radio-group");
    }

    private void InitUIValues()
    {
        // ========== Quality ==========
        int savedQuality = SettingsManager.CurrentGraphics;
        _qualitySlider.highValue = QualitySettings.names.Length - 1;
        _qualitySlider.value = savedQuality;
        _qualityValueLabel.text = QualitySettings.names[savedQuality];

        // ========== VSync ==========
        int savedVSync = SettingsManager.CurrentVSync;
        _vSyncToggle.value = savedVSync != 0;

        // ========== Audio ==========
        int savedMasterVolume = Mathf.RoundToInt(SettingsManager.CurrentAudioMaster * 100);
        _audioMasterSlider.value = savedMasterVolume;
        _audioMasterValueLabel.text = savedMasterVolume.ToString();

        int savedAmbientVolume = Mathf.RoundToInt(SettingsManager.CurrentAudioAmbient * 100);
        _audioAmbientSlider.value = savedAmbientVolume;
        _audioAmbientValueLabel.text = savedAmbientVolume.ToString();

        int savedSFXVolume = Mathf.RoundToInt(SettingsManager.CurrentAudioSFX * 100);
        _audioSFXSlider.value = savedSFXVolume;
        _audioSFXValueLabel.text = savedSFXVolume.ToString();
        
        // ========== Language ==========
        Localization.Lang savedLanguage = SettingsManager.CurrentLanguage;
        _languageRadioGroup.value = (int)savedLanguage;
    }

    private void UpdateLabelsLocalization()
    {
        _graphicsLabel.text = Localization.Get("graphics_settings_label");
        _qualityLabel.text = Localization.Get("graphics_quality");
        _audioLabel.text = Localization.Get("audio_settings_label");
        _audioMasterLabel.text = Localization.Get("audio_master_label");
        _audioAmbientLabel.text = Localization.Get("audio_ambient_label");
        _audioSFXLabel.text = Localization.Get("audio_sfx_label");
        _languageLabel.text = Localization.Get("language_settings_label");
    }

    private void RegisterCallbacks()
    {
        _qualitySlider.RegisterValueChangedCallback(evt => OnValueChangedQuality(evt.newValue));
        _vSyncToggle.RegisterValueChangedCallback(evt => OnValueChangedVSync(evt.newValue));
        
        _audioMasterSlider.RegisterValueChangedCallback(evt => OnValueChangedAudio(evt.newValue, _audioMasterValueLabel, AudioGroup.Master));
        _audioAmbientSlider.RegisterValueChangedCallback(evt => OnValueChangedAudio(evt.newValue, _audioAmbientValueLabel, AudioGroup.Ambient));
        _audioSFXSlider.RegisterValueChangedCallback(evt => OnValueChangedAudio(evt.newValue, _audioSFXValueLabel, AudioGroup.SFX));

        _languageRadioGroup.RegisterValueChangedCallback(evt => OnValueChangedLanguage(evt.newValue));
    }

    // ========== Quality ==========
    private void OnValueChangedQuality(int quality)
    {
        SettingsManager.QualityApply(quality);
        _qualityValueLabel.text = QualitySettings.names[quality];
    }

    // ========== VSync ==========
    private void OnValueChangedVSync(bool isWithVSync)
    {
        int n = isWithVSync ? 1 : 0;
        SettingsManager.VSyncApply(n);
    }

    // ========== Audio ==========
    private void OnValueChangedAudio(int volume, Label label, AudioGroup group)
    {
        SettingsManager.AudioApply(volume, group);
        label.text = volume.ToString();
    }

    // ========== Language ==========
    private void OnValueChangedLanguage(int languageIndex)
    {
        Localization.Lang lang = (Localization.Lang)languageIndex;
        SettingsManager.LanguageApply(lang);
    }

    // ========== Скрытие меню ==========
    public void ShowMenu()
    {
        IsSettingsMenuOpen = true;
        _settingsMenuContainer.style.display = DisplayStyle.Flex; // Делаем видимым
        _settingsMenuContainer.focusable = true;                  // Разрешаем фокус/взаимодействие
    }

    public void HideMenu()
    {
        IsSettingsMenuOpen = false;
        _settingsMenuContainer.style.display = DisplayStyle.None; // Полностью скрываем и отключаем клики
        _settingsMenuContainer.focusable = false;
    }
}
