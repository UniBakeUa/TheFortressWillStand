using UnityEngine;
using System;
using Managers;

namespace Items
{
    public class ClickableItem : MonoBehaviour
    {
        [SerializeField] protected int moneyValue = 10;

        protected Action<ClickableItem> _onItemFinished;

        public void Init(Action<ClickableItem> returnAction)
        {
            _onItemFinished = returnAction;
        }

        protected void GiveReward()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            MoneyManager.Instance.AddMoney(moneyValue);
        }

        protected virtual void OnMouseDown()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;

            Collect();
            Debug.Log("Collected");
        }

        protected virtual void Collect()
        {
            MoneyManager.Instance.AddMoney(moneyValue);
            Finish();
        }

        protected void Finish()
        {
            _onItemFinished?.Invoke(this);
        }
    }
}