// PickupProximityIndicator.cs
// Attach directly to a pickable item.
// Checks distance to Player and shows a small sphere indicator when inside radius.
// Draws a red gizmo for the radius.
// Psy-Lens

using UnityEngine;

public class PickupProximityIndicator : MonoBehaviour
{
    [Header("Proximity Settings")]
    [SerializeField] private float radius = 1.0f;
    [SerializeField] private string playerTag = "Player";

    [Header("Indicator")]
    [SerializeField] private GameObject indicatorPrefab;              // optional: custom glow/icon
    [SerializeField] private Vector3 indicatorLocalOffset = new Vector3(0f, 0.4f, 0f);

    [Header("Auto Indicator (if no prefab)")]
    [SerializeField] private float autoIndicatorScale = 0.2f;
    [SerializeField] private Color autoIndicatorColor = Color.yellow;

    private Transform player;
    private GameObject indicatorInstance;
    private bool isInside = false;

    private void Awake()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
                return;
        }

        float sqrDist = (player.position - transform.position).sqrMagnitude;
        float sqrRadius = radius * radius;

        bool shouldBeInside = sqrDist <= sqrRadius;

        if (shouldBeInside != isInside)
        {
            isInside = shouldBeInside;

            if (isInside)
                ShowIndicator();
            else
                HideIndicator();
        }
    }

    private void FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            player = go.transform;
    }

    private void ShowIndicator()
    {
        if (indicatorInstance == null)
            CreateIndicator();

        if (indicatorInstance != null)
            indicatorInstance.SetActive(true);
    }

    private void HideIndicator()
    {
        if (indicatorInstance != null)
            indicatorInstance.SetActive(false);
    }

    private void CreateIndicator()
    {
        if (indicatorPrefab != null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, transform);
            indicatorInstance.transform.localPosition = indicatorLocalOffset;
            return;
        }

        // Auto-create small sphere
        indicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicatorInstance.name = "Pickup_AutoIndicator";
        indicatorInstance.transform.SetParent(transform, false);
        indicatorInstance.transform.localPosition = indicatorLocalOffset;
        indicatorInstance.transform.localScale = Vector3.one * autoIndicatorScale;

        // Remove collider so it doesn't interfere
        Collider c = indicatorInstance.GetComponent<Collider>();
        if (c != null)
            Destroy(c);

        Renderer r = indicatorInstance.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = autoIndicatorColor;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
