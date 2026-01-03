// InputDeviceTracker.cs
// Uses the "Dummies" action map in GameControls to track last used device.
// Logs when device type changes and exposes a simple enum for UI hint switching.
// Psy-Lens

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;

public class InputDeviceTracker : MonoBehaviour, GameControls.IDummiesActions
{
    public static InputDeviceTracker Instance { get; private set; }

    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
        Other
    }

    [SerializeField]
    private InputDeviceKind currentKind = InputDeviceKind.KeyboardMouse;

    public InputDeviceKind CurrentKind => currentKind;
    public InputDevice CurrentDevice { get; private set; }

    private GameControls controls;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        controls = new GameControls();
        controls.Dummies.SetCallbacks(this);
        controls.Dummies.Enable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (controls != null)
        {
            controls.Dummies.RemoveCallbacks(this);
            controls.Dummies.Disable();
            controls.Dispose();
            controls = null;
        }
    }

    private void SetDeviceFromControl(InputControl control)
    {
        if (control == null)
            return;

        var device = control.device;
        if (device == null)
            return;

        InputDeviceKind newKind;
        string logMessage = null;

        // Keyboard / Mouse
        if (device is Keyboard || device is Mouse)
        {
            newKind = InputDeviceKind.KeyboardMouse;
            logMessage = "Input device changed to Mouse/Keyboard";
        }
        // Gamepads
        else if (device is Gamepad)
        {
            newKind = InputDeviceKind.Gamepad;

            if (device is XInputController)
            {
                logMessage = "Input device changed to Gamepad (Xbox)";
            }
            else if (device is DualShockGamepad)
            {
                logMessage = "Input device changed to Gamepad (PlayStation)";
            }
            else
            {
                logMessage = "Input device changed to Gamepad (Other)";
            }
        }
        else
        {
            newKind = InputDeviceKind.Other;
            logMessage = "Input device changed to Other device";
        }

        // If nothing actually changed, do nothing
        if (newKind == currentKind && device == CurrentDevice)
            return;

        currentKind = newKind;
        CurrentDevice = device;

        if (!string.IsNullOrEmpty(logMessage))
            Debug.Log(logMessage);
    }

    // ---------------------------------------------------------
    // IDummiesActions implementation
    // ---------------------------------------------------------

    public void OnMouseKeyboard(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SetDeviceFromControl(context.control);
    }

    public void OnMousevectors(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SetDeviceFromControl(context.control);
    }

    public void OnGamepad(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SetDeviceFromControl(context.control);
    }

    public void OnGamepadVectors(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        SetDeviceFromControl(context.control);
    }
}
