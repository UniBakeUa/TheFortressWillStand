using UnityEngine;

namespace Towers.Buildings
{
    public class TimedVisualEffect : MonoBehaviour
    {
        [SerializeField] private float _visibleDuration = 0.1f;

        private float _hideTimer;
        private bool _isVisible;

        public void Show()
        {
            gameObject.SetActive(true);
            _isVisible = true;
            _hideTimer = _visibleDuration;
        }

        private void Update()
        {
            if (!_isVisible) return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
            {
                _isVisible = false;
                gameObject.SetActive(false);
            }
        }
    }
}
