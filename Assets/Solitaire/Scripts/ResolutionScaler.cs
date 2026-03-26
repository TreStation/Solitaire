using UnityEngine;

public class ResolutionScaler : MonoBehaviour
{
    void Start()
    {
        float dpi = Screen.dpi;

        switch (dpi)
        {
            case < 200:
                QualitySettings.resolutionScalingFixedDPIFactor = 0.85f;
                break;
            case < 300:
                QualitySettings.resolutionScalingFixedDPIFactor = 0.95f;
                break;
            default:
                QualitySettings.resolutionScalingFixedDPIFactor = 1.0f;
                break;
        }
    }
}
