using System.Collections.Generic;
using DG.Tweening;
using Managers;
using TMPro;
using Towers.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Панель вибору карток: розкладає картки, тримає кнопки реролу і пропуску.
    /// Логіки правил не знає - усе питає в PerkSelectionController.
    /// </summary>
    public class PerkSelectionView : MonoBehaviour
    {
        [Header("Корінь панелі")]
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Картки")]
        [SerializeField] private PerkCardView _cardPrefab;
        [Tooltip("Порожній RectTransform. Layout Group на ньому НЕ потрібен - " +
                 "позиції рахує цей скрипт, інакше група б'ється з анімацією карток")]
        [SerializeField] private RectTransform _cardContainer;
        [Tooltip("Відстань між центрами сусідніх карток по X")]
        [SerializeField] private float _cardSpacingX = 230f;
        [Tooltip("Зсув усього ряду по Y відносно контейнера")]
        [SerializeField] private float _cardOffsetY;

        [Header("Кнопки")]
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollCostText;
        [SerializeField] private Button _skipButton;

        [Header("Підказка")]
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private string _freeCardHint = "Перша картка безкоштовна";
        [SerializeField] private string _paidCardHint = "Наступна картка - за пончик";

        [Header("Анімація панелі")]
        [Tooltip("Скільки триває виїзд/заїзд панелі. Весь цей час кліки заблоковані")]
        [SerializeField] private float _transitionDuration = 1f;
        [Tooltip("Панель, що їде. Якщо не задано - береться RectTransform кореня")]
        [SerializeField] private RectTransform _panelRect;
        [Tooltip("Звідки виїжджає і куди ховається панель, у пікселях по Y")]
        [SerializeField] private float _hiddenOffsetY = -1200f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private readonly List<PerkCardView> _spawnedCards = new();
        private PerkSelectionController _controller;

        // Позиція панелі у видимому стані - запам'ятовуємо до першого зсуву.
        private Vector2 _shownPosition;
        private bool _hasShownPosition;
        private Sequence _transition;

        /// <summary>Чи триває зараз виїзд/заїзд панелі. Поки true - кліки не приймаються.</summary>
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (_rerollButton != null) _rerollButton.onClick.AddListener(OnRerollClicked);
            if (_skipButton != null) _skipButton.onClick.AddListener(OnSkipClicked);

            CacheShownPosition();

            if (_root != null) _root.SetActive(false);
        }

        /// <summary>
        /// Запам'ятовує задану в редакторі позицію панелі як "видиму". Робити це
        /// треба до першої анімації, інакше панель поїде до вже зсунутої точки.
        /// </summary>
        private void CacheShownPosition()
        {
            if (_hasShownPosition) return;

            if (_panelRect == null && _root != null)
                _panelRect = _root.transform as RectTransform;

            if (_panelRect == null) return;

            _shownPosition = _panelRect.anchoredPosition;
            _hasShownPosition = true;
        }

        public void Show(IReadOnlyList<PerkConfig> perks, PerkSelectionController controller)
        {
            _controller = controller;

            CacheShownPosition();

            if (_root != null) _root.SetActive(true);

            BuildCards(perks, controller);

            _transition?.Kill();
            IsTransitioning = true;

            // Поки панель летить - кліки не приймаються, щоб гравець не встиг
            // натиснути картку на півдорозі.
            SetInteractable(false);

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            _transition = DOTween.Sequence().SetUpdate(true);

            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = _shownPosition + new Vector2(0f, _hiddenOffsetY);
                _transition.Join(_panelRect.DOAnchorPos(_shownPosition, _transitionDuration).SetEase(_showEase));
            }

            if (_canvasGroup != null)
                _transition.Join(_canvasGroup.DOFade(1f, _transitionDuration * 0.6f));

            _transition.OnComplete(() =>
            {
                IsTransitioning = false;
                SetInteractable(true);
            });
        }

        /// <summary>Вмикає/вимикає прийом кліків усією панеллю.</summary>
        private void SetInteractable(bool value)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.blocksRaycasts = value;
            _canvasGroup.interactable = value;
        }

        /// <summary>Замінює набір карток без перевідкриття панелі (реролл або наступний вибір).</summary>
        public void ShowNewCards(IReadOnlyList<PerkConfig> perks, PerkSelectionController controller)
        {
            BuildCards(perks, controller);
        }

        /// <summary>
        /// Перестворює тільки ті картки, де змінився перк. Потрібно, коли гравець
        /// забрав одну картку: сусідні мають лишитись на місці, без повторної
        /// анімації появи.
        /// </summary>
        public void RefreshChangedCards(IReadOnlyList<PerkConfig> perks, PerkSelectionController controller)
        {
            // Кількість слотів змінилась (колода спорожніла) - простіше зібрати заново.
            if (perks.Count != _spawnedCards.Count)
            {
                BuildCards(perks, controller);
                return;
            }

            for (int i = 0; i < perks.Count; i++)
            {
                PerkCardView existing = _spawnedCards[i];
                if (existing != null && existing.Perk == perks[i]) continue;

                if (existing != null)
                {
                    existing.Discard();
                    Destroy(existing.gameObject);
                }

                PerkCardView card = Instantiate(_cardPrefab, _cardContainer);
                card.Setup(perks[i], controller, indexInRow: 0, GetCardPosition(i, perks.Count), centerX: 0f);
                _spawnedCards[i] = card;
            }

            if (controller != null)
            {
                UpdateCosts(controller.GetCurrentCardPonchicCost(), controller.RerollCost, controller.CanReroll());
            }
        }

        /// <summary>
        /// Ховає панель із анімацією. onComplete спрацює, коли вона повністю
        /// поїхала - контролер тримає стейт вибору до цього моменту.
        /// </summary>
        public void Hide(System.Action onComplete = null)
        {
            _transition?.Kill();
            IsTransitioning = true;

            // Кліки вимикаємо одразу: панель ще на екрані, але вже "не жива".
            SetInteractable(false);

            _transition = DOTween.Sequence().SetUpdate(true);

            if (_panelRect != null && _hasShownPosition)
            {
                _transition.Join(_panelRect
                    .DOAnchorPos(_shownPosition + new Vector2(0f, _hiddenOffsetY), _transitionDuration)
                    .SetEase(_hideEase));
            }

            if (_canvasGroup != null)
            {
                _transition.Join(_canvasGroup.DOFade(0f, _transitionDuration)
                    .SetEase(Ease.InQuad));
            }

            _transition.OnComplete(() =>
            {
                IsTransitioning = false;

                // Картки прибираємо аж тепер - інакше панель їхала б порожньою.
                ClearCards();

                if (_root != null) _root.SetActive(false);
                if (_panelRect != null && _hasShownPosition)
                    _panelRect.anchoredPosition = _shownPosition;

                onComplete?.Invoke();
            });
        }

        /// <summary>Оновлює ціни на картках і стан кнопки реролу.</summary>
        public void UpdateCosts(int cardPonchicCost, int rerollCost, bool canReroll)
        {
            bool canAfford = cardPonchicCost <= 0
                             || (PonchicManager.Instance != null && PonchicManager.Instance.HasPonchics(cardPonchicCost));

            foreach (var card in _spawnedCards)
            {
                if (card != null) card.SetCost(cardPonchicCost, canAfford);
            }

            if (_rerollCostText != null) _rerollCostText.text = rerollCost.ToString();
            if (_rerollButton != null) _rerollButton.interactable = canReroll;

            if (_hintText != null)
                _hintText.text = cardPonchicCost > 0 ? _paidCardHint : _freeCardHint;
        }

        private void BuildCards(IReadOnlyList<PerkConfig> perks, PerkSelectionController controller)
        {
            ClearCards();

            if (_cardPrefab == null || _cardContainer == null) return;

            for (int i = 0; i < perks.Count; i++)
            {
                PerkCardView card = Instantiate(_cardPrefab, _cardContainer);
                // Центр ряду завжди 0: GetCardPosition розкладає картки
                // симетрично відносно початку контейнера.
                card.Setup(perks[i], controller, i, GetCardPosition(i, perks.Count), centerX: 0f);
                _spawnedCards.Add(card);
            }

            // Картки щойно створені - треба одразу проставити їм ціну, інакше вони
            // висітимуть без неї до наступного UpdateCosts.
            if (controller != null)
            {
                UpdateCosts(controller.GetCurrentCardPonchicCost(), controller.RerollCost, controller.CanReroll());
            }
        }

        /// <summary>
        /// Позиція картки в ряду. Ряд симетричний відносно центру контейнера:
        /// для трьох карток це -230 / 0 / +230, для двох - -115 / +115.
        /// </summary>
        private Vector2 GetCardPosition(int index, int totalCards)
        {
            float rowWidth = (totalCards - 1) * _cardSpacingX;
            float x = -rowWidth * 0.5f + index * _cardSpacingX;

            return new Vector2(x, _cardOffsetY);
        }

        private void ClearCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card == null) continue;

                // Destroy у Unity відкладений до кінця кадру, тож картку треба
                // знеактивити явно - інакше вона ще спіймає клік цього ж кадру.
                card.Discard();
                Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
        }

        private void OnRerollClicked() => _controller?.Reroll();

        private void OnSkipClicked() => _controller?.Skip();

        private void OnDestroy()
        {
            if (_rerollButton != null) _rerollButton.onClick.RemoveListener(OnRerollClicked);
            if (_skipButton != null) _skipButton.onClick.RemoveListener(OnSkipClicked);
            if (_canvasGroup != null) _canvasGroup.DOKill();
        }
    }
}
