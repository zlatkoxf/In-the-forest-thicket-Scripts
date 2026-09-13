using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private AudioGroup[] groupsToPause;
    [SerializeField] private SettingsMenu _settingsMenu;

    private VisualElement _pauseMenuContainer;
    private PlayerInputActions _inputActions;
    private PlayerInputActions.PlayerActions _playerActions;
    private AudioManager _audioManager;

    private Button _resumeButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Label _bindTips;

    private bool _pauseIsLocked = false;
    public bool PauseIsLocked { get => _pauseIsLocked; set => _pauseIsLocked = value; }
    private bool _quitAllowed = true;
    public bool quitAllowed { get => _quitAllowed; set => _quitAllowed = value; }

    public static PauseMenu Instance  { get; private set; }
    public static bool IsGamePaused { get; private set; }

    void OnEnable()
    {
        _pauseMenuContainer = GetComponent<UIDocument>().rootVisualElement;
        _resumeButton = _pauseMenuContainer.Q<Button>("resume-button");
        _settingsButton = _pauseMenuContainer.Q<Button>("settings-button");
        _quitButton = _pauseMenuContainer.Q<Button>("quit-button");
        _bindTips = _pauseMenuContainer.Q<Label>("bind_tips_label");

        UpdateLabelsLocalization();
        SettingsManager.OnLanguageChanged += UpdateLabelsLocalization;

        _resumeButton.clicked += Resume;
        _settingsButton.clicked += OpenSettings;
        _quitButton.clicked += QuitGame;

        Instance = this;
        IsGamePaused = false;

        _audioManager = AudioManager.Instance;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        _playerActions = _inputActions.Player;

        HideMenu();
    }

    void OnDisable()
    {
        _resumeButton.clicked -= Resume;
        _settingsButton.clicked -= OpenSettings;
        _quitButton.clicked -= QuitGame;
        SettingsManager.OnLanguageChanged -= UpdateLabelsLocalization;
    }

    private void UpdateLabelsLocalization()
    {
        _resumeButton.text = Localization.Get("resume_button");
        _settingsButton.text = Localization.Get("settings_button");
        _quitButton.text = Localization.Get("quit_button");
        _bindTips.text = Localization.Get("bind_tips");
    }

    void Awake() { Application.wantsToQuit += OnWantsToQuit; }

    void Update()
    {
        if (_playerActions.Escape.WasPressedThisFrame() && !_pauseIsLocked)
        {
            if (SettingsMenu.IsSettingsMenuOpen) _settingsMenu.HideMenu();
            else if (IsGamePaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        HideMenu();
        _audioManager.TogglePauseGroups(groupsToPause, false);
        Time.timeScale = 1f;
        IsGamePaused = false;
    }
    public void Pause()
    {
        ShowMenu();
        _audioManager.TogglePauseGroups(groupsToPause, true);
        Time.timeScale = 0f;
        IsGamePaused = true;
    }

    public void OpenSettings()
    {
        _settingsMenu.ShowMenu();
    }

    public void ShowMenu()
    {
        _pauseMenuContainer.style.display = DisplayStyle.Flex; // Делаем видимым
        _pauseMenuContainer.focusable = true;                  // Разрешаем фокус/взаимодействие
        if (quitAllowed) UnityEngine.Cursor.lockState = CursorLockMode.None;
        else UnityEngine.Cursor.lockState = CursorLockMode.Confined;
    }

    public void HideMenu()
    {
        _pauseMenuContainer.style.display = DisplayStyle.None; // Полностью скрываем и отключаем клики
        _pauseMenuContainer.focusable = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame()
    {
        if (!_quitAllowed) return;
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private bool OnWantsToQuit()
    {
        if (!_quitAllowed) return false; // Отменяет закрытие игры
        return true; // Разрешает закрытие игры
    }
}
