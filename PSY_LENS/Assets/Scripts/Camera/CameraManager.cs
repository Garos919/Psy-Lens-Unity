// CameraManager.cs
// Controls switching between fixed and follow cameras + global flag
// Psy-Lens

using UnityEngine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    public Camera followCamera;

    [Header("State")]
    [SerializeField] private Camera activeCamera;
    private readonly List<Camera> allCameras = new();

    public Camera ActiveCamera => activeCamera;

    // enum for mode tracking
    public enum CameraMode { Fixed, Follow }
    public CameraMode ActiveMode { get; private set; } = CameraMode.Fixed;

    [HideInInspector] public bool followModeFlag = false; // explicit flag for PlayerController

    void Awake()
    {
        allCameras.Clear();
        allCameras.AddRange(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None));

        foreach (var cam in allCameras)
            if (cam != null) cam.enabled = false;

        if (followCamera != null && !allCameras.Contains(followCamera))
            allCameras.Add(followCamera);
    }

    // =========================================================
    // === CAMERA SWITCHES ===
    // =========================================================
    public void SwitchToFixed(Camera targetCam)
    {
        if (targetCam == null) return;

        foreach (var cam in allCameras)
            if (cam != null) cam.enabled = false;

        targetCam.gameObject.SetActive(true);
        targetCam.enabled = true;
        activeCamera = targetCam;
        ActiveMode = CameraMode.Fixed;
        followModeFlag = false;

        Debug.Log($"[CameraManager] Active camera set to FIXED: {targetCam.name}");
    }

    public void SwitchToFollow()
    {
        if (followCamera == null) return;

        foreach (var cam in allCameras)
            if (cam != null) cam.enabled = false;

        followCamera.gameObject.SetActive(true);
        followCamera.enabled = true;
        activeCamera = followCamera;
        ActiveMode = CameraMode.Follow;
        followModeFlag = true;

        Debug.Log("[CameraManager] Switched to FOLLOW camera");
    }
}
