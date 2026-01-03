using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonGateInteractable : Interactable
{
    [Header("Gate")]
    [SerializeField] private GateMover gate;

    [Header("Visual")]
    [SerializeField] private Transform visual;     // the part that moves
    [SerializeField] private float pressDistance = 0.05f;

    public enum Axis { X, Y, Z }

    [Header("Press Axis")]
    [SerializeField] private Axis moveAxis = Axis.Z;
    [Tooltip("+1 or -1: along +axis or -axis")]
    [SerializeField] private int direction = -1;

    [Header("State")]
    [SerializeField] private bool isPressed = false;

    // stored unpressed local position (base)
    [SerializeField, HideInInspector] private Vector3 baseLocalPos;
    [SerializeField, HideInInspector] private bool hasBase = false;
    [SerializeField, HideInInspector] private bool lastIsPressed = false;

    private void Reset()
    {
        if (visual == null)
            visual = transform;

        baseLocalPos = visual.localPosition;
        hasBase = true;
        lastIsPressed = isPressed;
        ApplyPose();
    }

    private void OnValidate()
    {
        if (visual == null)
            visual = transform;

        // In edit mode, handle toggles and distance changes
        if (!Application.isPlaying)
        {
            if (!hasBase)
            {
                baseLocalPos = visual.localPosition;
                hasBase = true;
            }

            // Detect toggle in inspector
            if (isPressed != lastIsPressed)
            {
                if (isPressed)
                {
                    // false -> true : capture current as base
                    baseLocalPos = visual.localPosition;
                }
                else
                {
                    // true -> false : go back to base
                    visual.localPosition = baseLocalPos;
                }

                lastIsPressed = isPressed;
            }

            // If currently pressed, update offset (e.g. when pressDistance changes)
            if (isPressed)
                ApplyPose();
        }
    }

    private void ApplyPose()
    {
        if (!hasBase || visual == null)
            return;

        Vector3 axisVector = Vector3.zero;
        switch (moveAxis)
        {
            case Axis.X: axisVector = Vector3.right;  break;
            case Axis.Y: axisVector = Vector3.up;     break;
            case Axis.Z: axisVector = Vector3.forward;break;
        }

        float sign = direction >= 0 ? 1f : -1f;
        Vector3 offset = axisVector * pressDistance * sign;

        visual.localPosition = baseLocalPos + offset;
    }

    public override void Interact()
    {
        if (!IsInteractable)
            return;

        if (gate != null)
            gate.ToggleGate();

        // Runtime toggle behaves like inspector toggle
        if (!hasBase)
        {
            baseLocalPos = visual.localPosition;
            hasBase = true;
        }

        isPressed = !isPressed;

        if (isPressed)
        {
            // capture current as base, then apply offset
            baseLocalPos = visual.localPosition;
            ApplyPose();
        }
        else
        {
            // return to base
            visual.localPosition = baseLocalPos;
        }
    }
}
