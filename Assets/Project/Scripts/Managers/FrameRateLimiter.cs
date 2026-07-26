using UnityEngine;

namespace Managers
{
    public class FrameRateLimiter : MonoBehaviour
    {
        private enum FrameRateCap
        {
            Fps60 = 60,
            Fps90 = 90,
            Fps120 = 120
        }

        [SerializeField] private FrameRateCap _targetFrameRate = FrameRateCap.Fps60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = (int)_targetFrameRate;
        }
    }
}
