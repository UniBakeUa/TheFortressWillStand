using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WaveTimerView : MonoBehaviour
    {
        [SerializeField] private Slider _waveTimerSlider;

        private void Start()
        {
            HideTimer();
        }

        public void ShowTimer(float duration)
        {
            _waveTimerSlider.maxValue = duration;
            _waveTimerSlider.value = duration;

            gameObject.SetActive(true);
        }

        public void UpdateTimer(float timeRemaining)
        {
            _waveTimerSlider.value = timeRemaining;
        }

        public void HideTimer()
        {
            _waveTimerSlider.value = 0;

            gameObject.SetActive(false);
        }
    }
}