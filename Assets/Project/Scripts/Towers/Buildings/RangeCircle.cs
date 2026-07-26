using UnityEngine;

namespace Towers.Buildings
{
    [RequireComponent(typeof(LineRenderer))]
    public class RangeCircle : MonoBehaviour
    {
        [SerializeField] private int _segments = 64;

        private LineRenderer _lineRenderer;

        private LineRenderer LineRenderer
        {
            get
            {
                if (_lineRenderer == null)
                {
                    _lineRenderer = GetComponent<LineRenderer>();
                }
                return _lineRenderer;
            }
        }

        public void Show(float radius, Color color)
        {
            DrawCircle(radius);
            LineRenderer.startColor = color;
            LineRenderer.endColor = color;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void DrawCircle(float radius)
        {
            LineRenderer.loop = true;
            LineRenderer.useWorldSpace = false;
            LineRenderer.positionCount = _segments;

            for (int i = 0; i < _segments; i++)
            {
                float angle = 2f * Mathf.PI * i / _segments;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                LineRenderer.SetPosition(i, point);
            }
        }
    }
}
