// PlayerController.cs
// Fixed + Follow camera movement integration (flag-based mode, working)
// Psy-Lens

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, GameControls.IPlayerActions
{
    [Header("References")]
    public CameraManager cameraManager;         // assign in inspector (or auto-found in Awake)
    public PlayerInteraction playerInteraction; // assign in inspector (or auto-found in Awake)

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private Animator animator; // Animator on Jason_Visual

    [Header("Input Tuning")]
    [SerializeField] private float moveDeadzone = 0.2f; // 0..1 deadzone for left stick

    private CharacterController controller;
    private GameControls controls;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;
    private bool isFocusing;
    private bool flashlightOn;

    // internal reference to keep local copy of the flag
    private bool isFreeCamera; // true = follow-camera mode

    // keyboard movement memory
    private readonly Dictionary<Key, Camera> keyCameraMap = new();

    // controller analog locking (used in fixed camera mode)
    private Camera stickLockedCamera;
    private Vector2 stickLockedDir;
    private bool stickHasLock;
    private const float stickReacquireAngle = 25f;

    private Quaternion lastRotation;

    // last movement direction in world space (for facing)
    private Vector3 lastMoveDirWorld = Vector3.zero;

    // unified input tracking
    private enum LookSource { None, Mouse, Stick }
    private LookSource activeLookSource = LookSource.None;
    private float lastLookTime;

    // gameplay lock helper: if timeScale == 0, treat as "no gameplay"
    private bool IsGameplayFrozen => Time.timeScale == 0f;
    private bool wasFrozen = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // ensure we have a valid camera manager reference
        if (cameraManager == null)
            cameraManager = Object.FindFirstObjectByType<CameraManager>();

        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(); // finds Jason_Visual's Animator

        controls = new GameControls();
        controls.Player.SetCallbacks(this);
    }

    void Start() => lastRotation = transform.rotation;

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        bool frozenNow = IsGameplayFrozen;

        // just became frozen this frame
        if (frozenNow && !wasFrozen)
        {
            ResetGameplayInputState();
            wasFrozen = true;
            return;
        }

        // still frozen: do nothing
        if (frozenNow && wasFrozen)
        {
            return;
        }

        // just unfroze this frame
        if (!frozenNow && wasFrozen)
        {
            ResetGameplayInputState();
            wasFrozen = false;
        }

        // use the flag from CameraManager (set by camera zones)
        if (cameraManager != null)
            isFreeCamera = cameraManager.followModeFlag;

        HandleMovement();
        HandleFacing();
    }

    private void ResetGameplayInputState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        isRunning = false;
        isFocusing = false;

        keyCameraMap.Clear();

        stickHasLock = false;
        stickLockedCamera = null;
        stickLockedDir = Vector2.zero;

        lastMoveDirWorld = Vector3.zero;

        // keep lastRotation as-is so we don't snap the player

        // reset animation flags
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }
    }

    // =========================================================
    // === MOVEMENT ============================================
    // =========================================================
    void HandleMovement()
    {
        Vector3 moveWorld = Vector3.zero;

        // --- keyboard movement ---
        UpdateDirectionalKey(Key.W);
        UpdateDirectionalKey(Key.S);
        UpdateDirectionalKey(Key.A);
        UpdateDirectionalKey(Key.D);

        foreach (var kvp in keyCameraMap)
        {
            Vector3 localDir = LocalDirForKey(kvp.Key);  // magnitude 1 per key
            moveWorld += ConvertToWorld(localDir, kvp.Value);
        }

        bool hasKeyboardMove = keyCameraMap.Count > 0;

        // --- controller analog movement (fixed camera only) ---
        bool hasStickMove = moveInput.sqrMagnitude > 0.01f;
        if (hasStickMove)
        {
            Vector2 rawStick = moveInput;
            Vector2 stickDir2D = rawStick.normalized;

            if (!stickHasLock)
            {
                stickLockedDir = stickDir2D;
                stickLockedCamera = cameraManager != null ? cameraManager.ActiveCamera : Camera.main;
                stickHasLock = true;
            }
            else
            {
                float angle = Vector2.Angle(stickLockedDir, stickDir2D);
                if (angle > stickReacquireAngle)
                {
                    stickLockedDir = stickDir2D;
                    stickLockedCamera = cameraManager != null ? cameraManager.ActiveCamera : Camera.main;
                }
            }

            // only direction added to movement; magnitude handled separately
            Vector3 localStickDir = new Vector3(stickDir2D.x, 0f, stickDir2D.y);
            moveWorld += ConvertToWorld(localStickDir, stickLockedCamera);
        }
        else
        {
            stickHasLock = false;
        }

        // overall movement direction in world
        Vector3 moveDir = Vector3.zero;
        bool hasMove = moveWorld.sqrMagnitude > 0.0001f;
        if (hasMove)
        {
            moveDir = moveWorld.normalized;
            lastMoveDirWorld = moveDir;
        }
        else
        {
            lastMoveDirWorld = Vector3.zero;
        }

        // how strong the movement input is (for walk scaling)
        float moveAmount = 0f;
        if (hasStickMove && !hasKeyboardMove)
        {
            // stick magnitude 0..1
            moveAmount = Mathf.Clamp01(moveInput.magnitude);
        }
        else if (hasKeyboardMove)
        {
            // keyboard always full
            moveAmount = 1f;
        }
        else
        {
            moveAmount = 0f;
        }

        // run: if button is pressed and we are moving, we always run (full speed)
        bool allowRun = hasMove && isRunning;

        // drive animator (state + playback speed)
        if (animator != null)
        {
            animator.SetBool("IsWalking", hasMove);
            animator.SetBool("IsRunning", allowRun);

            // base animation speed:
            // - running: always 1x
            // - walking: scale with moveAmount (minimum a bit above 0 so it doesn't freeze)
            float animSpeed;
            if (allowRun)
            {
                animSpeed = 1f;
            }
            else if (hasMove)
            {
                // e.g. stick at 0.2 -> ~0.4x speed, stick at 1 -> 1x
                float minWalkAnimSpeed = 0.2f;
                animSpeed = Mathf.Lerp(minWalkAnimSpeed, 1f, moveAmount);
            }
            else
            {
                animSpeed = 1f; // idle at normal speed
            }

            animator.speed = animSpeed;
        }

        if (!hasMove) return;

        float focusMult = isFocusing ? 0.55f : 1f;

        float baseWalkSpeed = walkSpeed * focusMult;
        float baseRunSpeed  = runSpeed  * focusMult;

        // walking scaled by input, running always full speed
        float finalSpeed = allowRun
            ? baseRunSpeed
            : baseWalkSpeed * moveAmount;

        controller.SimpleMove(moveDir * finalSpeed);
    }

    // =========================================================
    // === FACING ==============================================
    // =========================================================
    void HandleFacing()
    {
        Camera cam = cameraManager != null ? cameraManager.ActiveCamera : Camera.main;
        if (cam == null) return;

        Vector3 newLookDir = Vector3.zero;
        bool gotInput = false;

        bool stickActive = lookInput.sqrMagnitude > 0.05f;
        bool mouseActive = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        if (stickActive)
        {
            activeLookSource = LookSource.Stick;
            lastLookTime = Time.time;
        }
        else if (mouseActive)
        {
            activeLookSource = LookSource.Mouse;
            lastLookTime = Time.time;
        }
        else if (Time.time - lastLookTime > 0.25f)
        {
            activeLookSource = LookSource.None;
        }

        // =====================================================
        // FIXED CAMERA MODE
        // =====================================================
        if (!isFreeCamera)
        {
            if (!isFocusing)
            {
                // rotation follows the actual movement direction from HandleMovement
                if (lastMoveDirWorld.sqrMagnitude > 0.001f)
                {
                    newLookDir = lastMoveDirWorld;
                    gotInput = true;
                }
            }
            else
            {
                // focus-based rotation
                if (activeLookSource == LookSource.Stick && stickActive)
                {
                    Vector3 camFwd = cam.transform.forward; camFwd.y = 0;
                    Vector3 camRight = cam.transform.right; camRight.y = 0;
                    camFwd.Normalize(); camRight.Normalize();
                    newLookDir = (camRight * lookInput.x + camFwd * lookInput.y).normalized;
                    gotInput = true;
                }
                else if (activeLookSource == LookSource.Mouse && mouseActive)
                {
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (new Plane(Vector3.up, transform.position).Raycast(ray, out float dist))
                    {
                        Vector3 hit = ray.GetPoint(dist);
                        newLookDir = hit - transform.position;
                        newLookDir.y = 0;
                        if (newLookDir.sqrMagnitude > 0.001f)
                            gotInput = true;
                    }
                }
            }
        }

        // =====================================================
        // FOLLOW CAMERA MODE
        // =====================================================
        else
        {
            // disable movement-based facing entirely
            float yawDelta = 0f;

            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                yawDelta += mouseDelta.x * rotationSpeed * 0.15f;
            }

            if (Mathf.Abs(lookInput.x) > 0.05f)
            {
                yawDelta += lookInput.x * rotationSpeed * 6f * Time.deltaTime;
            }

            if (Mathf.Abs(yawDelta) > 0.0001f)
            {
                transform.Rotate(Vector3.up, yawDelta);
                lastRotation = transform.rotation;
            }
            else
            {
                transform.rotation = lastRotation;
            }

            return; // skip rest of facing logic
        }

        // =====================================================
        // Apply rotation (fixed cam)
        // =====================================================
        if (gotInput)
        {
            // smooth but responsive rotation towards movement direction
            Quaternion target = Quaternion.LookRotation(newLookDir);
            float maxDegreesThisFrame = rotationSpeed * 90f * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxDegreesThisFrame);
            lastRotation = transform.rotation;
        }
        else
        {
            transform.rotation = lastRotation;
        }
    }

    // =========================================================
    // === INPUT CALLBACKS =====================================
    // =========================================================

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen) return;

        Vector2 raw = ctx.ReadValue<Vector2>();
        float mag = raw.magnitude;

        if (mag < moveDeadzone)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = raw;
        }
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen) return;
        lookInput = ctx.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen)
        {
            isRunning = false;
            return;
        }

        isRunning = ctx.ReadValueAsButton();
    }

    public void OnFocus(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen) return;

        bool newState = ctx.ReadValueAsButton();
        if (newState && !isFocusing)
            lastRotation = transform.rotation;
        isFocusing = newState;
    }

    public void OnFlashlight(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen) return;

        if (ctx.started)
        {
            flashlightOn = !flashlightOn;
            Debug.Log($"Flashlight toggled: {(flashlightOn ? "ON" : "OFF")}");
        }
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (IsGameplayFrozen) return;
        if (!ctx.performed)
            return;

        if (playerInteraction != null)
        {
            playerInteraction.HandleInteractInput();
        }
        else
        {
            Debug.LogWarning("OnInteract called but PlayerInteraction reference is missing.");
        }
    }

    public void OnInventory(InputAction.CallbackContext ctx)
    {
        // Inventory should work even when paused (it is what toggles pause for inventory)
        if (!ctx.performed)
            return;

        Debug.Log("[PlayerController] Inventory action performed");

        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv != null)
        {
            inv.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("[PlayerController] No PlayerInventory found on this GameObject");
        }
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        // Same idea: pause menu can still open/close even if gameplay is frozen
        if (ctx.performed)
            Debug.Log("Pause menu triggered");
    }

    // =========================================================
    // === HELPERS =============================================
    // =========================================================
    void UpdateDirectionalKey(Key key)
    {
        if (Keyboard.current == null) return;
        bool pressed = Keyboard.current[key].isPressed;

        if (pressed)
        {
            if (!keyCameraMap.ContainsKey(key))
            {
                Camera active = cameraManager != null ? cameraManager.ActiveCamera : Camera.main;
                keyCameraMap[key] = active;
            }
        }
        else
        {
            if (keyCameraMap.ContainsKey(key))
                keyCameraMap.Remove(key);
        }
    }

    Vector3 LocalDirForKey(Key key) => key switch
    {
        Key.W => Vector3.forward,
        Key.S => Vector3.back,
        Key.A => Vector3.left,
        Key.D => Vector3.right,
        _ => Vector3.zero
    };

    Vector3 ConvertToWorld(Vector3 local, Camera cam)
    {
        if (cam == null) return local;
        Vector3 fwd = cam.transform.forward;
        Vector3 right = cam.transform.right;
        fwd.y = 0; right.y = 0;
        fwd.Normalize(); right.Normalize();
        return (fwd * local.z + right * local.x);
    }
}
