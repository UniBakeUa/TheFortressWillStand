using System.Collections;
using UnityEngine;

namespace Managers
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private float _magnitude = 0.15f;

        private Vector3 _originalLocalPosition;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            Instance = this;
            _originalLocalPosition = transform.localPosition;
        }

        public void Shake()
        {
            Shake(_duration, _magnitude);
        }

        public void Shake(float duration, float magnitude)
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
            }
            else
            {
                _originalLocalPosition = transform.localPosition;
            }

            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);

                Vector2 offset = Random.insideUnitCircle * magnitude * damper;
                transform.localPosition = _originalLocalPosition + new Vector3(offset.x, offset.y, 0f);

                yield return null;
            }

            transform.localPosition = _originalLocalPosition;
            _shakeRoutine = null;
        }
    }
}
