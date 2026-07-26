using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class ButtonPulse : MonoBehaviour
    {
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _ease = Ease.InOutSine;
        [SerializeField] private bool _playOnEnable = true;

        private Vector3 _baseScale;
        private Tween _pulseTween;

        private void Awake() => _baseScale = transform.localScale;

        private void OnEnable()
        {
            if (_playOnEnable)
                Play();
        }

        private void OnDisable() => Stop();

        public void Play()
        {
            Stop();
            transform.localScale = _baseScale;
            _pulseTween = transform
                .DOScale(_baseScale * _scaleMultiplier, _duration)
                .SetEase(_ease)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        public void Stop()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            transform.localScale = _baseScale;
        }
    }
}
