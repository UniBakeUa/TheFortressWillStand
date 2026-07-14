using Towers.ScriptableObjects;
using Towers.UI;
using UI.Factories;
using UI;
using UnityEngine;
using Towers.Models;
using Managers;

namespace Towers.Buildings
{
    public class Drill : BaseBuilding, IMiner
    {
        [Header("Visual Settings")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;
        [SerializeField] private Transform _textContainer;

        private FloatingTextFactory _floatingTextFactory;
        private DrillView _drillView;

        private float _incomeTimer;
        private MoneyManager _moneyManager;
        public DrillModel DrillModel;

        public override void Initialize(BuildingConfig config)
        {
            base.Initialize(config);

            DrillModel = new DrillModel(config as DrillConfig);

            _floatingTextFactory = new FloatingTextFactory(_floatingTextPrefab, _textContainer);

            _drillView = _buildingView as DrillView;
            if (_drillView != null)
            {
                _drillView.SetupTimer(DrillModel.IncomeInterval);
            }

            _moneyManager = MoneyManager.Instance;
        }

        private void Update()
        {
            if (!IsReady || GameStateManager.Instance.CurrentState != GameState.Playing) return;

            _incomeTimer += Time.deltaTime;
            if (_incomeTimer >= DrillModel.IncomeInterval)
            {
                ProcessIncome();
                _incomeTimer = 0f;
            }

            if (_drillView != null)
            {
                _drillView.UpdateMoneyTimer(_incomeTimer);
            }
        }

        public void ProcessIncome()
        {
            _floatingTextFactory.SpawnText(DrillModel.IncomeAmount, transform.position + Vector3.up * 1.5f);
            _moneyManager.AddMoney(DrillModel.IncomeAmount);
        }

        public override void Collapse()
        {
            Debug.Log("Drill зруйновано водою!");

            base.Collapse();
        }
    }
}
