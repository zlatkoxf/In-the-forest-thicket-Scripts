using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioGroup { Master, Music, Ambient, SFX }

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    [SerializeField] public AudioMixer audioMixer;
    [SerializeField] public AudioMixerGroup masterGroup;
    [SerializeField] public AudioMixerGroup musicGroup;
    [SerializeField] public AudioMixerGroup ambientGroup;
    [SerializeField] public AudioMixerGroup sfxGroup;

    [Header("Clip Library Asset")]
    [SerializeField] private AudioClipLibrary clipLibrary;

    private Dictionary<string, AudioClip> clipDict = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        { 
            Instance = this; 
            //DontDestroyOnLoad(gameObject); 
            InitVolumes();
            BuildClipDictionary();
        }
        else { Destroy(gameObject); return; }
    }

    private void InitVolumes()
    {
        // Вызываем инициализацию, так как свойства в SettingsManager публичные
        SetVolume(SettingsManager.CurrentAudioMaster, AudioGroup.Master);
        SetVolume(SettingsManager.CurrentAudioAmbient, AudioGroup.Ambient);
        SetVolume(SettingsManager.CurrentAudioSFX, AudioGroup.SFX);
    }

    // =============== Словарь клипов ===============
    private void BuildClipDictionary()
    {
        clipDict.Clear();
        if (clipLibrary == null)
        { Debug.LogError("AudioClipLibrary not assigned in AudioManager!"); return; }
        foreach (var item in clipLibrary.Clips)
        { if (!string.IsNullOrEmpty(item.key) && item.clip != null && !clipDict.ContainsKey(item.key))
            clipDict.Add(item.key, item.clip); }
    }

    /// <summary>
    /// Получить клип по ключу из библиотеки.
    /// </summary>
    public AudioClip GetClip(string key)
    {
        if (clipDict.TryGetValue(key, out AudioClip clip))
            return clip;
        Debug.LogError($"AudioClip with key '{key}' not found in library!");
        return null;
    }

    // =============== Воспроизведение ===============

    // Звук, который играет в цикле (не уничтожается) //2D - прямо в ушах
    public AudioSource PlayLoopSound(
        string clipKey, 
        float volume = 1f, 
        float pitch = 1.0f,
        AudioMixerGroup group = null)
    {
        AudioClip clip = GetClip(clipKey);
        if (clip == null) {
            Debug.Log("Не удалось воспроизвести звук: " + clipKey);
            return null;
        }

        AudioSource audioSource = CreateAudioSource(clip, volume, pitch, loop: true, outputGroup: group ?? masterGroup);
        audioSource.Play();
        return audioSource;
    }

    // Звук, который играет в цикле, в указанной позиции //3D - с позиционированием или 2D
    public AudioSource PlayLoopSound(
        string clipKey, 
        Vector3 pos, 
        float volume = 1f, 
        float minDistance = 1, 
        float maxDistance = 500, 
        float pitch = 1.0f, 
        float spatialBlend = 1,
        AudioMixerGroup group = null)
    {
        AudioClip clip = GetClip(clipKey);
        if (clip == null) {
            Debug.Log("Не удалось воспроизвести звук: " + clipKey);
            return null;
        }

        // Передаем loop: true в CreateAudioSource
        AudioSource audioSource = CreateAudioSource(
            clip, volume, pitch, minDistance, maxDistance, pos, spatialBlend, 
            loop: true, 
            outputGroup: group ?? masterGroup
        );
        
        audioSource.Play();
        return audioSource; // Возвращаем, чтобы потом можно было вызвать Destroy вручную
    }

    // Однократный звук без привязки к позиции //2D - прямо в ушах
    public void PlaySound(
        string clipKey, 
        float volume = 1f, 
        float pitch = 1.0f,
        AudioMixerGroup group = null)
    {
        AudioClip clip = GetClip(clipKey);
        if (clip == null) {
            Debug.Log("Не удалось воспроизвести звук: " + clipKey);
            return;
        }

        AudioSource audioSource = CreateAudioSource(clip, volume, pitch, outputGroup: group ?? masterGroup);
        audioSource.Play();

        // Уничтожаем компонент через время проигрывания
        float duration = clip.length / Mathf.Abs(pitch);
        Destroy(audioSource, duration);
    }

    // Однократный звук в указанной позиции //3D - с позиционированием или 2D
    /// <param name="spatialBlend">От 0 до 1 где 0 - 2D, 1 - 3D.</param>
    public void PlaySound(
        string clipKey, 
        Vector3 pos, 
        float volume = 1f, 
        float minDistance = 1, 
        float maxDistance = 500, 
        float pitch = 1.0f, 
        float spatialBlend = 1,
        AudioMixerGroup group = null)
    {
        AudioClip clip = GetClip(clipKey);
        if (clip == null) {
            Debug.Log("Не удалось воспроизвести звук: " + clipKey);
            return;
        }

        AudioSource audioSource = CreateAudioSource(clip, volume, pitch, minDistance, maxDistance, pos, spatialBlend, outputGroup: group ?? masterGroup);
        audioSource.Play();

        // Уничтожаем весь игровой объект со звуком
        float duration = clip.length / Mathf.Abs(pitch);
        Destroy(audioSource.gameObject, duration);
    }

    // =============== Общий метод для создания и настройки AudioSource ===============
    private AudioSource CreateAudioSource(
        AudioClip clip, 
        float volume, 
        float pitch, 
        float minDistance = 1, 
        float maxDistance = 500, 
        Vector3? position = null, 
        float spatialBlend = 1, 
        bool loop = false,
        AudioMixerGroup outputGroup = null)
    {
        GameObject go;
        AudioSource audioSource;

        if (position.HasValue) //если 3D
        {
            // Создаём отдельный объект в указанной позиции
            go = new GameObject("AudioSource");
            go.transform.position = position.Value;
            audioSource = go.AddComponent<AudioSource>();
            audioSource.spatialBlend = spatialBlend; // 3D звук
        }
        else
        {
            // Используем текущий объект менеджера
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0; // 2D звук
        }

        // Общие настройки
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.clip = clip;
        audioSource.pitch = pitch;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;

        if (outputGroup != null)
            audioSource.outputAudioMixerGroup = outputGroup;
        else if (masterGroup != null)
            audioSource.outputAudioMixerGroup = masterGroup; // fallback

        return audioSource;
    }

    // =============== Управление громкостью через AudioMixer ===============
    public void SetVolume(float volume01, AudioGroup group)
    {
        string groupName = group.ToString();
        if (audioMixer != null)
            audioMixer.SetFloat(groupName + "Volume", LinearToDecibel(volume01));
    }

    // =============== Преобразование линейного значения (0..1) в децибелы (-80..0) ===============
    private float LinearToDecibel(float linear)
    {
        linear = Mathf.Clamp(linear, 0f, 10f);
        return linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
    }

    /// <summary>
    /// Ставит на паузу или снимает с паузы выбранные группы через изменение Pitch.
    /// </summary>
    public void TogglePauseGroups(AudioGroup[] groups, bool isPaused)
    {
        float targetPitch = isPaused ? 0f : 1f;
        foreach (var group in groups) SetGroupPitch(group, targetPitch);
    }
    /// <summary>
    /// Изменяет Pitch (скорость воспроизведения) для конкретной группы.
    /// </summary>
    public void SetGroupPitch(AudioGroup group, float pitchValue)
    {
        string parameterName = group.ToString() + "Pitch"; 
        audioMixer.SetFloat(parameterName, pitchValue);
    }
}