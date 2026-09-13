using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct CameraInput
{
    public Vector2 Look;
}

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private DynamicCamera dynamicCamera;
    [SerializeField] private FlashLight flashLight;
    [SerializeField] private PhotoCamera photoCamera;

    // Флаги блокировки
    [SerializeField] private bool _isControlLocked = false;      // блокировка управления персонажем
    [SerializeField] private bool _isInputEventLocked = false;   // блокировка InputEventUpdate

    public bool IsControlLocked { get => _isControlLocked; }
    public bool IsInputEventLocked { get => _isInputEventLocked; }

    private PlayerInputActions _inputActions;
    private PlayerInputActions.PlayerActions _playerActions; 

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        _playerActions = _inputActions.Player; 

        playerCharacter.Initialize();

        Transform cameraTarget = playerCharacter.GetCameraTarget();
        playerCamera.Initialize(cameraTarget);
        flashLight.Initialize(cameraTarget);
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        if (PauseMenu.IsGamePaused) return;

        var deltaTime = Time.deltaTime;

        var cameraInput = new CameraInput { Look = _playerActions.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(deltaTime, cameraInput);
        dynamicCamera.UpdateRotation
            (
                playerCharacter.transform, 
                playerCharacter.GetVelocity(), 
                playerCharacter.GetGroundingStatus().IsStableOnGround, 
                deltaTime
            );
        flashLight.UpdateRotation(playerCamera.GetTargetEulerAngles(), deltaTime);
        photoCamera.UpdateRotation(playerCamera.GetEulerAngles());

        var move = _isControlLocked ? Vector2.zero : _playerActions.Move.ReadValue<Vector2>();
        var sprint = !_isControlLocked && _playerActions.Sprint.IsPressed();
        var jump = !_isControlLocked && _playerActions.Jump.WasPressedThisFrame();

        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = move,
            Sprint = sprint,
            Jump = jump
        };

        playerCharacter.UpdateInput(characterInput);
    }

    private void LateUpdate()
    {
        if (PauseMenu.IsGamePaused) return;

        float deltaTime = Time.deltaTime;
        Transform cameraTarget = playerCharacter.GetCameraTarget();

        if (!_isInputEventLocked) InputEventUpdate();

        playerCamera.UpdatePosition(cameraTarget);
        playerCamera.UpdateState(deltaTime);

        flashLight.UpdatePosition(cameraTarget);
        flashLight.UpdateLightning(deltaTime);
        photoCamera.UpdatePosition(cameraTarget);
    }

    private void InputEventUpdate()
    {
        if (_playerActions.FlashLight.WasPressedThisFrame()) flashLight.PlayerLightSwitch();
        if (_playerActions.Zoom.WasPressedThisFrame()) playerCamera.ToggleZoom(true);
        if (_playerActions.Zoom.WasReleasedThisFrame()) playerCamera.ToggleZoom(false);
        if (_playerActions.MainUse.WasPressedThisFrame())
        {  
            if (Utils.Chance(0.1f)) StartCoroutine(GameManager.Instance.SpawnTheNone());
            photoCamera.TakePhoto();
        }
        if (_playerActions.SecondUse.WasPressedThisFrame()) photoCamera.InspectingSwitch();
#if UNITY_EDITOR
        if (Keyboard.current != null && 
            Keyboard.current.numpad1Key.wasPressedThisFrame)
            TeleportToNextPOI();
        if (Keyboard.current != null && 
            Keyboard.current.numpad2Key.wasPressedThisFrame)
            GameManager.Instance.PlayBells();
        if (Keyboard.current != null && 
            Keyboard.current.numpad3Key.wasPressedThisFrame)
            StartCoroutine(GameManager.Instance.GhostEvent());
#endif
    }

    private int lastPOIPosIndex = 0;
    private void TeleportToNextPOI()
    {
        List<Vector3> points = POIManager.Instance.GetAllPOIPositions();
        if (lastPOIPosIndex >= points.Count) lastPOIPosIndex = 0;
        Teleport(points[lastPOIPosIndex]); lastPOIPosIndex ++;
        lastPOIPosIndex = (lastPOIPosIndex + 1) % points.Count;
    }

    public void Teleport(Vector3? targetPos = null, Vector3? targetLook = null)
    {
        if (targetPos != null) playerCharacter.SetPosition((Vector3)targetPos);
        playerCamera.ForceSetLookDirection(targetLook ?? Vector3.zero);
    }
    public void PhotoCameraInspectingSwitch(bool? @bool = null) => photoCamera.InspectingSwitch(@bool);
    public void ControlLockSwitch(bool? @bool = null) => _isControlLocked = @bool ?? !_isControlLocked;
    public void InputEventLockSwitch(bool? @bool = null) => _isInputEventLocked = @bool ?? !_isInputEventLocked;
}

