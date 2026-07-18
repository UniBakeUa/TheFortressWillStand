using UnityEngine;
using UnityEngine.UI;

namespace Towers.UI
{
    public class TurretView : BuildingView
    {
        [SerializeField] private Slider _cdSlider;

        public void SetupTimer(float maxValue)
        {
            _cdSlider.maxValue = maxValue;
            _cdSlider.value = 0f;
        }

        public void UpdateMoneyTimer(float value)
        {
            _cdSlider.value = value;
        }
    }
}