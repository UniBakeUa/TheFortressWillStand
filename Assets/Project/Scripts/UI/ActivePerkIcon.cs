using DG.Tweening;
using TMPro;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Одна іконка в списку активних прокачок. Показує спрайт перка і, якщо його
    /// брали кілька разів, лічильник "x2".
    /// </summary>
    public class ActivePerkIcon : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [Tooltip("Підкладка/рамка, якій фарбується Tint з конфіга")]
        [SerializeField] private Image _frameImage;
        [SerializeField] private TMP_Text _stacksText;
        [Tooltip("Вмикається, лише коли перк узято більше одного разу")]
        [SerializeField] private GameObject _stacksRoot;

        [Header("Анімація появи")]
        [SerializeField] private float _appearDuration = 0.3f;

        private PerkConfig _perk;

        /// <summary>Який перк показує ця іконка - щоб в'юшка не перестворювала зайве.</summary>
        public PerkConfig Perk => _perk;

        public void Setup(PerkConfig perk, int stacks)
        {
            _perk = perk;

            if (_iconImage != null)
            {
                _iconImage.sprite = perk.Icon;
                _iconImage.enabled = perk.Icon != null;
            }

            if (_frameImage != null) _frameImage.color = perk.Tint;

            SetStacks(stacks);
        }

        /// <summary>Оновлює лічильник без перестворення іконки.</summary>
        public void SetStacks(int stacks)
        {
            if (_stacksRoot != null) _stacksRoot.SetActive(stacks > 1);
            if (_stacksText != null) _stacksText.text = stacks.ToString();
        }

        /// <summary>Коротка анімація появи - щоб нова прокачка кидалась в очі.</summary>
        public void PlayAppear()
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, _appearDuration)
                .SetEase(Ease.OutBack)
                // Іконка з'являється під час паузи вибору, тож ігноруємо timeScale.
                .SetUpdate(true);
        }

        /// <summary>Пульс, коли вже наявний перк узяли ще раз.</summary>
        public void PlayStackPulse()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.25f, _appearDuration, vibrato: 6)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
