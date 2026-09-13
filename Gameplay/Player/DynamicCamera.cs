using UnityEngine;

public class DynamicCamera : MonoBehaviour
{
    public System.Action<float> OnStepTaken; // Событие для вызова шага
    private float _lastSinValue; // Для отслеживания прохождения фазы
    private float _lastLastSinValue;

            // мягкая тряска камеры (дыхание)
    [SerializeField] private bool ambientRockingEnabled = true;
    [SerializeField] private float rockingSpeed = 0.2f;
    [SerializeField] private float rockingAmplitude = 0.4f;
    [Space] // тряска камеры в движении
    [SerializeField] private bool rockingEnabled = true;
    [SerializeField] private float verticalRockingSpeed = 1.5f;
    [SerializeField] private float verticalRockingAmplitude = 0.1f;
    [SerializeField] private float rockingSmoothTime = 10f;
    [Space] // динамический наклон в сторону движения
    [SerializeField] private bool tiltEnabled = true;
    [SerializeField] private float velocityPitchFactor = 1f;
    [SerializeField] private float velocityRollFactor = 2f;
    [SerializeField] private float tiltSmoothTime = 0.1f;
    [Space] // общее ограничение
    [SerializeField] private float maxTiltAngle = 10f;

    private float _cachedRockingTime;
    private float _currentRockingSpeed;
    private float _currentRockingAmplitude;
    private float _rockingTime;
    private Vector3 currentSpeedRocking;

    // просто удобное хранение значений для формата поворота камеры по двум осям vector2
    private Vector2 _currentTilt = Vector2.zero; 
    private Vector2 _targetTilt = Vector2.zero;
    private Vector2 _tiltVelocity = Vector2.zero;

    public void UpdateRotation(Transform playerTransform, Vector3 velocity, bool isStableOnGround, float deltaTime)
    {
        if (rockingEnabled) RockingAmplitudeUpdate(velocity.magnitude, isStableOnGround, deltaTime);
        Vector2 ambientRocking = ambientRockingEnabled ? AmbientRocking() : Vector3.zero;
        Vector3 speedRocking = rockingEnabled ? SpeedBasedVerticalRocking(deltaTime) : Vector3.zero;
        if (tiltEnabled) TiltUpdate(playerTransform, velocity, deltaTime);
        else { _currentTilt = Vector2.zero; }

        transform.localEulerAngles = new Vector3(
            Mathf.Clamp(ambientRocking.x + speedRocking.x + _currentTilt.x, -maxTiltAngle, maxTiltAngle),
            ambientRocking.y,
            Mathf.Clamp(-ambientRocking.y + speedRocking.y + _currentTilt.y, -maxTiltAngle, maxTiltAngle) // специально использовал -ambientRocking.y повторно.
        );
    }

    public void RockingAmplitudeUpdate(float playerSpeed, bool isStableOnGround, float deltaTime)
    {
        if (!isStableOnGround) playerSpeed = 0f;
        _currentRockingAmplitude = Mathf.Lerp(_currentRockingAmplitude, playerSpeed, deltaTime * rockingSmoothTime);
        _currentRockingSpeed = Mathf.Lerp(_currentRockingSpeed, playerSpeed, deltaTime * rockingSmoothTime);
    }
    private Vector2 AmbientRocking()
    {
        _cachedRockingTime += Time.deltaTime * rockingSpeed;
        return new Vector2(
            Mathf.Sin(_cachedRockingTime * 2) * rockingAmplitude,
            Mathf.Cos(_cachedRockingTime) * rockingAmplitude * 0.5f
        );
    }
    private Vector3 SpeedBasedVerticalRocking(float deltaTime)
    {
        _rockingTime += deltaTime * _currentRockingSpeed * verticalRockingSpeed;
        CheckFootstepTrigger(Mathf.Sin(_rockingTime)); // Footstep
        float angleX = Mathf.Sin(_rockingTime) * _currentRockingAmplitude * verticalRockingAmplitude;
        float angleY = Mathf.Sin(_rockingTime / 2) * _currentRockingAmplitude * verticalRockingAmplitude;
        currentSpeedRocking = new Vector3(angleX, angleY, 0f);
        return currentSpeedRocking;
    }
    private void TiltUpdate(Transform playerTransform, Vector3 velocity, float deltaTime)
    {
        float verticalVel = Vector3.Dot(velocity / 5, playerTransform.up);
        float lateralVel = Vector3.Dot(velocity / 5, playerTransform.right);
        _targetTilt.x = verticalVel * velocityPitchFactor;
        _targetTilt.y = -lateralVel * velocityRollFactor;
        _currentTilt.x = Mathf.SmoothDamp(_currentTilt.x, _targetTilt.x, ref _tiltVelocity.x, tiltSmoothTime, Mathf.Infinity, deltaTime);
        _currentTilt.y = Mathf.SmoothDamp(_currentTilt.y, _targetTilt.y, ref _tiltVelocity.y, tiltSmoothTime, Mathf.Infinity, deltaTime);
    }

    private void CheckFootstepTrigger(float currentSin)
    {
        if (_currentRockingSpeed <= 0.1f) return;
        float previousSin = _lastSinValue;
        if (previousSin < -0.8f && previousSin < _lastLastSinValue && currentSin > previousSin)
            { float stepForce = Mathf.Clamp01(_currentRockingAmplitude / 4.5f); OnStepTaken?.Invoke(stepForce); }
        _lastLastSinValue = previousSin;
        _lastSinValue = currentSin;
    }
}
