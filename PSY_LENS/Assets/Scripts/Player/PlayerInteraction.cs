using UnityEngine;

// PlayerInteraction.cs
// Frustum-based interaction volume (configurable near/far size + offset)
// Checks overlap between player frustum and each item's interaction sphere
// Input-agnostic: interaction is triggered by calling HandleInteractInput()
// Psy-Lens

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Origin")]
    [SerializeField] private Transform detectionOrigin;   // usually a child in front of the chest
    [SerializeField] private LayerMask detectionMask = ~0;

    [Header("Frustum Settings")]
    [Tooltip("Distance from origin where the frustum starts.")]
    [SerializeField] private float nearDistance = 0.2f;

    [Tooltip("Distance from origin where the frustum ends.")]
    [SerializeField] private float farDistance = 2.0f;

    [Tooltip("Half-width (X) and half-height (Y) at the near plane.")]
    [SerializeField] private Vector2 nearSize = new Vector2(0.1f, 0.2f);

    [Tooltip("Half-width (X) and half-height (Y) at the far plane.")]
    [SerializeField] private Vector2 farSize = new Vector2(0.8f, 1.0f);

    [Tooltip("Local offset of the whole frustum from the detection origin (x=right, y=up, z=forward).")]
    [SerializeField] private Vector3 frustumOffset = Vector3.zero;

    private Interactable currentTarget;
    private Interactable lastTarget; // for debug

    private void Awake()
    {
        if (detectionOrigin == null)
            detectionOrigin = transform;
    }

    private void Update()
    {
        UpdateCurrentTarget();

        // Debug: log when target changes
        if (currentTarget != lastTarget)
        {
            if (currentTarget == null)
                Debug.Log("Current target: NONE");
            else
                Debug.Log("Current target: " + currentTarget.name);

            lastTarget = currentTarget;
        }
    }

    /// <summary>
    /// Called from PlayerController when the Interact input action is performed.
    /// </summary>
    public void HandleInteractInput()
    {
        Debug.Log("Interact input received");

        if (currentTarget != null)
        {
            Debug.Log("Calling Interact() on: " + currentTarget.name);
            currentTarget.Interact();
        }
        else
        {
            Debug.Log("No target to interact with");
        }
    }

    private void UpdateCurrentTarget()
    {
        Transform origin = detectionOrigin != null ? detectionOrigin : transform;

        float near = Mathf.Max(0f, nearDistance);
        float far = Mathf.Max(near + 0.01f, farDistance); // ensure far > near

        // Broad-phase: everything within max distance
        Collider[] hits = Physics.OverlapSphere(
            origin.position,
            far,
            detectionMask
        );

        Interactable best = null;
        float bestSqrDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<Interactable>();
            if (interactable == null)
                continue;

            // Respect the per-item boolean
            if (!interactable.IsInteractable)
                continue;

            // Item's interaction sphere
            Vector3 center = interactable.GetInteractionGridCenter();
            float radius = interactable.InteractionRadius;

            // Check sphere–frustum overlap
            if (!SphereIntersectsFrustum(origin, center, radius, near, far))
                continue;

            float sqrDist = (center - origin.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = interactable;
            }
        }

        currentTarget = best;
    }

    /// <summary>
    /// Approximate test: does a sphere (center, radius) intersect the frustum
    /// defined by near/far distances and interpolated width/height?
    /// This version ignores any scale on 'origin' and uses only position + rotation.
    /// </summary>
    private bool SphereIntersectsFrustum(Transform origin, Vector3 worldPos, float radius, float near, float far)
    {
        // Convert worldPos into an "origin-local" space using only origin's rotation,
        // i.e. same basis we implicitly use when drawing the gizmos.
        Vector3 toPoint = worldPos - origin.position;

        float lx = Vector3.Dot(toPoint, origin.right);
        float ly = Vector3.Dot(toPoint, origin.up);
        float lz = Vector3.Dot(toPoint, origin.forward);

        Vector3 local = new Vector3(lx, ly, lz);

        // Shift by frustum offset
        local -= frustumOffset;

        // Depth range with radius taken into account
        float minZ = local.z - radius;
        float maxZ = local.z + radius;

        if (maxZ < near || minZ > far)
            return false;

        // Choose a representative depth clamped in [near, far]
        float z = Mathf.Clamp(local.z, near, far);
        float t = (z - near) / (far - near);

        // Frustum half-width/half-height at that depth
        float halfWidth = Mathf.Lerp(nearSize.x, farSize.x, t);
        float halfHeight = Mathf.Lerp(nearSize.y, farSize.y, t);

        // Expand by radius to approximate sphere overlap
        halfWidth += radius;
        halfHeight += radius;

        // Check horizontal/vertical bounds against expanded frustum
        if (Mathf.Abs(lx - frustumOffset.x) > halfWidth)
            return false;

        if (Mathf.Abs(ly - frustumOffset.y) > halfHeight)
            return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        Transform origin = detectionOrigin != null ? detectionOrigin : transform;
        Vector3 originPos = origin.position;

        float near = Mathf.Max(0f, nearDistance);
        float far = Mathf.Max(near + 0.01f, farDistance);

        // Center forward line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(originPos, originPos + origin.forward * far);

        // Build frustum corners in local space (including offset)
        Vector3[] localCorners = new Vector3[8];
        Vector3 offset = frustumOffset;

        // Near plane (local space)
        localCorners[0] = offset + new Vector3(-nearSize.x,  nearSize.y, near); // top-left near
        localCorners[1] = offset + new Vector3( nearSize.x,  nearSize.y, near); // top-right near
        localCorners[2] = offset + new Vector3( nearSize.x, -nearSize.y, near); // bottom-right near
        localCorners[3] = offset + new Vector3(-nearSize.x, -nearSize.y, near); // bottom-left near

        // Far plane (local space)
        localCorners[4] = offset + new Vector3(-farSize.x,  farSize.y, far); // top-left far
        localCorners[5] = offset + new Vector3( farSize.x,  farSize.y, far); // top-right far
        localCorners[6] = offset + new Vector3( farSize.x, -farSize.y, far); // bottom-right far
        localCorners[7] = offset + new Vector3(-farSize.x, -farSize.y, far); // bottom-left far

        // Transform to world space (position + rotation only)
        Matrix4x4 m = Matrix4x4.TRS(originPos, origin.rotation, Vector3.one);
        Vector3[] worldCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
            worldCorners[i] = m.MultiplyPoint3x4(localCorners[i]);

        Gizmos.color = Color.cyan;

        // Near rectangle
        Gizmos.DrawLine(worldCorners[0], worldCorners[1]);
        Gizmos.DrawLine(worldCorners[1], worldCorners[2]);
        Gizmos.DrawLine(worldCorners[2], worldCorners[3]);
        Gizmos.DrawLine(worldCorners[3], worldCorners[0]);

        // Far rectangle
        Gizmos.DrawLine(worldCorners[4], worldCorners[5]);
        Gizmos.DrawLine(worldCorners[5], worldCorners[6]);
        Gizmos.DrawLine(worldCorners[6], worldCorners[7]);
        Gizmos.DrawLine(worldCorners[7], worldCorners[4]);

        // Connect near and far
        Gizmos.DrawLine(worldCorners[0], worldCorners[4]);
        Gizmos.DrawLine(worldCorners[1], worldCorners[5]);
        Gizmos.DrawLine(worldCorners[2], worldCorners[6]);
        Gizmos.DrawLine(worldCorners[3], worldCorners[7]);

        // Show current target interaction point in green while playing
        if (Application.isPlaying && currentTarget != null)
        {
            Vector3 anchor = currentTarget.GetInteractionGridCenter();
            Gizmos.color = Color.green;
            Gizmos.DrawLine(originPos, anchor);
            Gizmos.DrawWireSphere(anchor, 0.08f);
        }
    }
}
