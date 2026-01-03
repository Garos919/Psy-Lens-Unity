// FollowCameraZone.cs
// Activates follow camera when the player enters its trigger
// Psy-Lens

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FollowCameraZone : MonoBehaviour
{
    [Header("References")]
    public CameraManager cameraManager;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && cameraManager != null)
        {
            Debug.Log("[CameraZone] Switching to FOLLOW camera");
            cameraManager.SwitchToFollow();
            cameraManager.followModeFlag = true; // <── flag follow mode
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
    
}
