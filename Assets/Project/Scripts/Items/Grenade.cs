using System;
using UnityEngine;

namespace Items
{
    public class Grenade : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _shadow;
        [SerializeField] private float _flightDuration = 0.6f;
        [SerializeField] private float _arcHeight = 2f;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _elapsed;
        private Action _onImpact;

        public void Launch(Vector3 targetPosition, Action onImpact)
        {
            _startPosition = transform.position;
            _targetPosition = targetPosition;
            _onImpact = onImpact;
            _elapsed = 0f;

            if (_shadow != null)
            {
                _shadow.position = _startPosition;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightDuration);

            Vector3 groundPosition = Vector3.Lerp(_startPosition, _targetPosition, t);
            float height = 4f * t * (1f - t) * _arcHeight;

            transform.position = groundPosition;

            if (_visual != null)
            {
                _visual.localPosition = new Vector3(0f, height, 0f);
            }

            if (_shadow != null)
            {
                _shadow.position = groundPosition;
            }

            if (t >= 1f)
            {
                _onImpact?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
