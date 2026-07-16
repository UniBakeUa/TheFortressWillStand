using DG.Tweening;
using Managers;
using Towers;
using UI;
using UI.Factories;
using UnityEngine;

namespace Items
{
    public class Jellyfish : ClickableItem, IDamageable
    {
        [Header("Visual Settings")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;

        private FloatingTextFactory _floatingTextFactory;

        public float ExposureFraction => 0;

        private void Awake()
        {
            _floatingTextFactory = new FloatingTextFactory(_floatingTextPrefab, transform.parent);
        }

        public void SwimTo(Vector3 startPos, Vector3 targetPos, float duration)
        {
            transform.DOKill();

            transform.position = startPos;

            Sequence swimSequence = DOTween.Sequence();

            swimSequence.Append(transform.DOMove(targetPos, duration).SetEase(Ease.OutSine));

            swimSequence.Join(transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0), duration, 3, 0.5f));
        }

        protected override void OnMouseDown()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing &&
                GameStateManager.Instance.CurrentState != GameState.Building)
                return;

            if (MoneyManager.Instance.GetMoney() >= moneyValue)
            {
                MoneyManager.Instance.SpendMoney(moneyValue);
                _floatingTextFactory.SpawnText((int)-moneyValue, transform.position + Vector3.up * 1.5f);
                transform.DOKill();
                _onItemFinished?.Invoke(this);
            }
            else
            {
                MoneyManager.Instance.ShowNotEnoughPopup();
            }
        }

        public void TakeDamage(float amount)
        {
            
        }

        public void Repair(float amount)
        {
            
        }

        public void Collapse()
        {
            transform.DOKill();
            _onItemFinished?.Invoke(this);
        }
    }
}