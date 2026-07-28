using System;
using System.Collections.Generic;
using Towers.ScriptableObjects;
using UI;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Керує стейтом вибору карток між хвилями.
    ///
    /// Правила раунду вибору:
    /// - перша картка безкоштовна;
    /// - кожна наступна коштує 1 пончик;
    /// - реролл один раз за раунд, за 5 монет, перемішує всі показані картки;
    /// - щойно гравець узяв картку і пончиків більше немає - стейт закінчується.
    /// </summary>
    public class PerkSelectionController : MonoBehaviour
    {
        public static PerkSelectionController Instance { get; private set; }

        [Header("Залежності")]
        [SerializeField] private PerkManager _perkManager;
        [SerializeField] private PerkSelectionView _view;
        [SerializeField] private WaveManager _waveManager;

        [Header("Правила")]
        [Tooltip("Скільки карток показувати за раз")]
        [SerializeField] private int _cardsPerRoll = 3;
        [Tooltip("Вартість реролу в монетах")]
        [SerializeField] private int _rerollCost = 5;
        [Tooltip("Скільки пончиків коштує кожна картка після першої (безкоштовної)")]
        [SerializeField] private int _ponchicCostPerExtraCard = 1;

        // Стан поточного раунду вибору.
        private bool _hasTakenCard;
        private bool _hasRerolled;
        private readonly List<PerkConfig> _currentOffer = new();

        // Колода на всю гру: тасується один раз на старті, далі тільки тягнемо.
        private readonly List<PerkConfig> _deck = new();
        private bool _isDeckBuilt;

        /// <summary>Скільки карток лишилось у колоді.</summary>
        public int CardsLeftInDeck => _deck.Count;

        /// <summary>Спрацьовує, коли раунд вибору завершено і можна йти далі.</summary>
        public event Action OnSelectionFinished;

        /// <summary>Чи триває зараз вибір карток.</summary>
        public bool IsSelecting { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Колода готується ще до першої хвилі - вона одна на всю гру.
            EnsureDeckBuilt();
        }

        private void EnsureDeckBuilt()
        {
            if (_isDeckBuilt) return;

            BuildDeck();
            _isDeckBuilt = true;
        }

        /// <summary>Відкриває панель вибору. Кличе WaveManager у кінці раунду.</summary>
        public void BeginSelection()
        {
            if (IsSelecting) return;

            // Фортеця могла впасти на останніх кадрах хвилі - тоді гра вже
            // програна і панель прокачки показувати нема сенсу.
            if (GameStateManager.Instance.CurrentState == GameState.Paused)
            {
                OnSelectionFinished?.Invoke();
                return;
            }

            EnsureDeckBuilt();

            _hasTakenCard = false;
            _hasRerolled = false;

            RollNewOffer();

            // Колода вичерпалась за гру - етап прокачки просто пропускаємо.
            // Стейт НЕ чіпаємо: перемкнувши його на PerkSelection до цієї
            // перевірки, ми лишили б гру в стані, з якого ніщо не виводить -
            // панелі немає, а WaveManager чекає на OnSelectionFinished.
            if (_currentOffer.Count == 0)
            {
                OnSelectionFinished?.Invoke();
                return;
            }

            IsSelecting = true;
            GameStateManager.Instance.ChangeState(GameState.PerkSelection);

            _view.Show(_currentOffer, this);
            RefreshViewState();
        }

        /// <summary>Ціна наступної картки в пончиках (0 - перша, безкоштовна).</summary>
        public int GetCurrentCardPonchicCost() => _hasTakenCard ? _ponchicCostPerExtraCard : 0;

        public int RerollCost => _rerollCost;

        /// <summary>Чи доступний реролл прямо зараз.</summary>
        public bool CanReroll()
        {
            if (_hasRerolled) return false;
            if (MoneyManager.Instance == null) return false;
            return MoneyManager.Instance.GetMoney() >= _rerollCost;
        }

        /// <summary>Гравець натиснув реролл: перемішує всі показані картки за монети.</summary>
        public void Reroll()
        {
            if (!IsSelecting) return;
            if (_view.IsTransitioning) return;
            if (_hasRerolled) return;

            if (MoneyManager.Instance == null || MoneyManager.Instance.GetMoney() < _rerollCost)
            {
                MoneyManager.Instance?.ShowNotEnoughPopup();
                return;
            }

            RollNewOffer();

            // Реролл ніколи не завершує вибір. Якщо колода нічого не дала -
            // грошей не беремо і лишаємо гравцю спробу.
            if (_currentOffer.Count == 0)
            {
                RefreshViewState();
                return;
            }

            MoneyManager.Instance.SpendMoney(_rerollCost);
            _hasRerolled = true;

            _view.ShowNewCards(_currentOffer, this);
            RefreshViewState();
        }

        /// <summary>Гравець забрав картку. Повертає false, якщо не вистачило пончиків.</summary>
        public bool TakeCard(PerkConfig perk)
        {
            if (!IsSelecting || perk == null) return false;
            if (_view.IsTransitioning) return false;

            int cost = GetCurrentCardPonchicCost();

            if (cost > 0)
            {
                if (PonchicManager.Instance == null || !PonchicManager.Instance.TrySpendPonchics(cost))
                {
                    // До цього не мало дійти - картки блокуються заздалегідь,
                    // але клік міг випередити оновлення UI.
                    FinishSelection();
                    return false;
                }
            }

            _perkManager.ApplyPerk(perk);
            _hasTakenCard = true;

            // Перк міг сам дати пончиків ("прибрати колаборанта") - тоді гравець
            // одразу може взяти ще одну картку, і стейт триває далі.
            if (!CanAffordAnotherCard())
            {
                FinishSelection();
                return true;
            }

            // Міняємо лише взяту картку - сусідні лишаються ті самі, щоб гравець
            // не втрачав з очей те, що вже роздивився.
            ReplaceCard(perk);

            if (_currentOffer.Count == 0)
            {
                // Колода скінчилась - пропонувати більше нічого.
                FinishSelection();
                return true;
            }

            _view.RefreshChangedCards(_currentOffer, this);
            RefreshViewState();
            return true;
        }

        /// <summary>Гравець свідомо пропустив вибір.</summary>
        public void Skip()
        {
            if (!IsSelecting) return;
            if (_view.IsTransitioning) return;

            FinishSelection();
        }

        private bool CanAffordAnotherCard()
        {
            int cost = GetCurrentCardPonchicCost();
            if (cost <= 0) return true;

            return PonchicManager.Instance != null && PonchicManager.Instance.HasPonchics(cost);
        }

        private void FinishSelection()
        {
            if (!IsSelecting) return;

            IsSelecting = false;

            // Невзяті картки лишаються в грі - повертаємо їх у колоду, інакше
            // кожен раунд назавжди з'їдав би по три перки.
            ReturnOfferToDeck();

            // Раунд вважається завершеним лише коли панель доїхала: інакше
            // Building почався б поверх анімації, що ще на екрані.
            _view.Hide(() => OnSelectionFinished?.Invoke());
        }

        private void RefreshViewState()
        {
            _view.UpdateCosts(GetCurrentCardPonchicCost(), _rerollCost, CanReroll());
        }

        /// <summary>
        /// Збирає і тасує колоду на старті гри: кожен перк кладеться стільки разів,
        /// скільки вказано в CopiesInDeck. Далі картки тільки тягнуться з неї, тож
        /// унікальний перк фізично не може трапитись двічі.
        /// </summary>
        private void BuildDeck()
        {
            _deck.Clear();

            if (_perkManager == null || _perkManager.Library == null) return;

            foreach (var perk in _perkManager.Library.Perks)
            {
                if (perk == null) continue;

                for (int i = 0; i < perk.CopiesInDeck; i++)
                {
                    _deck.Add(perk);
                }
            }

            Shuffle(_deck);
        }

        private void Shuffle(List<PerkConfig> deck)
        {
            // Фішер-Йейтс.
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        /// <summary>
        /// Тягне з колоди одну картку, придатну до показу зараз.
        /// </summary>
        /// <param name="exclude">
        /// Картки, які вже лежать на столі - щоб у наборі не було двох однакових,
        /// навіть якщо в колоді кілька копій.
        /// </param>
        private PerkConfig DrawCard(List<PerkConfig> exclude)
        {
            int currentWave = _waveManager != null ? _waveManager.CurrentLevel : 0;

            for (int i = 0; i < _deck.Count; i++)
            {
                PerkConfig candidate = _deck[i];

                // Ще закрита за хвилею - лишаємо в колоді на потім.
                if (candidate.RequiredWave > currentWave) continue;
                if (exclude != null && exclude.Contains(candidate)) continue;

                _deck.RemoveAt(i);
                return candidate;
            }

            return null;
        }

        /// <summary>Набирає повний набір карток на стіл.</summary>
        private void RollNewOffer()
        {
            ReturnOfferToDeck();
            _currentOffer.Clear();

            for (int i = 0; i < _cardsPerRoll; i++)
            {
                PerkConfig card = DrawCard(_currentOffer);
                if (card == null) break;

                _currentOffer.Add(card);
            }
        }

        /// <summary>
        /// Замінює одну картку в наборі на нову з колоди, решту лишає на місці.
        /// Якщо колода порожня - картка просто зникає з набору.
        /// </summary>
        private void ReplaceCard(PerkConfig taken)
        {
            int index = _currentOffer.IndexOf(taken);
            if (index < 0) return;

            // Прибираємо взяту картку зі столу ДО добору: інакше вона лишалась би
            // у списку виключень і блокувала власні ж копії в колоді.
            _currentOffer.RemoveAt(index);

            PerkConfig replacement = DrawCard(_currentOffer);

            if (replacement != null)
                _currentOffer.Insert(index, replacement);
        }

        /// <summary>
        /// Повертає нерозіграні картки в колоду і перетасовує. Потрібно при реролі
        /// та в кінці раунду, інакше показані, але не взяті картки просто зникали б
        /// з гри назавжди.
        /// </summary>
        private void ReturnOfferToDeck()
        {
            if (_currentOffer.Count == 0) return;

            _deck.AddRange(_currentOffer);
            _currentOffer.Clear();
            Shuffle(_deck);
        }
    }
}
