using UnityEngine;
using UnityEngine.UI;

namespace Towers.UI
{
    public class BuildingView : MonoBehaviour
    {
        [SerializeField] protected Slider _healthSlider;

        public void SetupHealth(float maxHealth)
        {
            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = maxHealth;
        }

        public void UpdateHealth(float currentHealth)
        {
            _healthSlider.value = currentHealth;
        }
    }
}