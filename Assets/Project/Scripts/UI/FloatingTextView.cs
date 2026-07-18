using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace UI
{
    public class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textMesh;

        [Header("Animation Settings")]
        [SerializeField] private float _moveYDistance = 1.5f;
        [SerializeField] private float _duration = 1f;

        private Action<FloatingTextView> _onAnimationFinished;

        public void Setup(int amount, Action<FloatingTextView> onComplete)
        {
            if(amount > 0)
                _textMesh.text = $"+{amount}";
            else
                _textMesh.text = $"{amount}";

            Color color = _textMesh.color;
            color.a = 1f;
            _textMesh.color = color;

            Animate(onComplete);
        }

        private void Animate(Action<FloatingTextView> onCompleteAction)
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Join(transform.DOMoveY(transform.position.y + _moveYDistance, _duration)
                    .SetEase(Ease.OutCubic));

            sequence.Join(_textMesh.DOFade(0f, _duration)
                    .SetEase(Ease.InCubic));

            sequence.OnComplete(() =>
            {
                onCompleteAction?.Invoke(this);
            });
        }

        private void OnDestroy()
        {
            transform.DOKill();
            _textMesh.DOKill();
        }
    }
}