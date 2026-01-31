using UnityEngine;

public class ResolutionScaler : MonoBehaviour
{
    void Start()
    {
        float dpi = Screen.dpi;

        if (dpi < 200)
            QualitySettings.resolutionScalingFixedDPIFactor = 0.85f; // Low-end devices
        else if (dpi < 300)
            QualitySettings.resolutionScalingFixedDPIFactor = 0.95f; // Mid-range
        else
            QualitySettings.resolutionScalingFixedDPIFactor = 1.0f; // High-end & tablets
    }
}
