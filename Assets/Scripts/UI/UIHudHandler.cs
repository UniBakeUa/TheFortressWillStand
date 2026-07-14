using DG.Tweening;
using System;
using UnityEngine;

namespace UI
{
    public class UIHudHandler : MonoBehaviour
    {
        [SerializeField] private float _showPosition = 225f;
        [SerializeField] private float _hidePosition = -650f;

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

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChange -= ToggleAnimation;
        }
    }
}