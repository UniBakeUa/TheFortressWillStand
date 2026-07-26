using Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Towers.UI
{
    public class BuildingView : MonoBehaviour, IDamageFlashTarget
    {
        [SerializeField] protected Slider _healthSlider;
        [SerializeField] private Gradient _healthColorGradient;
        [SerializeField] private Color _damageFlashColor = new Color(0.6f, 0.2f, 0.9f, 1f);
        [SerializeField] private float _damageFlashDuration = 0.05f;
        [SerializeField] private float _damageShakeOffset = 8f;

        private Image _healthFillImage;
        private RectTransform _healthSliderRect;
        private Vector2 _healthSliderOriginalPosition;
        private float _lastKnownHealth = float.NaN;
        private Coroutine _damageFlashRoutine;

        public void SetupHealth(float maxHealth)
        {
            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = maxHealth;

            if (_healthFillImage == null && _healthSlider.fillRect != null)
            {
                _healthFillImage = _healthSlider.fillRect.GetComponent<Image>();
            }

            if (_healthSliderRect == null)
            {
                _healthSliderRect = _healthSlider.GetComponent<RectTransform>();
                _healthSliderOriginalPosition = _healthSliderRect.anchoredPosition;
            }

            _lastKnownHealth = maxHealth;
            UpdateHealthColor(1f);
        }

        public void UpdateHealth(float currentHealth)
        {
            _healthSlider.value = currentHealth;

            float fraction = _healthSlider.maxValue > 0f ? currentHealth / _healthSlider.maxValue : 0f;

            if (currentHealth < _lastKnownHealth)
            {
                _damageFlashRoutine = DamageFlashEffect.Play(
                    this, this, _damageFlashRoutine, _damageFlashColor, _damageFlashDuration, _damageFlashDuration, _damageShakeOffset,
                    onComplete: () => _damageFlashRoutine = null);
            }
            else
            {
                UpdateHealthColor(fraction);
            }

            _lastKnownHealth = currentHealth;
        }

        public void SetFlashColor(Color color)
        {
            if (_healthFillImage != null)
            {
                _healthFillImage.color = color;
            }
        }

        public void ResetColor()
        {
            float fraction = _healthSlider.maxValue > 0f ? _healthSlider.value / _healthSlider.maxValue : 0f;
            UpdateHealthColor(fraction);
        }

        public void Shake(Vector2 offset)
        {
            if (_healthSliderRect != null)
            {
                _healthSliderRect.anchoredPosition = _healthSliderOriginalPosition + offset;
            }
        }

        public void ResetPosition()
        {
            if (_healthSliderRect != null)
            {
                _healthSliderRect.anchoredPosition = _healthSliderOriginalPosition;
            }
        }

        private void UpdateHealthColor(float fraction)
        {
            if (_healthFillImage == null) return;
            _healthFillImage.color = _healthColorGradient.Evaluate(Mathf.Clamp01(fraction));
        }
    }
}
