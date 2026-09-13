using System;
using UnityEngine;

public static class SettingsManager
{
    // ========== Ключи PlayerPrefs ==========
    private const string GraphicsKey = "GraphicsQualityLevel";
    private const string VSyncKey = "GraphicsVSyncCount";
    private const string MasterVolumeKey = "AudioMasterVolume";
    private const string AmbientVolumeKey = "AudioAmbientVolume";
    private const string SFXVolumeKey = "AudioSFXVolume";
    private const string LanguageKey = "LanguageCode";

    // ========== Публичные свойства ==========
    public static int CurrentGraphics { get; private set; }
    public static int CurrentVSync { get; private set; }
    public static float CurrentAudioMaster { get; private set; }
    public static float CurrentAudioAmbient { get; private set; }
    public static float CurrentAudioSFX { get; private set; }
    public static Localization.Lang CurrentLanguage { get; private set; }

    // ========== Ивенты ==========
    public static event Action OnLanguageChanged;

    // ========== Инициализация ==========
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeAndApply()
    {
        // ========== Quality ==========
        int defaultQuality = QualitySettings.names.Length - 1;
        CurrentGraphics = PlayerPrefs.GetInt(GraphicsKey, defaultQuality);
        QualitySettings.SetQualityLevel(CurrentGraphics, true);

        // ========== VSync ==========
        CurrentVSync = PlayerPrefs.GetInt(VSyncKey, 1);
        QualitySettings.vSyncCount = CurrentVSync;

        // ========== Audio ==========
        CurrentAudioMaster = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        CurrentAudioAmbient = PlayerPrefs.GetFloat(AmbientVolumeKey, 1f);
        CurrentAudioSFX = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

        // ========== Localization ==========
        Localization.Lang defaultLang = Localization.Lang.EN;
        if (Application.systemLanguage == SystemLanguage.Russian || 
            Application.systemLanguage == SystemLanguage.Ukrainian || 
            Application.systemLanguage == SystemLanguage.Belarusian)
        { defaultLang = Localization.Lang.RU; }
        string savedLangStr = PlayerPrefs.GetString(LanguageKey, defaultLang.ToString());
        if (Enum.TryParse<Localization.Lang>(savedLangStr, true, out var parsedLang)) CurrentLanguage = parsedLang;
        else CurrentLanguage = defaultLang; // На случай, если в Prefs записался мусор
        Localization.LoadLanguage(CurrentLanguage);
    }

    // ========== Quality ==========
    public static void QualityApply(int quality)
    {
        CurrentGraphics = quality;
        QualitySettings.SetQualityLevel(quality, true);
        PlayerPrefs.SetInt(GraphicsKey, quality);
        PlayerPrefs.Save(); 
        Debug.Log(String.Format("Настройки качества: {0}", quality));
    }

    // ========== VSync ==========
    public static void VSyncApply(int count)
    {
        CurrentVSync = count;
        QualitySettings.vSyncCount = count;
        PlayerPrefs.SetInt(VSyncKey, count);
        PlayerPrefs.Save();
        Debug.Log(String.Format("Настройки VSync: {0}", count));
    }

    // ========== Audio ==========
    public static void AudioApply(int volume, AudioGroup group)
    {
        float volume01 = volume / 100f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(volume01, group);
        switch (group)
        {
            case AudioGroup.Master:
                CurrentAudioMaster = volume01;
                PlayerPrefs.SetFloat(MasterVolumeKey, volume01);
                break;
            case AudioGroup.Ambient:
                CurrentAudioAmbient = volume01;
                PlayerPrefs.SetFloat(AmbientVolumeKey, volume01);
                break;
            case AudioGroup.SFX:
                CurrentAudioSFX = volume01;
                PlayerPrefs.SetFloat(SFXVolumeKey, volume01);
                break;
        }
        PlayerPrefs.Save();
        Debug.Log(String.Format("Настройки громкости {0}: {1} | 0..1: {2}", group.ToString(), volume, volume01));
    }

    // ========== Localization ==========
    public static void LanguageApply(Localization.Lang langCode)
    {
        CurrentLanguage = langCode;
        Localization.LoadLanguage(langCode); // Перезаписываем кэш в локализации
        
        PlayerPrefs.SetString(LanguageKey, langCode.ToString());
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
        Debug.Log(String.Format("Настройки языка изменены на: {0}", langCode));
    }
}
