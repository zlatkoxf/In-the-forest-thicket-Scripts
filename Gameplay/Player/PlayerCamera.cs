using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    public Camera Camera { get => _camera; }

    [SerializeField] private bool lookEnabled = true;
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float smoothTime = 0.02f;
    [Space]
    [SerializeField] private float zoomFov = 20f;
    [SerializeField] private float zoomLerp = 15f;
    [SerializeField] private bool zoomEnabled = false;

    [Header("Плавный поворот камеры в точку (Magnet)")]
    [SerializeField] private bool targetLookEnabled = false;
    [SerializeField] private float attractionForce = 5f;
    [SerializeField] private float targetDamping = 1f;

    private float defaultFov = 60f;
    private float currentSensitivity;

    private Vector3 _eulerAngles;
    private Vector3 _targetEulerAngles;
    private Vector3 _smoothDampVelocity;

    private Vector3 _forcedTargetWorldPosition = Vector3.zero;
    public Vector3 ForcedTargetWorldPosition { get => _forcedTargetWorldPosition; set => _forcedTargetWorldPosition = value; }

    public void Initialize(Transform target)
    {
        defaultFov = _camera.fieldOfView;
        transform.position = target.position;
        _eulerAngles = transform.eulerAngles;
        _targetEulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }

    public void UpdateRotation(float deltaTime, CameraInput input)
    {
        float fovRatio = _camera.fieldOfView / defaultFov;
        currentSensitivity = sensitivity * fovRatio;

        if (targetLookEnabled) ApplyTargetAttraction(deltaTime);
        if (lookEnabled) 
        { 
            _targetEulerAngles += new Vector3(-input.Look.y, input.Look.x) * currentSensitivity;
            _targetEulerAngles.x = Mathf.Clamp(_targetEulerAngles.x, -80f, 80f);
        } 

        _eulerAngles = Vector3.SmoothDamp(
            current: _eulerAngles,
            target: _targetEulerAngles,
            currentVelocity: ref _smoothDampVelocity,
            smoothTime: smoothTime,
            maxSpeed: Mathf.Infinity,
            deltaTime: deltaTime
        );

        transform.eulerAngles = _eulerAngles;
    }

    public void UpdateState(float delta)
    {
        _camera.fieldOfView = Mathf.Lerp(
            _camera.fieldOfView,
            zoomEnabled ? zoomFov : defaultFov, delta * zoomLerp
        );
    }

    public void SetTargetLock(bool enable, Vector3 worldPosition = default)
    {
        targetLookEnabled = enable;
        if (enable) _forcedTargetWorldPosition = worldPosition;
    }

    private void ApplyTargetAttraction(float deltaTime)
    {
        Vector3 direction = _forcedTargetWorldPosition - transform.position;
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 forcedEuler = lookRotation.eulerAngles;
        float forcedX = forcedEuler.x > 180f ? forcedEuler.x - 360f : forcedEuler.x;
        float currentTargetX = _targetEulerAngles.x > 180f ? _targetEulerAngles.x - 360f : _targetEulerAngles.x;
        float deltaX = forcedX - currentTargetX;
        float deltaY = Mathf.DeltaAngle(_targetEulerAngles.y, forcedEuler.y);
        float dynamicForce = attractionForce / (1f + targetDamping * deltaTime);

        _targetEulerAngles.x += deltaX * dynamicForce * deltaTime;
        _targetEulerAngles.y += deltaY * dynamicForce * deltaTime;
    }

    public void ForceSetLookDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 euler = lookRotation.eulerAngles;
        if (euler.x > 180f) euler.x -= 360f;
        euler.x = Mathf.Clamp(euler.x, -80f, 80f);
        _targetEulerAngles = euler; _eulerAngles = euler; _smoothDampVelocity = Vector3.zero;

        transform.eulerAngles = euler;
    }

    public void ToggleLook(bool? @bool = null) => lookEnabled = @bool ?? !lookEnabled;
    public void ToggleTargetLook(bool? @bool = null) => targetLookEnabled = @bool ?? !targetLookEnabled;
    public void ToggleZoom(bool? @bool = null) => zoomEnabled = @bool ?? !zoomEnabled;
    public Vector3 GetTargetEulerAngles() => _targetEulerAngles;
    public Vector3 GetEulerAngles() => _eulerAngles;
    public Transform GetCameraTransform() => _camera.transform;
}
