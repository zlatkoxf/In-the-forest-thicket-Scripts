using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System;
using System.Runtime.InteropServices;

[DefaultExecutionOrder(100)] 
public class GameManager : MonoBehaviour
{
    [Space]
    [SerializeField] private string featureName = "CrashEffect";
    [Space]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerCharacter _playerCharacter;
    [SerializeField] private PlayerCamera _playerCamera;
    [Space]
    [SerializeField] private PrefabSpawner _prefabSpawner;
    [SerializeField] private List<Transform> _prefabSpawnPoints = new List<Transform>();
    [Space]
    [SerializeField] TypewriterStyleText typewriter;
    [Space]
    [SerializeField][Range(0f, 1f)] private float oneShotVolume = 1f;
    [SerializeField] private Range oneShotDelay = new Range(30, 180);
    [SerializeField] private bool _oneShotIsStopped = false;
    [Space]
    [SerializeField] private Transform tombStone;
    [SerializeField] private FlashLight _flashLight;

    public bool OneShotIsStopped { get => _oneShotIsStopped; set => _oneShotIsStopped = value; }

    private AudioManager _audioManager;
    private float _timer;
    private float _nextOneShotTime;
    private GameObject _ghost;
    private bool _ghostIsSpawned = false;
    private bool _ghostSpawning = false;
    private bool _isCrashInitiated = false;

    private const string KEY_AMBIENT_WIND = "WindLoop";
    private const string KEY_BRANCH = "Branch";
    private const string KEY_BRANCH2 = "Branch2";
    private const string KEY_BRANCH3 = "Branch3";
    private const string KEY_OWL = "Owl";
    private const string KEY_SHOT = "Shot";
    private const string KEY_WOLF = "Wolf";
    private const string KEY_WOLF2 = "Wolf2";
    private const string KEY_WOLF3 = "Wolf3";
    private const string KEY_WOLF4 = "Wolf4";
    private const string KEY_CRASH = "Crash";
    private const string KEY_AMBIENT_TRIGGER = "AmbientTrigger";
    private const string KEY_PARANOIA_AMBIENT = "ParanoiaAmbient";
    private const string KEY_GHOST = "GhostSound";
    private const string KEY_BELLS = "Bells";

    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        // Настройка синглтона
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _audioManager = AudioManager.Instance;
        _audioManager.PlayLoopSound(KEY_AMBIENT_WIND, 0.7f, group: _audioManager.ambientGroup);
        _nextOneShotTime = Utils.GetRandOfRange(oneShotDelay);
        //Speak("hello");
        StartCoroutine(StartGhostEvent());
        StartCoroutine(RepeatActionCoroutine(30f, () => PlayBells()));
        //StartCoroutine(RepeatActionCoroutine(5f, () => SpawnPeaceGhost()));
        StartCoroutine(EnableCamera());
        //StartCoroutine(RepeatActionCoroutine(10f, () => TryDisableFlashLight()));
    }

    // private void TryDisableFlashLight()
    // {
    //     if (Utils.Chance(50f))
    //     {
    //         float distanceSqr = (_playerCharacter.transform.position - tombStone.position).sqrMagnitude;
    //         if (distanceSqr <= 5f) _flashLight.LightSwitch(false);
    //     }
    //     if (Utils.Chance(5f)) _flashLight.LightSwitch(false);
    // }

    // public void Speak(string text, float delayInSeconds)
    // {
    //     // Заменяем запятую на точку, чтобы PowerShell корректно распарсил float (например, 2.1 вместо 2,1)
    //     string delayStr = delayInSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    //     // PowerShell сначала ждет delayInSeconds, затем подгружает библиотеки и говорит
    //     string command = $"Start-Sleep -Seconds {delayStr}; Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{text}')";
    //     var processInfo = new System.Diagnostics.ProcessStartInfo("powershell", $"-Command \"{command}\"")
    //     {
    //         CreateNoWindow = true,
    //         UseShellExecute = true // ПРОЦЕСС НЕЗАВИСИМ. Он выживет, даже если игра умрет через секунду
    //     };
    //     System.Diagnostics.Process.Start(processInfo);
    // }

    private IEnumerator RepeatActionCoroutine(float interval, Action actionToCall)
    {
        WaitForSeconds wait = new WaitForSeconds(interval);
        while (true)
        {
            yield return wait;
            actionToCall?.Invoke(); 
        }
    }

    private IEnumerator EnableCamera()
    {
        yield return new WaitForSeconds(120f);
        if (HiddenCamera.Toggle(true) == true) typewriter.SendMessage(Localization.Get("gameplay_typewriter_camera"));
    }

    public void PlayBells()
    {
        Dictionary<string, Transform> points = POIManager.Instance.GetComingUnvisitedPoints(_playerCharacter.transform.position, 200f);
        foreach (var point in points)
        {
            BellChimeUtility.PlayChime(this, KEY_BELLS, point.Value.position);
            Debug.Log("[GameManager] Колокольчики у " + point.Key);
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _nextOneShotTime)
        {
            _timer = 0;
            _nextOneShotTime = Utils.GetRandOfRange(oneShotDelay);

            if (_oneShotIsStopped) return;
            PlayRandomSound();
        }

        if (CheckGhostDistance()) // конец игры.
        {
            _audioManager.PlaySound(KEY_CRASH);
            if (!_isCrashInitiated) StartCoroutine(InitGameCrash());
        }

        POIManager.Instance.UpdateVisitedPoints(_playerCharacter.transform.position, 20f);
    }

    [DllImport("kernel32.dll")]
    public static extern void ExitProcess(uint uExitCode);
    private IEnumerator InitGameCrash()
    {
        PauseMenu.Instance.PauseIsLocked = true;
        PauseMenu.Instance.Resume();

        if (_isCrashInitiated) yield break; 
        _isCrashInitiated = true;

        ToggleFeature(featureName, true);
        yield return new WaitForSecondsRealtime(0.1f);
        _audioManager.SetVolume(10f, AudioGroup.Master);
        AudioCrashEffect.Instance.IsCrashed = true;
#if !UNITY_EDITOR
        float freezeEndTime = Time.realtimeSinceStartup + 1.8f;
        while (Time.realtimeSinceStartup < freezeEndTime) { }
        yield return null; 
        //PostGameNotifier.SendNotification("In the forest thicket", "В этой игре нельзя победить.");
        MonitorController.SetMonitorState(false);
        freezeEndTime = Time.realtimeSinceStartup + 0.2f;
        while (Time.realtimeSinceStartup < freezeEndTime) { }

        ExitProcess(0);
        UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.AccessViolation);
#else
        System.Threading.Thread.Sleep(1000);
        //PostGameNotifier.SendNotification("In the forest thicket", "В этой игре нельзя победить.");
        //MonitorController.SetMonitorState(false);
        //if (StreamSpy.IsOBSRunning()) Speak("Передай привет своим зрителям. Шоу окончено.", 3f);
        Debug.LogWarning("[GameManager] Здесь игра должна была зависнуть на 1 секунду.");
        ToggleFeature(featureName, false);
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private bool CheckGhostDistance()
    {
        if (_ghost == null) return false;
        return Vector3.Distance(_playerCharacter.transform.position, _ghost.transform.position) <= 0.5f ? true : false;
    }

    private void PlayRandomSound()
    {
        string key = "";
        float radius = 50;
        float volume = oneShotVolume;
        float chance = 90;
        float minDistance = 1;

        int rN = UnityEngine.Random.Range(1, 5); // 1..4
        switch (rN)
        {
            case 1:
                key = UnityEngine.Random.Range(1, 4) switch
                {
                    1 => KEY_BRANCH,
                    2 => KEY_BRANCH2,
                    3 => KEY_BRANCH3, _ => KEY_BRANCH
                };
                radius = 3;
                break;
            case 2:
                key = UnityEngine.Random.Range(1, 5) switch
                {
                    1 => KEY_WOLF,
                    2 => KEY_WOLF2,
                    3 => KEY_WOLF3,
                    4 => KEY_WOLF4, _ => KEY_WOLF
                };
                radius = 100;
                chance = 50;
                volume = volume / 2;
                minDistance = 200;
                break;
            case 3:
                key = KEY_OWL;
                radius = 20;
                chance = 50;
                minDistance = 10;
                break;
            case 4:
                key = KEY_SHOT;
                radius = 200;
                chance = 20;
                minDistance = 400;
                break;
        }

        if (string.IsNullOrEmpty(key)) return;

        // Вычисляем случайную позицию вокруг игрока
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        Vector3 randomPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        Vector3 finalPos = _playerCharacter.transform.position + randomPos;

        if (Utils.Chance(chance))
            _audioManager.PlaySound(key, finalPos, volume, minDistance: minDistance, group: _audioManager.ambientGroup);
            Debug.Log("[GameManager] Воспроизведен звук " + key);
    }

    private IEnumerator StartGhostEvent()
    {
        yield return new WaitForSeconds(1000f);
        StartCoroutine(GhostEvent());
    }

    public IEnumerator GhostEvent()
    {
        if (_ghostIsSpawned) yield return null;
        _ghostSpawning = true;

        PauseMenu.Instance.quitAllowed = false;

        _audioManager.PlaySound(KEY_AMBIENT_TRIGGER);
        Vector3 spawnPos = GetRandomVector3(_prefabSpawnPoints.ToArray());
        _player.PhotoCameraInspectingSwitch(false);
        _player.ControlLockSwitch(true);
        _player.InputEventLockSwitch(true);
        _playerCamera.SetTargetLock(true, spawnPos);
        _playerCamera.ToggleLook(false);
        //_playerCamera.ForcedTargetWorldPosition = spawnPos;
        //_playerCamera.ToggleTargetLook(true);

        yield return new WaitForSeconds(5f);

        string realName = System.Environment.UserName;
        typewriter.SendStringMessage(String.Format(Localization.Get("gameplay_typewriter_theghost"), realName));
        _audioManager.PlayLoopSound(KEY_PARANOIA_AMBIENT);
        _player.ControlLockSwitch(false);
        _player.InputEventLockSwitch(false);
        _playerCamera.ToggleLook(true);
        _playerCamera.ToggleTargetLook(false);

        _ghostSpawning = false;
        SpawnTheGhost(spawnPos);
    }

    public void SpawnTheGhost(Vector3 spawnPos)
    {
        if (_ghostIsSpawned || _ghostSpawning) return;
        _ghost = _prefabSpawner.SpawnPrefabByIndex(0, spawnPos);
        _ghost.GetComponent<ObjectLinearMove>().TargetTransform = _playerCharacter.transform;
        
        AudioSource ghostAudio = _audioManager.PlayLoopSound(KEY_GHOST, _ghost.transform.position, minDistance: 10f, maxDistance: 50f);
        ghostAudio.gameObject.transform.SetParent(_ghost.transform);
        ghostAudio.gameObject.transform.localPosition = Vector3.zero;
        
        StartCoroutine(SpeedUpTheGhost(_ghost));

        _ghostIsSpawned = true;
    }

    private IEnumerator SpeedUpTheGhost(GameObject ghost)
    {
        yield return new WaitForSecondsRealtime(120f);
        ghost.GetComponent<ObjectLinearMove>().MoveSpeed = 15f;
    }

    // private GameObject peaceGhost;
    // private void SpawnPeaceGhost()
    // {
    //     Destroy(peaceGhost);
    //     peaceGhost = _prefabSpawner.SpawnPrefabByIndex(1, CameraUtils.GetRandomPointInViewport(_playerCamera.Camera,
    //         Vector2.zero,
    //         Vector2.one,
    //         UnityEngine.Random.Range(3, 10)
    //     ));
    // }

    public IEnumerator SpawnTheNone()
    {
        GameObject theNone = _prefabSpawner.SpawnPrefabByIndex(2, _playerCharacter.transform.position);
        theNone.transform.Rotate(0f, UnityEngine.Random.Range(0f,360f), 0f);
        theNone.transform.position += Vector3.up * 20f;
        yield return new WaitForSeconds(0.5f);
        Destroy(theNone);
    }

    private Vector3 GetRandomVector3(Vector3[] vectors) => vectors[UnityEngine.Random.Range(0, vectors.Length - 1)];
    private Vector3 GetRandomVector3(Transform[] transforms) => transforms[UnityEngine.Random.Range(0, transforms.Length - 1)].position;

    // private IEnumerator SendText()
    // {
    //     yield return new WaitForSeconds(3f);
    //     typewriter.SendStringMessage("Проверка текста/Еще раз, строка 2./Строка 3...");
    //     yield return new WaitForSeconds(25f);
    //     typewriter.SendStringMessage("Проверка завершена. Длинный текст, очень длинный, длинный длинный длинный, проверка адаптивного размера текста.");
    // }

    public void ToggleFeature(string featureName, bool active)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;

        int idx = (int)typeof(UniversalRenderPipelineAsset)
            .GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(urp);

        var renderer = urp.GetRenderer(idx);
        if (renderer == null) return;

        // Пробуем получить свойство rendererFeatures (публичное)
        var prop = renderer.GetType().GetProperty("rendererFeatures", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var features = prop?.GetValue(renderer) as System.Collections.Generic.List<ScriptableRendererFeature>;

        // Если не получилось – пробуем поле m_RendererFeatures
        if (features == null)
        {
            var field = renderer.GetType().GetField("m_RendererFeatures", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            features = field?.GetValue(renderer) as System.Collections.Generic.List<ScriptableRendererFeature>;
        }

        if (features == null) return;

        foreach (var f in features)
            if (f != null && f.name == featureName)
                f.SetActive(active);
    }
}
