using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : SingletonBehaviour<InputManager>
{
    public PlayerInput playerInput;
    public PauseMenu pauseMenu;


    private Vector2 lookInput;
    private bool crouchInput;
    private Vector2 moveInput;
    private bool fireInput;
    private bool jumpInput;
    private bool sprintInput;
    private bool aimInput;
    private bool altAimInput;
    private bool slowWalkInput;
    private bool dashInput;

    public static Vector2 MoveInput { get => PauseMenu.GamePaused ? Vector2.zero : Instance.moveInput; set => Instance.moveInput = value; }
    public static Vector2 LookInput { get => PauseMenu.GamePaused ? Vector2.zero : Instance.lookInput; set => Instance.lookInput = value; }
    public static bool FireInput { get => Instance.fireInput && !PauseMenu.GamePaused; set => Instance.fireInput = value; }
    public static bool JumpInput { get => Instance.jumpInput && !PauseMenu.GamePaused; set => Instance.jumpInput = value; }
    public static bool SprintInput { get => Instance.sprintInput && !PauseMenu.GamePaused; set => Instance.sprintInput = value; }
    public static bool CrouchInput { get => Instance.crouchInput && !PauseMenu.GamePaused; set => Instance.crouchInput = value; }
    public static bool AimInput { get => Instance.aimInput && !PauseMenu.GamePaused; set => Instance.aimInput = value; }
    public static bool AltAimInput { get => Instance.altAimInput && !PauseMenu.GamePaused; set => Instance.altAimInput = value; }
    public static bool SlowWalkInput { get => Instance.slowWalkInput && !PauseMenu.GamePaused; set => Instance.slowWalkInput = value; }
    public static bool DashInput { get => Instance.dashInput && !PauseMenu.GamePaused; set => Instance.dashInput = value; }

    public bool gamepadCrouchToggle = true;
    public bool altAimToggle = true;

    #region Input
    public void GetMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void GetLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    public void GetFireInput(InputAction.CallbackContext context)
    {
        fireInput = context.ReadValueAsButton();
    }
    public void GetJumpInput(InputAction.CallbackContext context)
    {
        jumpInput = context.ReadValueAsButton();
    }
    public void GetSprintInput(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValueAsButton();
    }
    public void GetCrouchInput(InputAction.CallbackContext context)
    {
        crouchInput = context.ReadValueAsButton();
    }
    public void GetAimInput(InputAction.CallbackContext context)
    {
        aimInput = context.ReadValueAsButton();
    }
    public void GetCrouchToggleInput(InputAction.CallbackContext context)
    {
        if (context.performed)
            crouchInput = !crouchInput;
    }
    public void GetAltAimInput(InputAction.CallbackContext context)
    {

        if (altAimToggle)
        {
            if (context.performed)
                altAimInput = !altAimInput;
        }
        else
        {
            altAimInput = context.ReadValueAsButton();
        }

    }
    public void GetSlowWalkToggleInput(InputAction.CallbackContext context)
    {
        if(context.performed)
            slowWalkInput = !slowWalkInput;
    }
    public void GetSlowWalkInput(InputAction.CallbackContext context)
    {
        slowWalkInput = context.ReadValueAsButton();
    }
    public void GetCrouchGamepad(InputAction.CallbackContext context)
    {
        if (gamepadCrouchToggle)
        {
            if (context.performed)
                crouchInput = !crouchInput;
        }
        else
        {
            crouchInput = context.ReadValueAsButton();
        }
    }

    public void GetDashInput(InputAction.CallbackContext context)
    {
        dashInput = context.ReadValueAsButton();
    }

    public void GetPauseInput(InputAction.CallbackContext context)
    {
        if (context.performed)
            pauseMenu.TogglePause();
    }
    #endregion

    private void Awake()
    {
        if(Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void PauseGame(bool paused)
    {
        playerInput.DeactivateInput();
    }
}
