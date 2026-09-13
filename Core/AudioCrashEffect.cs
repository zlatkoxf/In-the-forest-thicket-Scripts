using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class AudioCrashEffect : MonoBehaviour
{
    [Header("Настройки эффекта")]
    [Tooltip("Длина петли зависания в миллисекундах")]
    public float loopDurationMs = 100f; 
    public bool isCrashed = false;
    public bool IsCrashed { set => isCrashed = value; }

    private float[] ringBuffer;
    private int writeIndex = 0;
    private int readIndex = 0;
    private int bufferLength;
    private int currentChannels = 2; // Значение по умолчанию (стерео)
    
    // Кэшируем частоту дискретизации, чтобы не вызывать её из аудиопотока
    private int cachedSampleRate; 
    private float lastLoopDurationMs;

    public static AudioCrashEffect Instance { get; private set; }

    void Start()
    {
        Instance = this;
        // Вызываем в главном потоке — это безопасно
        cachedSampleRate = AudioSettings.outputSampleRate;
        lastLoopDurationMs = loopDurationMs;

        InitBuffer(currentChannels);
    }

    void Update()
    {
        // Проверяем изменение длительности в главном потоке игры
        if (Mathf.Abs(loopDurationMs - lastLoopDurationMs) > 0.01f)
        {
            lastLoopDurationMs = loopDurationMs;
            InitBuffer(currentChannels);
        }
    }

    // Метод для безопасного выделения памяти в главном потоке
    void InitBuffer(int channelsCount)
    {
        bufferLength = Mathf.CeilToInt((cachedSampleRate * (loopDurationMs / 1000f)) * channelsCount);
        ringBuffer = new float[bufferLength];
        writeIndex = 0;
        readIndex = 0;
    }

    // Этот метод вызывается в отдельном аудиопотоке
    void OnAudioFilterRead(float[] data, int channelsCount)
    {
        if (channelsCount != currentChannels)
        {
            currentChannels = channelsCount;
        }

        // Если буфер еще не создан (например, при старте), пропускаем обработку
        if (ringBuffer == null || ringBuffer.Length == 0) return;

        for (int i = 0; i < data.Length; i++)
        {
            // Защита на случай, если массив data длиннее, чем наш ringBuffer
            if (i >= ringBuffer.Length) break; 

            if (!isCrashed)
            {
                // Режим записи
                ringBuffer[writeIndex] = data[i];
                writeIndex = (writeIndex + 1) % ringBuffer.Length;
                readIndex = writeIndex; 
            }
            else
            {
                // Режим краша
                data[i] = ringBuffer[readIndex];
                readIndex = (readIndex + 1) % ringBuffer.Length;
            }
        }
    }
}
