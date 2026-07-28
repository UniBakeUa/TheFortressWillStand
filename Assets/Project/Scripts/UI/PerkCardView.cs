using DG.Tweening;
using Managers;
using TMPro;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Одна картка прокачки. При наведенні ефектно підростає, при кліку -
    /// повідомляє контролер.
    /// </summary>
    public class PerkCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Елементи картки")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [Tooltip("Вмикається, коли картка платна. Всередині - іконка пончика")]
        [SerializeField] private GameObject _costRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Анімація наведення")]
        [SerializeField] private float _hoverScale = 1.08f;
        [SerializeField] private float _hoverDuration = 0.18f;
        [SerializeField] private float _hoverLiftY = 12f;

        [Header("Анімація появи")]
        [SerializeField] private float _appearDuration = 0.35f;
        [Tooltip("Затримка між появою сусідніх карток")]
        [SerializeField] private float _appearStagger = 0.08f;
        [SerializeField] private float _appearFromY = -120f;
        [Tooltip("Чи злітаються картки з центру ряду. Вимкнено - кожна просто " +
                 "піднімається знизу на своє місце")]
        [SerializeField] private bool _appearFromCenter = true;

        [Header("Анімація взяття")]
        [Tooltip("На скільки картка злітає вгору, коли її забрали")]
        [SerializeField] private float _takenFlyDistance = 220f;
        [SerializeField] private float _takenScale = 1.15f;
        [Tooltip("Має бути коротко: нова картка чекає, поки ця долетить")]
        [SerializeField] private float _takenDuration = 0.2f;

        [Header("Недоступна картка")]
        [SerializeField, Range(0f, 1f)] private float _lockedAlpha = 0.45f;

        private PerkConfig _perk;

        /// <summary>Який перк зараз на картці - щоб панель бачила, що змінилось.</summary>
        public PerkConfig Perk => _perk;

        private PerkSelectionController _controller;
        private RectTransform _rect;
        private Vector2 _restPosition;
        private float _centerX;
        private bool _isInteractable = true;
        private Sequence _hoverSequence;
        private Sequence _appearSequence;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        /// <summary>
        /// Наповнює картку даними перка і програє появу.
        /// </summary>
        /// <param name="restPosition">
        /// Куди картка має прилетіти. Рахує PerkSelectionView, а не Layout Group:
        /// груп-компонент пересував би картку вже після старту твіна і ламав
        /// анімацію появи й наведення.
        /// </param>
        /// <param name="centerX">X центру ряду - звідти картки розлітаються на свої місця.</param>
        /// <param name="appearDelay">
        /// Пауза перед появою. Використовується, коли на цьому місці ще долітає
        /// щойно взята картка.
        /// </param>
        public void Setup(PerkConfig perk, PerkSelectionController controller, int indexInRow,
            Vector2 restPosition, float centerX, float appearDelay = 0f)
        {
            _restPosition = restPosition;
            _centerX = centerX;

            _perk = perk;
            _controller = controller;

            if (_nameText != null) _nameText.text = perk.DisplayName;
            if (_descriptionText != null) _descriptionText.text = perk.GetFormattedDescription();

            if (_iconImage != null)
            {
                _iconImage.sprite = perk.Icon;
                // Без спрайта картка не має світити порожнім білим квадратом.
                _iconImage.enabled = perk.Icon != null;
            }

            if (_frameImage != null) _frameImage.color = perk.Tint;

            PlayAppearAnimation(indexInRow, appearDelay);
        }

        /// <summary>Оновлює ціну картки в пончиках і доступність.</summary>
        public void SetCost(int ponchicCost, bool canAfford)
        {
            if (_costRoot != null) _costRoot.SetActive(ponchicCost > 0);

            SetInteractable(canAfford);
        }

        private void SetInteractable(bool value)
        {
            _isInteractable = value;

            // Картка вже відлітає - її alpha веде твін, і перезаписувати її тут
            // означало б обірвати згасання.
            if (_isFlyingAway)
            {
                if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = value ? 1f : _lockedAlpha;
                // Заблокована картка не має ловити наведення чи клік.
                _canvasGroup.blocksRaycasts = value;
            }
        }

        private void PlayAppearAnimation(int indexInRow, float appearDelay = 0f)
        {
            _appearSequence?.Kill();

            // _restPosition уже задана ззовні (Setup) - hover-анімація повертає
            // картку саме сюди.
            _rect.localScale = Vector3.one * 0.8f;

            // Стартуємо або з центру ряду (картки розлітаються в боки), або
            // просто нижче власного місця.
            float startX = _appearFromCenter ? _centerX : _restPosition.x;
            _rect.anchoredPosition = new Vector2(startX, _restPosition.y + _appearFromY);

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            _appearSequence = DOTween.Sequence();
            _appearSequence.AppendInterval(appearDelay + indexInRow * _appearStagger);
            _appearSequence.Append(_rect.DOAnchorPos(_restPosition, _appearDuration).SetEase(Ease.OutBack));
            _appearSequence.Join(_rect.DOScale(1f, _appearDuration).SetEase(Ease.OutBack));

            if (_canvasGroup != null)
            {
                _appearSequence.Join(_canvasGroup.DOFade(_isInteractable ? 1f : _lockedAlpha, _appearDuration));
            }

            // Анімація крутиться на паузі вибору, тож ігнорує Time.timeScale.
            _appearSequence.SetUpdate(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            _hoverSequence?.Kill();
            _hoverSequence = DOTween.Sequence();
            _hoverSequence.Append(_rect.DOScale(_hoverScale, _hoverDuration).SetEase(Ease.OutBack));
            _hoverSequence.Join(_rect.DOAnchorPos(_restPosition + new Vector2(0f, _hoverLiftY), _hoverDuration).SetEase(Ease.OutCubic));
            _hoverSequence.SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverSequence?.Kill();
            _hoverSequence = DOTween.Sequence();
            _hoverSequence.Append(_rect.DOScale(1f, _hoverDuration).SetEase(Ease.OutCubic));
            _hoverSequence.Join(_rect.DOAnchorPos(_restPosition, _hoverDuration).SetEase(Ease.OutCubic));
            _hoverSequence.SetUpdate(true);
        }

        /// <summary>
        /// Робить картку остаточно неактивною. Викликається перед Destroy: той
        /// відкладений до кінця кадру, тож картка ще встигла б спіймати клік.
        /// </summary>
        public void Discard()
        {
            _perk = null;
            _controller = null;
            SetInteractable(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable || _perk == null || _controller == null) return;

            // Знімаємо інтерактив одразу: другий клік по тій самій картці, поки
            // програється анімація, не має купити перк двічі.
            SetInteractable(false);

            PlayTakenAnimation();

            _controller.TakeCard(_perk);
        }

        /// <summary>
        /// Ривок угору зі згасанням - підтвердження, що картку забрали. Грає на
        /// цій, уже приреченій картці; заміна створюється поверх неї окремо.
        /// </summary>
        private void PlayTakenAnimation()
        {
            _hoverSequence?.Kill();
            _appearSequence?.Kill();

            // Виводимо з-під нової картки, щоб вони не накладались.
            _rect.SetAsLastSibling();

            Sequence taken = DOTween.Sequence().SetUpdate(true);
            // InBack: картка спершу трохи присідає вниз, а тоді її різко висмикує
            // вгору - читається як ривок, а не як плавний від'їзд.
            taken.Join(_rect.DOAnchorPos(_restPosition + new Vector2(0f, _takenFlyDistance), _takenDuration)
                .SetEase(Ease.InBack));
            taken.Join(_rect.DOScale(_takenScale, _takenDuration).SetEase(Ease.InQuad));

            if (_canvasGroup != null)
            {
                // Згасання під кінець: більшу частину польоту картка лишається
                // видимою, інакше ривок губиться.
                taken.Join(_canvasGroup.DOFade(0f, _takenDuration * 0.6f)
                    .SetDelay(_takenDuration * 0.4f));
            }

            _isFlyingAway = true;
            taken.OnComplete(() =>
            {
                if (this != null) Destroy(gameObject);
            });
        }

        /// <summary>
        /// Картку вже забрали і вона долітає. Панель не має її знищувати - інакше
        /// анімація обірветься на першому ж кадрі.
        /// </summary>
        public bool IsFlyingAway => _isFlyingAway;

        /// <summary>Скільки триває виліт - панель на цей час притримує нову картку.</summary>
        public float TakenDuration => _takenDuration;

        private bool _isFlyingAway;

        private void OnDestroy()
        {
            _hoverSequence?.Kill();
            _appearSequence?.Kill();
            _rect.DOKill();
        }
    }
}
