using UnityEngine;
using UnityEngine.UI;

namespace Towers.UI
{
    public class DrillView : BuildingView
    {
        [SerializeField] private Slider _moneyTimerSlider;

        public void SetupTimer(float maxValue)
        {
            _moneyTimerSlider.maxValue = maxValue;
            _moneyTimerSlider.value = 0f;
        }

        public void UpdateMoneyTimer(float value)
        {
            _moneyTimerSlider.value = value;
        }
    }
}