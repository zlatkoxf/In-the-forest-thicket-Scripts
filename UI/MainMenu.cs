using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private UIDocument _mainMenu;
    [SerializeField] private UIDocument _loadingScreen;
    [SerializeField] private SettingsMenu _settingsMenu;

    private VisualElement _mainMenuContainer;
    private VisualElement _loadingScreenContainer;

    private Button _startButton;
    private Button _settingsButton;
    private Button _quitButton;
    private ProgressBar _progressBar;

    private PlayerInputActions _inputActions;
    private PlayerInputActions.PlayerActions _playerActions;
    private bool _isLoading = false;

    void OnEnable()
    {
        if (_mainMenu == null) return;

        _mainMenuContainer = _mainMenu.rootVisualElement;
        _startButton = _mainMenuContainer.Q<Button>("start-button");
        _settingsButton = _mainMenuContainer.Q<Button>("settings-button");
        _quitButton = _mainMenuContainer.Q<Button>("quit-button");

        UpdateLabelsLocalization();
        SettingsManager.OnLanguageChanged += UpdateLabelsLocalization;

        _loadingScreenContainer = _loadingScreen.rootVisualElement;
        _progressBar = _loadingScreenContainer.Q<ProgressBar>("ProgressBar");
        _loadingScreenContainer.style.display = DisplayStyle.None;

        _startButton.clicked += StartGame;
        _settingsButton.clicked += OpenSettings;
        _quitButton.clicked += QuitGame;
    }

    void OnDisable()
    {
        _startButton.clicked -= StartGame;
        _settingsButton.clicked -= OpenSettings;
        _quitButton.clicked -= QuitGame;
        SettingsManager.OnLanguageChanged -= UpdateLabelsLocalization;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        _playerActions = _inputActions.Player;
    }

    void Update()
    {
        if (_playerActions.Escape.WasPressedThisFrame())
            if (SettingsMenu.IsSettingsMenuOpen) _settingsMenu.HideMenu();
    }

    private void UpdateLabelsLocalization()
    {
        _startButton.text = Localization.Get("start_button");
        _settingsButton.text = Localization.Get("settings_button");
        _quitButton.text = Localization.Get("quit_button");
    }

    public void StartGame()
    {
        StartCoroutine(LoadSceneProcess("MainScene"));
    }

    IEnumerator LoadSceneProcess(string sceneName)
    {
        _inputActions.Disable();
        _inputActions.Dispose();
        _mainMenuContainer.style.display = DisplayStyle.None;
        _loadingScreenContainer.style.display = DisplayStyle.Flex;
        yield return new WaitForSecondsRealtime(0.1f);

        _isLoading = true;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        float visualProgress = 0f, targetProgress = 0f;

        // --- ЭТАП 1: Считывание файлов с диска (занимает от 0% до 40% шкалы) ---
        while (visualProgress < 0.4f)
        {
            targetProgress = (Mathf.Min(op.progress, 0.9f) / 0.9f) * 0.4f;
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.unscaledDeltaTime * 1.5f);
            if (_progressBar != null) _progressBar.value = visualProgress;
            if (op.progress >= 0.9f && visualProgress >= 0.39f) visualProgress = 0.4f;
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // --- ЭТАП 2: Пошаговая инициализация объектов новой сцены (от 40% до 100%) ---
        SceneInitializer initializer = FindAnyObjectByType<SceneInitializer>();
        bool stepFinished = false;

        StartCoroutine(initializer.InitializeWorldRoutine(step => {
            targetProgress = 0.4f + (step * 0.6f);
            if (Mathf.Approximately(step, 1f)) stepFinished = true;
        }));

        while (visualProgress < 1f)
        {
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.unscaledDeltaTime * 2f);
            if (_progressBar != null) _progressBar.value = visualProgress;
            if (stepFinished && visualProgress >= 0.99f) visualProgress = 1f;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        ObjectStreamer streamer = FindAnyObjectByType<ObjectStreamer>();
        var unloadOp = SceneManager.UnloadSceneAsync("MainMenu");
        if (streamer != null) streamer.InitAndStartStreaming();

        Destroy(gameObject);
    }

    public void OpenSettings() { if (_settingsMenu != null && !_isLoading) _settingsMenu.ShowMenu(); }

    public void QuitGame()
    {
        if (_isLoading) return;
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
