using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private bool isInteractable = true;   // tick box in inspector
    [SerializeField] private string promptText = "Interact";

    [Header("Interaction Grid (Sphere)")]
    [Tooltip("Optional transform used as the interaction grid center instead of the object pivot.")]
    [SerializeField] private Transform interactionCenter;

    [Tooltip("If no interactionCenter is set, this local offset (x=right, y=up, z=forward) from the object pivot is used.")]
    [SerializeField] private Vector3 interactionOffset = Vector3.zero;

    [Tooltip("Radius of this item's interaction grid (used for overlap with the player frustum).")]
    [SerializeField] private float interactionRadius = 0.2f;

    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    public string PromptText => promptText;

    /// <summary>
    /// World-space center of this item's interaction sphere/grid.
    /// PlayerInteraction uses this instead of the pivot.
    /// </summary>
    public Vector3 GetInteractionGridCenter()
    {
        if (interactionCenter != null)
            return interactionCenter.position;

        return transform.TransformPoint(interactionOffset);
    }

    public float InteractionRadius => interactionRadius;

    public virtual void Interact()
    {
        Debug.Log($"Interact() called on {name}. isInteractable = {isInteractable}");

        if (!isInteractable)
            return;

        Debug.Log($"Interacted with: {name}");
        // Later: override or extend this per item (pickup, door, clue, etc.)
    }

    private void OnDrawGizmos()
    {
        Vector3 center = interactionCenter != null
            ? interactionCenter.position
            : transform.TransformPoint(interactionOffset);

        Gizmos.color = isInteractable ? Color.magenta : Color.gray;
        Gizmos.DrawWireSphere(center, interactionRadius);
    }
}
