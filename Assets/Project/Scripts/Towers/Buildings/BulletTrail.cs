using UnityEngine;

namespace Towers.Buildings
{
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTrail : MonoBehaviour
    {
        [SerializeField] private float _visibleDuration = 0.1f;

        private LineRenderer _lineRenderer;
        private float _hideTimer;
        private bool _isVisible;

        public void Show(Vector3 from, Vector3 to)
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            _lineRenderer.SetPosition(0, from);
            _lineRenderer.SetPosition(1, to);

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
