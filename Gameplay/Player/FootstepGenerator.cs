using UnityEngine;
using System;

public class FootstepGenerator : MonoBehaviour
{
    // Событие шага, которое передает наружу силу (громкость) от 0.0 до 1.0
    public event Action<float> OnStepTaken;

    [Header("References")]
    [Tooltip("Ссылка на скрипт игрока. Если не назначен, попытается найти на этом же GameObject.")]
    [SerializeField] private PlayerCharacter playerCharacter; // Замените PlayerCharacter на имя вашего класса персонажа

    [Header("Speed Settings")]
    [SerializeField] private float minPlayerSpeed = 0.5f;
    [SerializeField] private float maxPlayerSpeed = 10.0f;

    [Header("Frequency (Speed of Sin)")]
    [SerializeField] private float minStepFrequency = 0.1f; // Частота шагов при медленной ходьбе
    [SerializeField] private float maxStepFrequency = 2.5f; // Частота шагов при спринте

    [Header("Amplitude (Volume)")]
    [SerializeField] private float minStepAmplitude = 0.3f; // Громкость шага при минимальной скорости
    [SerializeField] private float maxStepAmplitude = 1.0f; // Громкость шага при спринте

    [Header("Smoothing")]
    [SerializeField] private float speedSmoothTime = 10f;

    private float _currentSpeedFactor;
    private float _rockingTime;
    
    // Переменные для поиска математического дна синусоиды
    private float _lastSinValue;
    private float _lastLastSinValue;

    private void Awake()
    {
        // Автоматический поиск компонента игрока, если забыли привязать в инспекторе
        if (playerCharacter == null) playerCharacter = GetComponent<PlayerCharacter>(); // Укажите ваш точный класс
    }

    private void Update()
    {
        if (playerCharacter == null) return;
        float currentRealSpeed = playerCharacter.GetVelocity().magnitude;
        bool isGrounded = playerCharacter.GetGroundingStatus().IsStableOnGround;
        UpdateGeneratorInternal(currentRealSpeed, isGrounded, Time.deltaTime);
    }

    public void UpdateGeneratorInternal(float currentRealSpeed, bool isGrounded, float deltaTime)
    {
        if (!isGrounded || currentRealSpeed < minPlayerSpeed) currentRealSpeed = 0f; // Если персонаж в воздухе, скорость для шагов приравниваем к нулю
        // 1. Плавно сглаживаем текущую скорость (аналог вашего RockingAmplitudeUpdate)
        _currentSpeedFactor = Mathf.Lerp(_currentSpeedFactor, currentRealSpeed, deltaTime * speedSmoothTime);
        if (_currentSpeedFactor <= 0.05f) // Если персонаж стоит и генератор полностью затих, сбрасываем фазу и выходим
        {
            _rockingTime = 0f;
            _lastSinValue = 0f;
            _lastLastSinValue = 0f;
            return;
        }
        // 2. Рассчитываем, где мы находимся на шкале от мин. до макс. скорости (0.0 .. 1.0)
        float speedNormalized = Mathf.InverseLerp(minPlayerSpeed, maxPlayerSpeed, _currentSpeedFactor);
        // 3. Динамически вычисляем частоту (скорость изменения синуса) на основе текущей скорости
        float currentFrequency = Mathf.Lerp(minStepFrequency, maxStepFrequency, speedNormalized);
        // 4. Наращиваем фазу (время синусоиды) с учетом частоты
        _rockingTime += deltaTime * currentFrequency * (2f * Mathf.PI); // Множитель 2 * PI нужен, чтобы частота (Frequency) соответствовала реальному количеству шагов в секунду
        // 5. Генерируем чистое значение синусоиды
        float currentSin = Mathf.Sin(_rockingTime);
        // 6. Ищем локальный минимум синусоиды (самое дно волны, момент удара ноги)
        float previousSin = _lastSinValue;
        if (previousSin < -0.8f && previousSin < _lastLastSinValue && currentSin > previousSin)
        {
            float finalVolume = Mathf.Lerp(minStepAmplitude, maxStepAmplitude, speedNormalized); // Динамически рассчитываем громкость (амплитуду) шага
            OnStepTaken?.Invoke(finalVolume); // Вызываем событие шага
        }
        // Сдвигаем историю для следующего кадра
        _lastLastSinValue = previousSin;
        _lastSinValue = currentSin;
    }
}
