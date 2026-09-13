using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionOptimizer : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Точка отсчёта дистанции. Если пусто и autoFindPlayer включён, ищется объект с тегом Player.")]
    public Transform playerTarget;
    public bool autoFindPlayer = true;

    [Header("Сканирование сцены")]
    [Tooltip("Сканировать сцену автоматически при старте. Можно также вызвать ScanScene() кнопкой или с UI.")]
    public bool autoScanOnStart = true;
    [Tooltip("Оптимизируются только эти слои. Значение 'Everything' — все слои.")]
    public LayerMask onlyOptimizeLayers = ~0;
    [Tooltip("Слои, которые оптимизатор никогда не трогает.")]
    public LayerMask ignoreLayers = 0;
    [Tooltip("Оптимизировать и триггеры тоже.")]
    public bool includeTriggers = false;
    [Tooltip("Оптимизировать статичные коллайдеры (без Rigidbody). Включение/выключение статичных коллайдеров перестраивает статичный broadphase — обычно оставлять выключенным.")]
    public bool includeStaticColliders = false;
    [Tooltip("Не трогать коллайдеры на самом игроке и его детях.")]
    public bool protectPlayerColliders = true;
    [Tooltip("Периодически пересканировать сцену, чтобы подхватывать объекты, созданные во время игры.")]
    public bool autoRescan = false;
    public float rescanInterval = 10f;

    [Header("Дистанция и гистерезис")]
    [Tooltip("Дистанция, на которой выключенный коллайдер включается.")]
    public float enableDistance = 40f;
    [Tooltip("Дистанция, на которой включённый коллайдер выключается. Должна быть больше enableDistance — так избегается мерцание на границе радиуса.")]
    public float disableDistance = 55f;
    [Tooltip("Как часто проверять дистанции до коллайдеров.")]
    public float checkInterval = 2f;

    [Header("Предсказание движения (конус)")]
    [Tooltip("Включать коллайдеры в конусе впереди по направлению движения — они будут готовы до того, как игрок до них дойдёт.")]
    public bool usePrediction = true;
    [Tooltip("Брать скорость из PlayerCharacter (KCC). Если компонент не найден, скорость оценивается по перемещению трансформа.")]
    public bool useKccVelocity = true;
    [Tooltip("Проверять конус в 3D (включая вертикаль). Выключено — конус лежит в горизонтальной плоскости, как для наземного персонажа.")]
    public bool cone3D = false;
    [Tooltip("Длина конуса предсказания. Должна быть больше enableDistance, иначе конус не даёт выигрыша.")]
    public float coneLength = 70f;
    [Tooltip("Полный угол конуса в градусах (0-360). Конус смотрит вперёд по направлению движения.")]
    public float coneAngle = 50f;
    [Tooltip("Менять длину конуса по скорости игрока: чем быстрее движение, тем дальше вперёд включаются коллайдеры.")]
    public bool scaleConeWithSpeed = true;
    [Tooltip("Скорость, при которой конус достигает полной длины coneLength.")]
    public float coneSpeedReference = 10f;
    [Tooltip("Минимальная доля полной длины конуса при медленном движении (0-1).")]
    [Range(0f, 1f)] public float coneMinScale = 0.35f;
    [Tooltip("Запас гистерезиса на границе конуса (добавляется к длине и углу) — чтобы не мерцало при лёгком покачивании направления.")]
    public float coneDisableMargin = 10f;
    [Tooltip("Минимальная скорость, при которой конус ориентируется по скорости движения.")]
    public float minVelocityForPrediction = 0.5f;
    [Tooltip("Когда игрок стоит (скорость мала), направлять конус по взгляду (forward) вместо скорости.")]
    public bool useFacingWhenIdle = false;

    [Header("Производительность (анти-микрофриз)")]
    [Tooltip("Максимум коллайдеров, переключаемых за один кадр. Меньше — меньше риск микрофризов, но дольше применяется изменение.")]
    public int maxStateChangesPerFrame = 8;
    [Tooltip("Усыплять Rigidbody при выключении его коллайдера и будить при включении. Может 'заморозить' далёкие объекты в воздухе.")]
    public bool sleepInactiveRigidbodies = false;

    [Header("Отладка")]
    public bool debugLog = false;
    public bool drawDebugGizmos = false;
    [SerializeField] private int _trackedCount;
    [SerializeField] private int _activeCount;
    [SerializeField] private Vector3 _debugPredictionDir;

    private readonly List<Collider> _colliders = new List<Collider>();
    private readonly List<Collider> _toEnable = new List<Collider>();
    private readonly List<Collider> _toDisable = new List<Collider>();
    private readonly HashSet<Collider> _disabledByUs = new HashSet<Collider>();

    private Coroutine _routine;
    private WaitForSeconds _wait;
    private float _waitInterval = -1f;

    private PlayerCharacter _playerCharacter;
    private Vector3 _smoothedVelocity;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPlayerPosition;

    private void OnEnable()
    {
        if (Application.isPlaying && _routine == null)
        {
            _routine = StartCoroutine(OptimizeRoutine());
        }
        _hasLastPlayerPosition = false;
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        RestoreAll();
    }

    private void Start()
    {
        if (playerTarget == null && autoFindPlayer)
            TryFindPlayer();

        CachePlayerComponent();

        if (autoScanOnStart)
            ScanScene();
    }

    private void Update()
    {
        UpdatePredictionVelocity();
    }

    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            CachePlayerComponent();
        }
    }

    private void CachePlayerComponent()
    {
        _playerCharacter = null;
        if (playerTarget == null) return;

        _playerCharacter = playerTarget.GetComponent<PlayerCharacter>();
        if (_playerCharacter == null) _playerCharacter = playerTarget.GetComponentInParent<PlayerCharacter>();
        if (_playerCharacter == null) _playerCharacter = playerTarget.GetComponentInChildren<PlayerCharacter>();
    }

    private void UpdatePredictionVelocity()
    {
        if (!usePrediction || playerTarget == null) return;

        if (useKccVelocity && _playerCharacter != null)
        {
            Vector3 velocity = _playerCharacter.GetVelocity();
            float blend = Mathf.Clamp01(Time.deltaTime * 10f);
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, blend);
            _hasLastPlayerPosition = false;
            return;
        }

        Vector3 position = playerTarget.position;
        if (_hasLastPlayerPosition)
        {
            float dt = Time.deltaTime;
            if (dt > 0.0001f)
            {
                Vector3 velocity = (position - _lastPlayerPosition) / dt;
                float blend = Mathf.Clamp01(dt * 10f);
                _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, blend);
            }
        }
        _lastPlayerPosition = position;
        _hasLastPlayerPosition = true;
    }

    private Vector3 GetPredictionDirection()
    {
        if (_smoothedVelocity.sqrMagnitude > minVelocityForPrediction * minVelocityForPrediction)
        {
            if (cone3D)
            {
                _debugPredictionDir = _smoothedVelocity.normalized;
                return _debugPredictionDir;
            }

            Vector3 horizontal = Vector3.ProjectOnPlane(_smoothedVelocity, Vector3.up);
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                _debugPredictionDir = horizontal.normalized;
                return _debugPredictionDir;
            }
        }

        if (useFacingWhenIdle)
        {
            Vector3 facing = cone3D
                ? playerTarget.forward
                : Vector3.ProjectOnPlane(playerTarget.forward, Vector3.up);
            if (facing.sqrMagnitude > 0.0001f)
            {
                _debugPredictionDir = facing.normalized;
                return _debugPredictionDir;
            }
        }

        _debugPredictionDir = Vector3.zero;
        return Vector3.zero;
    }

    private float GetConeLength()
    {
        if (!scaleConeWithSpeed)
            return Mathf.Max(coneLength, 1f);

        float speed = _smoothedVelocity.magnitude;
        float scale = Mathf.Lerp(coneMinScale, 1f, Mathf.Clamp01(speed / Mathf.Max(0.1f, coneSpeedReference)));
        return Mathf.Max(coneLength * scale, 1f);
    }

    private IEnumerator OptimizeRoutine()
    {
        float nextRescanTime = -1f;

        while (true)
        {
            yield return RefreshWait();

            if (playerTarget == null)
            {
                TryFindPlayer();
                if (playerTarget == null)
                {
                    if (debugLog) Debug.LogWarning("[CollisionOptimizer] playerTarget не задан.", this);
                    continue;
                }
            }

            yield return EvaluateAndApply();

            if (autoRescan && rescanInterval > 0f && Time.time >= nextRescanTime)
            {
                ScanScene();
                nextRescanTime = Time.time + rescanInterval;
            }
        }
    }

    private WaitForSeconds RefreshWait()
    {
        if (_wait == null || Mathf.Abs(_waitInterval - checkInterval) > 0.0001f)
        {
            _waitInterval = Mathf.Max(0.05f, checkInterval);
            _wait = new WaitForSeconds(_waitInterval);
        }
        return _wait;
    }

    private IEnumerator EvaluateAndApply()
    {
        Vector3 playerPos = playerTarget.position;
        float enableSqr = enableDistance * enableDistance;
        float disableSqr = disableDistance * disableDistance;

        Vector3 predDir = Vector3.zero;
        if (usePrediction)
        {
            predDir = GetPredictionDirection();
        }
        bool hasPrediction = predDir.sqrMagnitude > 0.01f;
        float effectiveConeLength = GetConeLength();

        _toEnable.Clear();
        _toDisable.Clear();

        int count = _colliders.Count;
        for (int i = 0; i < count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null) continue;

            Vector3 to = collider.transform.position - playerPos;
            float sqrDistance = to.sqrMagnitude;

            Vector3 horizontalTo = to;
            horizontalTo.y = 0f;

            Vector3 coneTo = cone3D ? to : horizontalTo;

            bool inCone = hasPrediction && IsInCone(coneTo, predDir, effectiveConeLength, coneAngle);

            if (collider.enabled)
            {
                if (sqrDistance <= enableSqr) continue;
                if (inCone) continue;

                if (sqrDistance > disableSqr)
                {
                    bool inDisableCone = hasPrediction &&
                                         IsInCone(coneTo, predDir, effectiveConeLength + coneDisableMargin,
                                                  coneAngle + coneDisableMargin);
                    if (!inDisableCone) _toDisable.Add(collider);
                }
            }
            else
            {
                if (sqrDistance <= enableSqr || inCone) _toEnable.Add(collider);
            }
        }

        if (_toEnable.Count == 0 && _toDisable.Count == 0) yield break;

        while (_toEnable.Count > 0 || _toDisable.Count > 0)
        {
            int budget = Mathf.Max(1, maxStateChangesPerFrame);

            for (int i = _toEnable.Count - 1; i >= 0 && budget > 0; i--)
            {
                ApplyEnable(_toEnable[i]);
                _toEnable.RemoveAt(i);
                budget--;
            }

            for (int i = _toDisable.Count - 1; i >= 0 && budget > 0; i--)
            {
                ApplyDisable(_toDisable[i]);
                _toDisable.RemoveAt(i);
                budget--;
            }

            if (_toEnable.Count > 0 || _toDisable.Count > 0)
                yield return null;
        }
    }

    private bool IsInCone(Vector3 to, Vector3 dir, float range, float fullAngle)
    {
        float fwd = Vector3.Dot(to, dir);
        if (fwd < 0f) return false;

        float distSqr = to.sqrMagnitude;
        if (distSqr > range * range) return false;

        float half = Mathf.Clamp(fullAngle * 0.5f, 0f, 90f) * Mathf.Deg2Rad;
        float cosHalfSqr = Mathf.Cos(half);
        cosHalfSqr *= cosHalfSqr;
        return fwd * fwd >= distSqr * cosHalfSqr;
    }

    private void ApplyEnable(Collider collider)
    {
        collider.enabled = true;
        _disabledByUs.Remove(collider);
        _activeCount++;

        if (sleepInactiveRigidbodies)
        {
            Rigidbody body = collider.attachedRigidbody;
            if (body != null && !body.isKinematic && body.IsSleeping()) body.WakeUp();
        }
    }

    private void ApplyDisable(Collider collider)
    {
        collider.enabled = false;
        _disabledByUs.Add(collider);
        _activeCount--;

        if (sleepInactiveRigidbodies)
        {
            Rigidbody body = collider.attachedRigidbody;
            if (body != null && !body.isKinematic && !body.IsSleeping()) body.Sleep();
        }
    }

    [ContextMenu("Сканировать сцену")]
    public void ScanScene()
    {
        _colliders.Clear();
        _activeCount = 0;

        CachePlayerComponent();

        Collider[] all = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (IsEligible(all[i])) _colliders.Add(all[i]);
        }

        _trackedCount = _colliders.Count;

        int active = 0;
        for (int i = 0; i < _colliders.Count; i++)
        {
            if (_colliders[i] != null && _colliders[i].enabled) active++;
        }
        _activeCount = active;

        if (debugLog) Debug.Log($"[CollisionOptimizer] Отслеживается коллайдеров: {_colliders.Count}.", this);
    }

    [ContextMenu("Восстановить все коллайдеры")]
    public void RestoreAll()
    {
        for (int i = 0; i < _colliders.Count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null) continue;
            if (_disabledByUs.Contains(collider))
            {
                collider.enabled = true;
                _disabledByUs.Remove(collider);
            }
        }

        int active = 0;
        for (int i = 0; i < _colliders.Count; i++)
        {
            if (_colliders[i] != null && _colliders[i].enabled) active++;
        }
        _activeCount = active;
    }

    private bool IsEligible(Collider collider)
    {
        if (collider == null) return false;
        if (!collider.enabled && !_disabledByUs.Contains(collider)) return false;

        int layerBit = 1 << collider.gameObject.layer;

        if (onlyOptimizeLayers.value != 0 && (onlyOptimizeLayers.value & layerBit) == 0) return false;
        if (ignoreLayers.value != 0 && (ignoreLayers.value & layerBit) != 0) return false;

        if (protectPlayerColliders && playerTarget != null &&
            (collider.transform.IsChildOf(playerTarget) || playerTarget.IsChildOf(collider.transform)))
            return false;

        if (collider.isTrigger && !includeTriggers) return false;

        if (collider.attachedRigidbody == null && !includeStaticColliders) return false;

        return true;
    }

    public int TrackedCount => _colliders.Count;

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || playerTarget == null) return;
        DrawDistanceGizmos();
        DrawConeGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTarget == null) return;
        DrawDistanceGizmos();
        DrawConeGizmos();
    }

    private void DrawDistanceGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(playerTarget.position, enableDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(playerTarget.position, disableDistance);
    }

    private void DrawConeGizmos()
    {
        if (!usePrediction) return;

        Vector3 dir = GetPredictionDirection();
        if (dir.sqrMagnitude <= 0.01f) return;

        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Quaternion rotation = Quaternion.LookRotation(dir, up);
        float length = GetConeLength();

        ConeGizmos.DrawCone(playerTarget.position, rotation, length, coneAngle, 32,
                            new Color(0f, 1f, 1f, 0.8f));
        ConeGizmos.DrawCone(playerTarget.position, rotation, length + coneDisableMargin, coneAngle + coneDisableMargin, 32,
                            new Color(0.5f, 0.5f, 0.5f, 0.6f));
    }
}
