using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIHudHandler : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _showPosition = 225f;
        [SerializeField] private float _hidePosition = -750f;

        [Header("Build Animation Settings")]
        [SerializeField] private float _showBuildPosition = 225f;
        [SerializeField] private float _hideBuildPosition = -750f;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text _buttonText;
        [SerializeField] private Transform _buttonsTransform;

        private bool _isVisible = false;

        private void Start()
        {
            GameStateManager.Instance.OnStateChange += ToggleAnimation;
        }

        private void ToggleAnimation(GameState state)
        {
            if(state == GameState.Building)
                transform.DOLocalMoveY(_showPosition, 0.5f);
            else
                transform.DOLocalMoveY(_hidePosition, 0.5f);
        }

        public void ToogleHUD()
        {
            string buttonText = _isVisible ? "▼" : "▲";
            float targetPosition = _isVisible ? _showBuildPosition : _hideBuildPosition;
            _buttonText.text = buttonText;
            _buttonsTransform.DOLocalMoveY(targetPosition, 0.5f);

            _isVisible = !_isVisible;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChange -= ToggleAnimation;
        }
    }
}