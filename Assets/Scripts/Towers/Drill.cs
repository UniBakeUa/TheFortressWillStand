using System.Collections;
using UnityEngine;
using Money;
using Towers.UI;
using UI;
using UI.Factories;

namespace Towers
{
    public class Drill : Solid, IIncomeProducer
    {
        [Header("Income Settings")]
        [SerializeField] float moneyPerTick = 1f;
        [SerializeField] float tickInterval = 5f;

        [Header("Visual Settings")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;
        [SerializeField] private Transform _textContainer;

        private FloatingTextFactory _floatingTextFactory;
        private DrillView _drillView;

        public float IncomePerTick => moneyPerTick;
        public float TickInterval => tickInterval;

        float _incomeTimer;
        MoneyManager _moneyManager;

        // ФІКС: раніше було "private void Start()" без override - це ХОВАЛО Solid.Start()
        // замість перевизначення (спрацьовувало лише тому, що всередині явно викликали base.Start()).
        // Тепер це правильний override, як і має бути для узгодженості з рештою ієрархії.
        protected override void Start()
        {
            base.Start(); // критично: тут IsReady=true і RegisterFootprint()

            _floatingTextFactory = new FloatingTextFactory(_floatingTextPrefab, _textContainer);
            _drillView = _buildingView as DrillView;
            _drillView.SetupTimer(tickInterval);
            _moneyManager = MoneyManager.Instance;
            PlaySpawnAnimation();
        }

        private void Update()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _incomeTimer += Time.deltaTime;
            if (_incomeTimer >= tickInterval)
            {
                ProduceMoney();
                _incomeTimer = 0f;
            }

            _drillView.UpdateMoneyTimer(_incomeTimer);
        }

        private void ProduceMoney()
        {
            _floatingTextFactory.SpawnText((int)moneyPerTick, transform.position + Vector3.up * 1.5f);
            _moneyManager.AddMoney((int)moneyPerTick);
        }

        public override void Collapse()
        {
            Debug.Log("Drill зруйновано водою!");
            base.Collapse();
        }
    }
}