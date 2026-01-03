// InventoryUI.cs
// Shows the currently selected item (name + description + count) from PlayerInventory
// and rotates the displayed model using the Inventory action map
// in GameControls (PreviousItem, NextItem, RotateItem, RotateMouseHold, Back).
// Mouse rotation requires a held mouse button; gamepad stick works directly.
// Hint UI switches between keyboard/mouse and gamepad using InputDeviceTracker.
// Psy-Lens

using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private TextMeshProUGUI itemNameText;    // ItemNameText
    [SerializeField] private TextMeshProUGUI itemDetailsText; // ItemDetailsText (details panel)

    [Header("Hint Roots")]
    [SerializeField] private GameObject keyboardHintsRoot;    // e.g. Hints_KeyboardMouse
    [SerializeField] private GameObject gamepadHintsRoot;     // e.g. Hints_Gamepad

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 0.3f; // degrees per unit input

    private GameControls controls;

    private enum InputMode
    {
        KeyboardMouse,
        Gamepad
    }

    private InputMode lastInputMode = InputMode.KeyboardMouse;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        controls = new GameControls();
    }

    private void OnEnable()
    {
        if (controls == null)
            controls = new GameControls();

        controls.Inventory.PreviousItem.performed += OnPrevItemPerformed;
        controls.Inventory.NextItem.performed     += OnNextItemPerformed;
        controls.Inventory.Back.performed         += OnBackPerformed;

        controls.Inventory.Enable();

        UpdateHintsFromTracker(force: true);
        Refresh();
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Inventory.PreviousItem.performed -= OnPrevItemPerformed;
            controls.Inventory.NextItem.performed     -= OnNextItemPerformed;
            controls.Inventory.Back.performed         -= OnBackPerformed;

            controls.Inventory.Disable();
        }
    }

    private void Update()
    {
        Refresh();
        UpdateHintsFromTracker(force: false);
        HandleRotationInput();
    }

    // ---------------------------------------------------------
    // INPUT HANDLERS
    // ---------------------------------------------------------

    private void OnPrevItemPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || playerInventory == null)
            return;

        playerInventory.SelectPreviousItem();
        Refresh();
    }

    private void OnNextItemPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || playerInventory == null)
            return;

        playerInventory.SelectNextItem();
        Refresh();
    }

    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || playerInventory == null)
            return;

        // For now: Back closes the inventory.
        playerInventory.ToggleInventory();
    }

    // ---------------------------------------------------------
    // DEVICE-BASED HINTS (GLOBAL)
    // ---------------------------------------------------------

    private void UpdateHintsFromTracker(bool force)
    {
        InputMode mode = InputMode.KeyboardMouse;

        var tracker = InputDeviceTracker.Instance;
        if (tracker != null)
        {
            switch (tracker.CurrentKind)
            {
                case InputDeviceTracker.InputDeviceKind.Gamepad:
                    mode = InputMode.Gamepad;
                    break;
                case InputDeviceTracker.InputDeviceKind.KeyboardMouse:
                case InputDeviceTracker.InputDeviceKind.Other:
                default:
                    mode = InputMode.KeyboardMouse;
                    break;
            }
        }

        if (!force && mode == lastInputMode)
            return;

        lastInputMode = mode;

        if (keyboardHintsRoot != null)
            keyboardHintsRoot.SetActive(mode == InputMode.KeyboardMouse);

        if (gamepadHintsRoot != null)
            gamepadHintsRoot.SetActive(mode == InputMode.Gamepad);
    }

    // ---------------------------------------------------------
    // UI REFRESH
    // ---------------------------------------------------------

    public void Refresh()
    {
        if (playerInventory == null)
        {
            if (itemNameText != null)    itemNameText.text = "";
            if (itemDetailsText != null) itemDetailsText.text = "";
            return;
        }

        string id    = playerInventory.GetSelectedItemId();
        int    count = playerInventory.GetSelectedCount();

        if (string.IsNullOrEmpty(id))
        {
            if (itemNameText != null)    itemNameText.text = "";
            if (itemDetailsText != null) itemDetailsText.text = "";
            return;
        }

        string nameToShow    = id;
        string detailsToShow = "";

        if (playerInventory.TryGetSelectedDefinition(out var def) && def != null)
        {
            if (!string.IsNullOrWhiteSpace(def.displayName))
                nameToShow = def.displayName;

            if (!string.IsNullOrEmpty(def.description))
                detailsToShow = def.description;
        }

        // format: "(x) Name" when count > 1
        if (count > 1)
            nameToShow = $"({count}) {nameToShow}";

        if (itemNameText != null)
            itemNameText.text = nameToShow;

        if (itemDetailsText != null)
            itemDetailsText.text = detailsToShow;
    }

    // ---------------------------------------------------------
    // ROTATION
    // ---------------------------------------------------------

    private void HandleRotationInput()
    {
        if (playerInventory == null || controls == null)
            return;

        // Combined action: Pointer delta + right stick
        Vector2 input = controls.Inventory.RotateItem.ReadValue<Vector2>();
        if (input.sqrMagnitude <= 0.0001f)
            return;

        // Mouse path: only rotate if mouse is moving AND the hold button is pressed.
        bool mouseMoving = false;
        bool mouseHeld   = false;

        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseMoving = mouseDelta.sqrMagnitude > 0.0001f;

            // RotateMouseHold is a Button action bound to <Mouse>/leftButton or rightButton
            float holdValue = controls.Inventory.RotateMouseHold.ReadValue<float>();
            mouseHeld = holdValue > 0.5f;
        }

        // If pointer is moving but the hold button is NOT pressed, ignore this input.
        // Gamepad right stick is unaffected.
        if (mouseMoving && !mouseHeld)
            return;

        float yaw   = -input.x * rotationSpeed; // left/right
        float pitch =  input.y * rotationSpeed; // up/down

        playerInventory.RotateInspect(new Vector2(yaw, pitch));
    }
}
