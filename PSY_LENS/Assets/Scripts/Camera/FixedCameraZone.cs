// FixedCameraZone.cs
// Activates a specific fixed camera when the player enters its trigger
// Psy-Lens

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FixedCameraZone : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public CameraManager cameraManager;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && cameraManager != null && targetCamera != null)
        {
            Debug.Log($"[CameraZone] Switching to FIXED camera: {targetCamera.name}");
            cameraManager.SwitchToFixed(targetCamera);
            cameraManager.followModeFlag = false; // <── flag fixed mode
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
