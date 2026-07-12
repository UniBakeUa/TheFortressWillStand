using System;
using UnityEngine;

namespace UI
{
    public class UIHudHandler : MonoBehaviour
    {
        [SerializeField] Animator animator;

        private void Start()
        {
            animator.SetBool("Building",true);
            GameStateManager.Instance.OnStateChange += ToggleAnimation;
        }

        private void ToggleAnimation(GameState state)
        {
            if(state == GameState.Building)
                animator.SetBool("Building",true);
            else
                animator.SetBool("Building",false);
        }
    }
}