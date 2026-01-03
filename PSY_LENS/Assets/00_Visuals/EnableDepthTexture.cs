using UnityEngine;

/// Put this on the fog plane object (the one with the fog shader).
/// It will automatically enable depth texture on any camera that renders this plane.
[ExecuteAlways]
public class FogDepthEnabler : MonoBehaviour
{
    void OnEnable()
    {
        Camera.onPreCull += OnPreCull;
    }

    void OnDisable()
    {
        Camera.onPreCull -= OnPreCull;
    }

    void OnPreCull(Camera cam)
    {
        int fogLayer = gameObject.layer;

        // If this camera renders the fog plane's layer, enable depth on it
        if ((cam.cullingMask & (1 << fogLayer)) != 0)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
