using UnityEngine;
using Money;
using Core;
using System;

namespace Items
{
    public class ClickableItem : MonoBehaviour
    {
        [SerializeField] protected int moneyValue = 10;

        protected Action<ClickableItem> _onItemClicked;

        public void Init(Action<ClickableItem> returnAction)
        {
            _onItemClicked = returnAction;
        }

        protected virtual void OnMouseDown()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            MoneyManager.Instance.AddMoney(moneyValue);

            _onItemClicked?.Invoke(this);
        }
    }
}