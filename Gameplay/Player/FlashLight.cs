using UnityEngine;

public class FlashLight : MonoBehaviour
{
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private float smoothTime = 0.02f;
    [Space]
    [SerializeField] private float minSpotAngle = 40f;
    [SerializeField] private float maxSpotAngle = 90f;
    [SerializeField] private float minIntensity = 5f;
    [SerializeField] private float maxIntensity = 50f;
    [Space]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float sphereRadius = 0.1f;
    [SerializeField] private float castBackDistance = 0.5f;
    [SerializeField] private Vector3 flashlightOffset = new Vector3(0.2f, -0.3f, 0.4f);

    private AudioManager _audioManager;
    private Vector3 _eulerAngles;
    private Vector3 _smoothDampVelocity;
    private Light spotLight;
    private float lookDistance;

    public void Initialize(Transform target)
    {
        spotLight = GetComponent<Light>();

        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
        _smoothDampVelocity = Vector3.zero;
        _audioManager = AudioManager.Instance;
    }

    public bool IsOn => spotLight != null && spotLight.enabled;

    public void UpdateRotation(Vector3 target, float deltaTime)
    {
        _eulerAngles = Vector3.SmoothDamp(
            current: _eulerAngles,
            target: target,
            currentVelocity: ref _smoothDampVelocity,
            smoothTime: smoothTime,
            maxSpeed: Mathf.Infinity,
            deltaTime: deltaTime
        );

        transform.eulerAngles = _eulerAngles;
    }

    public void UpdateLightning(float deltaTime) {
        if (!IsOn) return;

        Vector3 origin = transform.position;
        RaycastHit hitInfo;
        int maxDistance = 50;

        if (Physics.Raycast(origin, transform.forward, out hitInfo, maxDistance)) {
            lookDistance = Mathf.Min(maxDistance, Mathf.Pow(Vector3.Distance(origin, hitInfo.point), 2));
            Debug.DrawRay(origin, transform.forward * maxDistance, Color.green);
        } else {
            if (lookDistance != maxDistance) lookDistance = maxDistance;
            Debug.DrawRay(origin, transform.forward * maxDistance, Color.red);
        }

        float distanceFactor = Mathf.InverseLerp(0, maxDistance, lookDistance);
        float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, distanceFactor);
        spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, deltaTime / 0.1f);
        float targetAngle = Mathf.Lerp(maxSpotAngle, minSpotAngle, distanceFactor);
        spotLight.spotAngle = Mathf.Lerp(spotLight.spotAngle, targetAngle, deltaTime / 0.1f);
        spotLight.innerSpotAngle = Mathf.Min(spotLight.spotAngle, spotLight.spotAngle * 0.5f + 10);
    }

    public void UpdatePosition(Transform target)
    {
        Transform camTransform = playerCamera.GetCameraTransform();

        Vector3 desiredPos = target.position +
            (camTransform.right * flashlightOffset.x) +
            (Vector3.up * flashlightOffset.y) +
            (camTransform.forward * flashlightOffset.z);
        Vector3 origin = desiredPos + camTransform.forward * -castBackDistance;
        
        if (Physics.SphereCast(
            origin,
            sphereRadius,
            camTransform.forward,
            out RaycastHit hit,
            castBackDistance,
            collisionLayers
        ))
        {
            castStartPoint = origin;
            castDistancePoint = origin + camTransform.forward * hit.distance;

            desiredPos -= camTransform.forward * (castBackDistance - hit.distance);
        }
        transform.position = desiredPos;
    }

    private Vector3 castStartPoint;
    private Vector3 castDistancePoint;

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(castStartPoint, 0.01f);
        Gizmos.DrawWireSphere(castDistancePoint, sphereRadius);
        Gizmos.DrawLine(castStartPoint, castDistancePoint);
    }

    public void PlayerLightSwitch(bool? @bool = null) 
    {
        _audioManager.PlaySound("Click", 0.5f, pitch: Utils.GetRandOfRange(0.8f, 1.2f), group: _audioManager.sfxGroup);
        LightSwitch(@bool);
    }
    public void LightSwitch(bool? @bool = null) => spotLight.enabled = @bool ?? !spotLight.enabled;
}
