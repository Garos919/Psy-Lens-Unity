using UnityEngine;

[ExecuteAlways]
public class GateMover : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How far up the gate moves when fully open.")]
    [SerializeField] private float openHeight = 3f;

    [Tooltip("Movement speed in units per second (Play mode).")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("State")]
    [Tooltip("Preview / runtime state: true = open, false = closed.")]
    [SerializeField] private bool isOpen = false;

    [SerializeField, HideInInspector] private Vector3 closedPosition;
    [SerializeField, HideInInspector] private bool hasClosedPose = false;

    private bool _isMovingRuntime = false;

    private void Reset()
    {
        // Called when you first add the component or press Reset in the Inspector.
        closedPosition = transform.position;
        hasClosedPose = true;
        ApplyEditorPose();
    }

    private void OnEnable()
    {
        if (!hasClosedPose)
        {
            closedPosition = transform.position;
            hasClosedPose = true;
        }

        ApplyEditorPose();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (!hasClosedPose)
            {
                closedPosition = transform.position;
                hasClosedPose = true;
            }

            ApplyEditorPose();
        }
    }

    /// <summary>
    /// Use the current Scene position as the new closed pose.
    /// Call this from a context menu if you want.
    /// </summary>
    [ContextMenu("Capture Closed From Current")]
    private void CaptureClosedFromCurrent()
    {
        closedPosition = transform.position;
        hasClosedPose = true;
        ApplyEditorPose();
    }

    /// <summary>
    /// Applies closed/open pose in edit mode so you can see it on the fly.
    /// </summary>
    private void ApplyEditorPose()
    {
        if (!enabled || !hasClosedPose)
            return;

        Vector3 openPos = closedPosition + Vector3.up * openHeight;
        transform.position = isOpen ? openPos : closedPosition;
    }

    /// <summary>
    /// Called by the button in Play mode to toggle the gate.
    /// </summary>
    public void ToggleGate()
    {
        if (!hasClosedPose)
        {
            closedPosition = transform.position;
            hasClosedPose = true;
        }

        if (!Application.isPlaying)
        {
            // In edit mode, just flip the flag and update pose immediately.
            isOpen = !isOpen;
            ApplyEditorPose();
            return;
        }

        // In play mode: animate between closedPosition and openPosition.
        isOpen = !isOpen;
        _isMovingRuntime = true;
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!_isMovingRuntime || !hasClosedPose)
            return;

        Vector3 targetClosed = closedPosition;
        Vector3 targetOpen = closedPosition + Vector3.up * openHeight;
        Vector3 target = isOpen ? targetOpen : targetClosed;

        if (moveSpeed <= 0f)
        {
            transform.position = target;
            _isMovingRuntime = false;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if ((transform.position - target).sqrMagnitude < 0.000001f)
        {
            transform.position = target;
            _isMovingRuntime = false;
        }
    }
}
